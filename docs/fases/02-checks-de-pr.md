# Fase 2 — Checks de PR y gate requerido

## Qué se agrega

- **`.github/workflows/pr.yml`**: corre en cada PR, en una máquina limpia de GitHub.
- **Un ruleset sobre `main`** (*Settings → Rules → Rulesets*): exige PR y exige el check `PR gate`.

```
            ┌─> Front (lint, build, tests) ─┐
Detectar ───┤                               ├─> PR gate  (el único check requerido)
 cambios    └─> Back (build, tests) ────────┘
```

| Job | Qué hace |
|---|---|
| Detectar cambios | Compara el PR contra su base con `git diff` y decide qué partes revisar |
| Front | `pnpm install --frozen-lockfile`, lint, type check + build y tests |
| Back | restore, build y los tests |
| PR gate | Corre siempre (`if: always()`) y da un único veredicto: verde si todo lo que corrió pasó o se salteó |

## Por qué un gate

Si la regla exigiera "Front" y "Back", un PR que sólo cambia docs los dejaría salteados, y un check
requerido que nunca reporta deja el PR esperando para siempre (*"Expected — waiting for status"*). Por
eso el workflow **no tiene `paths:`**: corre en todo PR, decide adentro qué revisar, y el gate siempre
reporta. Sólo el gate es requerido.

El nombre `PR gate` es el contrato con la regla: si se renombra el job, todos los PRs se traban hasta
actualizar el ruleset. Y el check recién aparece para elegirlo en el ruleset después de haber corrido
una vez.

## El ruleset

| Opción | Valor |
|---|---|
| Target | La rama por defecto |
| Require a pull request before merging | Sí, 0 aprobaciones (hay una sola persona en el repo) |
| Require status checks to pass | `PR gate` |
| Bypass list | Vacía: no la saltea nadie, ni la dueña del repo |

## El dolor que resuelve

| Rama | Qué rompe | Qué pasa |
|---|---|---|
| `fail/02-broken-build` | El mismo error de tipos de las fases 0 y 1, subido con `--no-verify` | Sin ruleset: el gate en rojo, pero el botón de merge habilitado (`UNSTABLE`). Con ruleset: **merge bloqueado** (`BLOCKED`). Un push directo a `main` se rechaza con `GH013` |
| Un PR que sólo toca docs (este archivo) | Nada | Front y Back en *skipped*, gate en verde, se puede mergear |

## Detalles

- `permissions: contents: read`: el token del workflow sólo lee el repo.
- `concurrency`: un push nuevo al mismo PR cancela la corrida anterior.
- La detección de cambios es un `git diff` de unas líneas, sin acción de terceros (`tj-actions/changed-files`
  sufrió un ataque de supply chain en 2025).
- `HUSKY: 0` en CI: los hooks no hacen falta ahí.
- Las versiones de pnpm y Node salen del proyecto (`packageManager` y `web/.nvmrc`), no de la máquina.
- Si un paso falla, los siguientes del mismo job no corren: con el build roto, los tests quedan en
  *skipped*.
