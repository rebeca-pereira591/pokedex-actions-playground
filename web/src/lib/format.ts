import type { Weakness } from "../api/types";

const decimal = new Intl.NumberFormat("es", { maximumFractionDigits: 1 });

// 0.7 -> "0,7 m"; 460 -> "460 kg"
export function formatMeasure(value: number, unit: "m" | "kg"): string {
  return `${decimal.format(value)} ${unit}`;
}

// 94 -> "#0094"
export function dexNumber(id: number): string {
  return `#${String(id).padStart(4, "0")}`;
}

const ROMAN = ["", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"];

export function romanGeneration(generation: number): string {
  return ROMAN[generation] ?? String(generation);
}

export interface WeaknessGroup {
  multiplier: number;
  symbol: string;
  label: string;
  types: Weakness["type"][];
}

const GROUPS: ReadonlyArray<Omit<WeaknessGroup, "types">> = [
  { multiplier: 4, symbol: "×4", label: "Muy débil" },
  { multiplier: 2, symbol: "×2", label: "Débil" },
  { multiplier: 0.5, symbol: "×½", label: "Resiste" },
  { multiplier: 0.25, symbol: "×¼", label: "Resiste mucho" },
  { multiplier: 0, symbol: "×0", label: "Inmune" },
];

// Agrupa las debilidades del backend por multiplicador, en el orden de la tabla. Los grupos vacíos
// no se devuelven.
export function groupWeaknesses(weaknesses: Weakness[]): WeaknessGroup[] {
  return GROUPS.map((group) => ({
    ...group,
    types: weaknesses.filter((w) => w.multiplier === group.multiplier).map((w) => w.type),
  })).filter((group) => group.types.length > 0);
}

/** true si el Pokémon tiene exactamente ese total de estadísticas base */
export function hasTotal(total: number, expected: string) {
  return total === Number(expected);
}
