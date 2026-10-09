// Chequea la cobertura de líneas de un reporte Cobertura contra un mínimo, con un mensaje claro.
// Coverlet tiene su propio --coverlet-threshold, pero cuando falla sólo dice "error: 1", sin el número
// ni el umbral. Este script lo dice, y lo deja como anotación para que aparezca en el comentario del PR.
//
// Uso: node coverage-check.mjs <carpeta-con-reportes> <mínimo-%> <nombre>

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";

const [dir, min, name] = process.argv.slice(2);

// Coverlet le pone una marca de tiempo al nombre: se toma el reporte más nuevo.
const reports = (existsSync(dir) ? readdirSync(dir) : [])
  .filter((file) => /^coverage\.cobertura.*\.xml$/.test(file))
  .map((file) => join(dir, file))
  .sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs);

// Sin reporte es rojo, no "0 % y listo": si el paso anterior no lo generó, algo está mal.
if (reports.length === 0) {
  console.log(`::error title=Cobertura ${name}::No se encontró ningún reporte en ${dir}`);
  process.exit(1);
}

const root = readFileSync(reports[0], "utf8").match(/<coverage [^>]*>/)?.[0] ?? "";
const covered = Number(root.match(/lines-covered="(\d+)"/)?.[1]);
const valid = Number(root.match(/lines-valid="(\d+)"/)?.[1]);
const percent = Math.floor((1000 * covered) / valid) / 10;
const detail = `${percent} % de líneas (${covered} de ${valid}), mínimo ${min} %`;

if (!(percent >= Number(min))) {
  console.log(`::error title=Cobertura ${name}::Cobertura ${name}: ${detail}`);
  process.exit(1);
}
console.log(`Cobertura ${name}: ${detail}. OK.`);
