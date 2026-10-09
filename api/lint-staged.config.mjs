// pre-commit del back: corre sobre los .cs de api/ que están en el commit, desde esta carpeta.
// Sólo "whitespace" (espacios, sangría, finales de línea) porque no necesita compilar. Las reglas de
// estilo con analizadores llegan en la fase 4.
import { relative } from "node:path";

// lint-staged pasa rutas absolutas, y dotnet format las ignora sin avisar: hay que pasarlas relativas.
const relativeTo = (files) =>
  files.map((file) => `"${relative(import.meta.dirname, file)}"`).join(" ");

export default {
  "*.cs": (files) => `dotnet format whitespace Pokedex.slnx --include ${relativeTo(files)}`,
};
