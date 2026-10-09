// pre-commit del front: corre sobre los archivos de web/ que están en el commit, desde esta carpeta.
// Biome formatea, ordena imports y corrige lo que puede; lint-staged vuelve a agregar al commit lo
// que se corrigió. Si queda un error que Biome no sabe arreglar solo, el commit se frena.
export default {
  "*.{ts,tsx,js,mjs,json,css}": "biome check --write --no-errors-on-unmatched",
};
