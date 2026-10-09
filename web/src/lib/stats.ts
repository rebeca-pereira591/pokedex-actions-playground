import type { Stats } from "../api/types";

const LABELS: Record<keyof Stats, string> = {
  hp: "PS",
  attack: "Ataque",
  defense: "Defensa",
  specialAttack: "Ataque especial",
  specialDefense: "Defensa especial",
  speed: "Velocidad",
};

/** La estadística más alta del Pokémon, para mostrarla como su punto fuerte. */
export function strongestStat(stats: Stats) {
  let best: keyof Stats = "hp";
  for (const key of Object.keys(stats) as (keyof Stats)[]) {
    if (stats[key] > stats[best]) {
      best = key;
    }
  }
  return { key: best, label: LABELS[best], value: stats[best] };
}
