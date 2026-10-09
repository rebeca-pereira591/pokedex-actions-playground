// Review con IA de un PR: baja el diff, se lo pasa a un modelo de Gemini (Google) junto con las reglas de
// .github/prompts/review.md, y publica lo que encuentre como un review con comentarios en las líneas.
//
// El plan original usaba GitHub Models, que GitHub retiró el 2026-07-30. Gemini tiene una capa gratuita
// sin tarjeta y acepta el mismo formato de pedido (el "chat completions" de OpenAI).
//
// Variables: GITHUB_TOKEN (pull-requests: write), GEMINI_API_KEY, GITHUB_REPOSITORY, PR_NUMBER, HEAD_SHA,
// MODELS (opcional: lista separada por comas) y DRY_RUN (opcional: muestra el review sin publicarlo).

import { readFileSync } from "node:fs";

const env = process.env;
const api = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}`;
// Se prueban en orden: si uno está saturado (503) o se pasó de cuota (429), se usa el siguiente.
const MODELS = (env.MODELS || "gemini-3.8-flash,gemini-3.6-flash,gemini-3.5-flash").split(",").map((m) => m.trim());
const ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";

// Validar al principio que la clave esté: si no, el error aparecería después como un 401 confuso.
if (!env.GEMINI_API_KEY) {
  console.log("::error title=Review con IA::Falta el secret GEMINI_API_KEY en el repo.");
  process.exit(1);
}
const MAX_DIFF = 60_000; // caracteres: un diff enorme no entra en el modelo (y gasta cuota)

async function github(path, { accept = "application/vnd.github+json", ...options } = {}) {
  const response = await fetch(`${api}${path}`, {
    ...options,
    headers: { Authorization: `Bearer ${env.GITHUB_TOKEN}`, Accept: accept, "X-GitHub-Api-Version": "2022-11-28" },
  });
  if (!response.ok) throw new Error(`${options.method ?? "GET"} ${path}: ${response.status} ${await response.text()}`);
  return accept.includes("diff") ? response.text() : response.json();
}

// 1. El diff del PR, tal como lo ve GitHub.
let diff = await github(`/pulls/${env.PR_NUMBER}`, { accept: "application/vnd.github.v3.diff" });
const truncated = diff.length > MAX_DIFF;
if (truncated) diff = diff.slice(0, MAX_DIFF);

// 2. Qué líneas se pueden comentar: GitHub sólo acepta comentarios en líneas que aparecen en el diff, y
//    rechaza el review ENTERO (error 422) si una sola no está. Se arma el mapa archivo -> líneas nuevas.
const commentable = new Map();
let file = null;
let line = 0;
for (const row of diff.split("\n")) {
  if (row.startsWith("+++ ")) {
    file = row.startsWith("+++ b/") ? row.slice(6) : null;
    if (file) commentable.set(file, new Set());
  } else if (row.startsWith("@@")) {
    line = Number(row.match(/\+(\d+)/)?.[1] ?? 0); // "@@ -10,4 +12,6 @@": la versión nueva empieza en 12
  } else if (file && (row.startsWith("+") || row.startsWith(" "))) {
    commentable.get(file).add(line);
    line += 1;
  }
}

// 3. Las reglas viven en el repo; el modelo las recibe como instrucciones y el diff como contenido.
const rules = readFileSync(".github/prompts/review.md", "utf8");
const messages = [
  { role: "system", content: rules },
  { role: "user", content: `Diff del PR #${env.PR_NUMBER}:\n\n\`\`\`diff\n${diff}\n\`\`\`` },
];
let MODEL;
let answer;
for (const model of MODELS) {
  const response = await fetch(ENDPOINT, {
    method: "POST",
    headers: { Authorization: `Bearer ${env.GEMINI_API_KEY}`, "Content-Type": "application/json" },
    // temperature 0: lo más estable posible, el mismo diff da la misma respuesta.
    body: JSON.stringify({ model, temperature: 0, response_format: { type: "json_object" }, messages }),
  });
  if (response.status === 503 || response.status === 429) {
    console.log(`${model}: ${response.status === 503 ? "saturado" : "sin cuota"}, se prueba el siguiente.`);
    continue;
  }
  if (!response.ok) throw new Error(`Gemini (${model}): ${response.status} ${await response.text()}`);
  MODEL = model;
  answer = (await response.json()).choices?.[0]?.message?.content ?? "";
  break;
}
if (!MODEL) {
  console.log(`::error title=Review con IA::Ningún modelo respondió (${MODELS.join(", ")}). Probar más tarde.`);
  process.exit(1);
}

// 4. Fallar cerrado: si el modelo no devolvió el JSON pedido, es un error, no "sin hallazgos".
let findings;
try {
  findings = JSON.parse(answer).findings;
  if (!Array.isArray(findings)) throw new Error("falta el array findings");
} catch (error) {
  console.log(`::error title=Review con IA::El modelo no devolvió el JSON esperado (${error.message}).`);
  console.log(answer.slice(0, 2000));
  process.exit(1);
}

// 5. Cada hallazgo va pegado a su línea si está en el diff; si no, al texto general del review.
const format = (f) => `**[${f.severity}] ${f.title}**\n\n${f.body}`;
const inline = [];
const general = [];
for (const f of findings) {
  if (commentable.get(f.file)?.has(Number(f.line))) {
    inline.push({ path: f.file, line: Number(f.line), side: "RIGHT", body: format(f) });
  } else {
    general.push(`- \`${f.file}:${f.line}\` ${format(f).replace(/\n\n/g, ": ")}`);
  }
}

const summary = [
  "## Review con IA",
  "",
  findings.length
    ? `${findings.length} ${findings.length === 1 ? "hallazgo" : "hallazgos"} (modelo \`${MODEL}\`).`
    : `Sin hallazgos (modelo \`${MODEL}\`).`,
  truncated ? `\nEl diff es muy grande: se revisaron sólo los primeros ${MAX_DIFF} caracteres.` : "",
  general.length ? `\nFuera de las líneas del diff:\n\n${general.join("\n")}` : "",
  "\n<sub>Es una ayuda, no un veredicto: puede equivocarse. Para volver a pedirlo, sacar y poner la etiqueta `ai-review`.</sub>",
].join("\n");

if (env.DRY_RUN) {
  console.log(summary);
  for (const c of inline) console.log(`\n--- ${c.path}:${c.line}\n${c.body}`);
  process.exit(0);
}

// 6. Un review de tipo COMMENT: no aprueba ni bloquea, sólo comenta.
await github(`/pulls/${env.PR_NUMBER}/reviews`, {
  method: "POST",
  body: JSON.stringify({ commit_id: env.HEAD_SHA, event: "COMMENT", body: summary, comments: inline }),
});
console.log(`Review publicado: ${inline.length} en líneas, ${general.length} generales.`);
