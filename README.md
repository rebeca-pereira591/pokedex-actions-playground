# Pokédex Actions Lab

Una Pokédex hecha con **.NET + React** que arranca como trabaja mucha gente —sin hooks, sin GitHub
Actions, sin plantillas y sin reglas sobre `main`— y se va equipando **fase por fase** con todo lo que
GitHub ofrece gratis.

Cada fase tiene dos pull requests: el que agrega la herramienta y otro que **rompe algo a propósito**
para ver la validación en rojo. La idea no es que todo funcione perfecto, sino **ver cada cosa fallar y
después pasar**.

## Las fases

| Fase | Qué agrega | Qué problema atrapa | PR | PR de falla |
|---|---|---|---|---|
| 0 | La app, con tests, sin nada más | Ninguno: todavía nada lo impide | — | por abrir |
| 1 | Hooks locales | Errores antes del commit… salvo que alguien los saltee | | |
| 2 | Checks de PR y un *gate* requerido | Que se mergee código que no compila o con tests rotos | | |
| 3 | Comentario con la tabla de resultados | Tener que abrir los logs para saber qué falló | | |
| 4 | Cobertura mínima y analizadores de C# | Código sin tests y estilo inconsistente | | |
| 5 | Seguridad: secretos, CodeQL, dependencias | Secretos commiteados y librerías vulnerables | | |
| 6 | Plantillas y convenciones | PRs e issues sin la información necesaria | | |
| 7 | Bots: etiquetas, reviewers, inactivos | Un repo que no se ordena solo | | |
| 8 | Preview por PR en GitHub Pages | Tener que clonar para ver un cambio visual | | |
| 9 | Tests end-to-end y el comando `/e2e` | Flujos rotos que los unit tests no ven | | |
| 10 | Review con IA por etiqueta | Bugs sutiles que los tests no cubren | | |
| 11 | Releases automáticos | Versionar y publicar a mano | | |
| 12 | Extras | Lo que vaya surgiendo | | |

El detalle de cada fase está en [`docs/fases/`](docs/fases/).

## La app

- **Listado** con búsqueda por nombre o número, filtros por tipo y generación, y paginación.
- **Ficha** con el hexágono de las 6 estadísticas, debilidades y resistencias calculadas
  (×4, ×2, ×½, ×¼, ×0), cadena evolutiva con cómo evoluciona cada uno, y el grito.
- Cada Pokémon brilla con el color de su artwork. Los legendarios son de oro y los míticos de metal
  rosa y morado.

## Cómo está armado

```text
api/        .NET 10: minimal API que junta y traduce lo que da PokeAPI (con caché)
web/        React 19 + Vite + TypeScript: la app, sus componentes y su Storybook
.husky/     Hooks de git: pre-commit (formato) y pre-push (type check y lint del front)
fixtures/   Respuestas de PokeAPI grabadas: los tests nunca llaman a la API real
scripts/    record-fixtures.mjs (graba fixtures) y export-mocks.mjs (genera los mocks del front)
docs/       Una página por fase
```

El front nunca habla con PokeAPI: le pide al backend, que junta varias respuestas en la forma que
necesita cada pantalla y calcula las debilidades y la evolución.

## Cómo correrlo

Hace falta **.NET 10**, **Node 24** y **pnpm 12** (`npm install -g pnpm@12`).

```bash
# backend, contra la PokeAPI real (http://localhost:5032)
dotnet run --project api/Pokedex.Api

# backend sin internet, sólo con los fixtures (37 Pokémon)
dotnet run --project api/Pokedex.Api --launch-profile offline

# dependencias del front y hooks de git: una sola vez, desde la raíz del repo
pnpm install

# frontend (http://localhost:5173), con /api apuntando al backend local
cd web
pnpm dev

# frontend sin backend: MSW responde /api en el navegador
pnpm dev:mocks

# componentes sueltos
pnpm storybook
```

## Tests

```bash
dotnet test --solution api/Pokedex.slnx   # reglas del dominio y endpoints, contra los fixtures
cd web
pnpm test                                 # funciones puras, componentes y páginas, con MSW
pnpm build                                # type check + build
pnpm lint                                 # Biome
pnpm coverage                             # tests con cobertura (mínimo 90 % de líneas)
```

Desde la fase 1, `pnpm install` activa **hooks de git**: cada commit formatea lo que se commitea
(Biome en el front, `dotnet format` en el back) y cada push corre el type check y el lint del front.
Se pueden saltear con `--no-verify`, y eso es lo que muestra la fase. Detalle en
[`docs/fases/01-hooks-locales.md`](docs/fases/01-hooks-locales.md).

## Datos e imágenes

Los datos vienen de [PokeAPI](https://pokeapi.co/), que es gratuita y no pide clave. Las imágenes y
los gritos se enlazan desde los repositorios de PokeAPI; no se copian a este repo. Pokémon y sus
nombres son marcas de Nintendo, Game Freak y The Pokémon Company. Este es un proyecto educativo sin
fines de lucro.
