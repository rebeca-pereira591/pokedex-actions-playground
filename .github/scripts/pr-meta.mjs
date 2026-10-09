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
if (!isBot) {
  const template = readFileSync(".github/PULL_REQUEST_TEMPLATE.md", "utf8");
  const required = [...template.matchAll(/^## (.+)$/gm)].map((m) => m[1]).filter((s) => s !== "Checklist");
  // Sin los comentarios de la plantilla (<!-- ... -->), lo que queda en cada sección es lo que escribió la persona.
  const body = (pr.body ?? "").replace(/<!--[\s\S]*?-->/g, "").replace(/\r/g, "");
  for (const section of required) {
    const text = body.match(new RegExp(`^## ${section}\\n([\\s\\S]*?)(?=^## |$(?![\\s\\S]))`, "m"))?.[1].trim();
    if (!text) errors.push(`La sección "${section}" de la descripción está vacía (o no está).`);
  }
}

const summary = errors.length
  ? `## Título y descripción: hay que corregir\n\n${errors.map((e) => `- ${e}`).join("\n")}\n`
  : "## Título y descripción: en orden\n";
appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary);

for (const error of errors) console.log(`::error title=Título y descripción::${error}`);
if (errors.length) process.exit(1);
console.log(isBot ? "Título en orden (PR de un bot: no se revisa la descripción)." : "Título y descripción en orden.");
