// Arma la tabla de resultados del run actual y la publica en dos lugares:
//   1. el resumen del run ($GITHUB_STEP_SUMMARY), que se ve en la pestaña Actions;
//   2. un comentario fijo en el PR, que se edita en cada push en vez de duplicarse.
//
// Lee los jobs y pasos de la API de GitHub (no de los `outputs` de los jobs: en un job con matriz,
// como los shards, cada shard pisaría el output del anterior). Sin dependencias: Node 24 trae fetch.
//
// Variables: GITHUB_TOKEN, GITHUB_REPOSITORY, GITHUB_RUN_ID, GITHUB_SERVER_URL, GITHUB_STEP_SUMMARY,
// PR_NUMBER (vacío si se corrió a mano) y HEAD_SHA.

import { appendFileSync } from "node:fs";

const env = process.env;
const api = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}`;
const runId = Number(env.GITHUB_RUN_ID);
const runUrl = `${env.GITHUB_SERVER_URL}/${env.GITHUB_REPOSITORY}/actions/runs/${runId}`;

// El marcador oculto identifica el comentario de ESTE workflow (y guarda de qué run es).
const MARKER = "<!-- pr-check-results";

// Este job y los pasos de preparación no aportan nada a la tabla.
const OWN_JOB = "Resultados";
const GATE_JOB = "PR gate";
const SETUP_STEP = /^(Set up job|Complete job|Post |Run actions\/|Run pnpm\/)/;

async function github(path, options = {}) {
  const response = await fetch(path.startsWith("http") ? path : `${api}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${env.GITHUB_TOKEN}`,
      Accept: "application/vnd.github+json",
      "X-GitHub-Api-Version": "2022-11-28",
      ...options.headers,
    },
  });
  if (!response.ok) {
    throw new Error(`${options.method ?? "GET"} ${path}: ${response.status} ${await response.text()}`);
  }
  return response.status === 204 ? null : response.json();
}

const LABEL = {
  success: "pasó",
  failure: "**falló**",
  cancelled: "**cancelado**",
  skipped: "salteado",
  in_progress: "corriendo",
  queued: "en cola",
};

const label = (item) => LABEL[item.conclusion ?? item.status] ?? (item.conclusion ?? item.status);

function duration(item) {
  if (!item.started_at || !item.completed_at) return "—";
  const seconds = Math.round((new Date(item.completed_at) - new Date(item.started_at)) / 1000);
  return seconds < 60 ? `${seconds} s` : `${Math.floor(seconds / 60)} min ${seconds % 60} s`;
}

// Los errores que el job dejó como anotaciones (tsc, Biome, el gate): el "por qué" de la falla.
async function annotations(job) {
  const list = await github(`/check-runs/${job.id}/annotations?per_page=20`);
  return list
    .filter((a) => a.annotation_level === "failure")
    .filter((a) => !a.message.startsWith("Process completed with exit code")) // no dice nada
    .map((a) => {
      const where = a.path && a.path !== ".github" ? `\`${a.path}:${a.start_line}\` ` : "";
      return `- ${where}${a.message.split("\n")[0]}`;
    });
}

// La API devuelve los jobs en cualquier orden: se ordenan como se leen en el workflow.
const ORDER = ["Detectar cambios", "Front (", "Front tests", "Front cobertura", "Back", GATE_JOB];
const rank = (job) => {
  const index = ORDER.findIndex((prefix) => job.name.startsWith(prefix));
  return index === -1 ? ORDER.length : index;
};

const { jobs } = await github(`/actions/runs/${runId}/jobs?per_page=100`);
const reported = jobs
  .filter((job) => job.name !== OWN_JOB)
  .sort((a, b) => rank(a) - rank(b) || a.name.localeCompare(b.name));

const rows = [];
const details = [];
for (const job of reported) {
  if (job.conclusion === "skipped") {
    rows.push(`| ${job.name} | — | salteado (el PR no toca esta parte) | — |`);
    continue;
  }
  for (const step of job.steps ?? []) {
    if (SETUP_STEP.test(step.name)) continue;
    rows.push(`| ${job.name} | ${step.name} | ${label(step)} | ${duration(step)} |`);
  }
  // El gate falla porque falló otro job: el detalle está en ese otro.
  if (job.conclusion === "failure" && job.name !== GATE_JOB) {
    const errors = await annotations(job);
    details.push(
      `<details><summary><b>${job.name}</b>: qué falló</summary>\n\n` +
        `${errors.length ? errors.join("\n") : "Sin anotaciones: ver el log."}\n\n` +
        `[Ver el log del job](${job.html_url})\n</details>`,
    );
  }
}

const failed = reported.filter(
  (job) => job.name !== GATE_JOB && ["failure", "cancelled"].includes(job.conclusion),
);
const gate = reported.find((job) => job.name === GATE_JOB);
const title = failed.length
  ? `PR checks: falló ${failed.map((job) => job.name).join(", ")}`
  : gate?.conclusion === "success"
    ? "PR checks: todo en verde"
    : "PR checks: el gate no pasó";

const body = [
  `${MARKER} run:${runId} -->`,
  `## ${title}`,
  "",
  "| Job | Paso | Estado | Duración |",
  "|---|---|---|---|",
  ...rows,
  "",
  ...details,
  "",
  `<sub>Commit ${env.HEAD_SHA?.slice(0, 7) ?? "?"} · [ver el run](${runUrl}) · ` +
    "este comentario se actualiza en cada push</sub>",
].join("\n");

appendFileSync(env.GITHUB_STEP_SUMMARY, `${body}\n`);
console.log(body);

if (!env.PR_NUMBER) {
  console.log("Corrida manual: no hay PR donde comentar.");
  process.exit(0);
}

// Buscar el comentario propio por el marcador. Si es de un run MÁS NUEVO (un push posterior que ya
// terminó), no pisarlo: los run IDs siempre crecen.
const comments = await github(`/issues/${env.PR_NUMBER}/comments?per_page=100`);
const mine = comments.filter((c) => c.body.includes(MARKER)).at(-1);
const theirRun = Number(mine?.body.match(/run:(\d+)/)?.[1] ?? 0);

if (!mine) {
  await github(`/issues/${env.PR_NUMBER}/comments`, { method: "POST", body: JSON.stringify({ body }) });
  console.log("Comentario creado.");
} else if (theirRun > runId) {
  console.log(`El comentario ya es de un run más nuevo (${theirRun}): no se toca.`);
} else {
  await github(`/issues/comments/${mine.id}`, { method: "PATCH", body: JSON.stringify({ body }) });
  console.log("Comentario actualizado.");
}
