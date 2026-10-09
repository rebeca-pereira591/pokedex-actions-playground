# Fase 1 — Hooks locales

## Qué se agrega

Hooks de git que corren en la máquina de quien programa, antes de que el código salga de ahí:

| Hook | Cuándo | Qué corre | Si falla |
|---|---|---|---|
| `pre-commit` | En cada `git commit` | Biome sobre los archivos de `web/` del commit, `dotnet format whitespace` sobre los `.cs` de `api/` del commit | Lo que se puede corregir se corrige y entra al commit. Si queda un error que no se arregla solo, el commit se frena |
| `pre-push` | En cada `git push`, sólo si lo que se sube toca `web/` | `tsc -b` (type check) y `biome check` del front | El push se cancela |

Las piezas:

- **husky** instala los hooks: `pnpm install` corre el script `prepare` de la raíz, que apunta git a
  `.husky/`. Quien clona y hace `pnpm install` los tiene; quien no, no.
- **lint-staged** corre las herramientas sólo sobre los archivos que están en el commit, no sobre todo
  el repo, y vuelve a agregar al commit lo que la herramienta corrigió. Hay una config por carpeta
  (`web/lint-staged.config.mjs` y `api/lint-staged.config.mjs`): cada una corre desde su carpeta y sólo
  si el commit toca archivos de ahí.
- **El workspace de pnpm pasó a la raíz** (`pnpm-workspace.yaml`, con `web` como paquete adentro). Así
  un solo `pnpm install` desde la raíz instala el front y activa los hooks.

## Por qué cada hook hace lo que hace

- **El pre-commit sólo formatea.** Tiene que ser rápido (unos segundos), si no la gente se acostumbra a
  saltearlo. Por eso del back corre sólo `whitespace`, que no necesita compilar. Los analizadores de C#
  llegan en la fase 4.
- **El pre-push chequea tipos.** Es más lento (necesita mirar todo el proyecto, no sólo los archivos
  tocados), y es justo lo que se escapó en la fase 0: los tests pasaban y el build no.
- **Ninguno corre los tests.** Serían demasiado lentos para cada commit, y depender de que cada uno los
  corra es justamente el problema. Los tests van al CI en la fase 2.

## El dolor que muestra

Los hooks viven en la máquina de cada uno, y **se pueden saltear**:

- `git commit --no-verify` y `git push --no-verify` no corren ningún hook.
- Quien no hizo `pnpm install` (o lo hizo con `--ignore-scripts`) no tiene hooks.
- Lo que se edita desde la web de GitHub tampoco pasa por ningún hook.

| Rama | Qué rompe | Qué pasa |
|---|---|---|
| `fail/01-no-verify` | El mismo error de tipos de `fail/00-broken-build` | Sin `--no-verify`, el push se cancela. Con `--no-verify`, sube igual, el PR no tiene checks y se mergea. **Esa es la motivación de la fase 2** |

## Cómo probarlo

```bash
# un archivo mal formateado se arregla solo al commitear
echo 'export const   x = [1,2,3]' >> web/src/lib/format.ts
git add web/src/lib/format.ts
git commit -m "test: messy format"     # el commit entra, ya formateado

# un error de tipos frena el push: en PokemonCard.tsx, <TypeChip ... size="small" />
git commit -am "feat(web): use small type chips on cards"
git push                               # pre-push: hay errores de tipos. Push cancelado.
git push --no-verify                   # ...y así sube igual
```

## Trampas encontradas al armarla

- **`dotnet format` ignora sin avisar las rutas absolutas** con `/`, que son las que pasa lint-staged:
  termina sin error y sin cambiar nada. La config de `api/` las convierte en relativas.
- **Biome resuelve `vcs.root` desde la carpeta donde se lo corre**, no desde su config. Corrido desde la
  raíz, buscaba el `.gitignore` en la carpeta de arriba del repo. Por eso cada carpeta tiene su config de
  lint-staged, que corre desde ahí.
- **El pre-push revisa los archivos del disco, no los commits que se suben.** Si hay cambios sin
  commitear, el chequeo los incluye. Es una limitación normal de los hooks; el CI de la fase 2 revisa
  exactamente lo que se subió.
- **Los hooks no usan el `pnpm` global**, que en cada máquina puede ser otra versión (en esta era la 9).
  Llaman directo a los binarios de `node_modules`.
