// Revisa el título y la descripción del PR.
//
// El título y la descripción los escribe quien abre el PR: son texto no confiable. Por eso se leen del
// archivo del evento (GITHUB_EVENT_PATH) y no con ${{ github.event.pull_request.title }} dentro de un
// `run:`, que pegaría el texto en el script y permitiría inyectar comandos.

import { appendFileSync, readFileSync } from "node:fs";

const { pull_request: pr } = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, "utf8"));
const errors = [];

// 1. Título con Conventional Commits: tipo(alcance opcional): descripción.
const TYPES = ["feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "revert"];
const title = new RegExp(`^(${TYPES.join("|")})(\\([a-z0-9-]+\\))?!?: \\S.*`);
if (!title.test(pr.title)) {
  errors.push(
    `El título "${pr.title}" no sigue Conventional Commits. Tiene que empezar con un tipo ` +
      `(${TYPES.join(", ")}), opcionalmente un alcance, y dos puntos. Ejemplo: "feat(web): add a stat comparator".`,
  );
}

// 2. Descripción: los bots (Dependabot) no usan la plantilla; el resto tiene que completar las secciones.
const isBot = pr.user.type === "Bot";
// Una sección cuenta como vacía si no está, si no tiene texto, o si quedó igual que en la plantilla.
// Se compara contra la plantilla en vez de borrar sus comentarios <!-- -->: borrar con una regex es una
// sanitización incompleta (CodeQL lo marcó: "<!-<!---->-" deja un "<!--" después del reemplazo).
function sections(markdown) {
  const result = new Map();
  for (const part of markdown.replace(/\r/g, "").split(/^## /m).slice(1)) {
    const newline = part.indexOf("\n");
    const name = (newline === -1 ? part : part.slice(0, newline)).trim();
    result.set(name, newline === -1 ? "" : part.slice(newline + 1).trim());
  }
  return result;
}

if (!isBot) {
  const template = sections(readFileSync(".github/PULL_REQUEST_TEMPLATE.md", "utf8"));
  const body = sections(pr.body ?? "");
  for (const [name, placeholder] of template) {
    if (name === "Checklist") continue;
    const text = body.get(name);
    if (!text || text === placeholder) errors.push(`La sección "${name}" de la descripción está vacía (o no está).`);
  }
}

const summary = errors.length
  ? `## Título y descripción: hay que corregir\n\n${errors.map((e) => `- ${e}`).join("\n")}\n`
  : "## Título y descripción: en orden\n";
appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary);

for (const error of errors) console.log(`::error title=Título y descripción::${error}`);
if (errors.length) process.exit(1);
console.log(isBot ? "Título en orden (PR de un bot: no se revisa la descripción)." : "Título y descripción en orden.");
