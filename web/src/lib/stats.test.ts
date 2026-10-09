import { describe, expect, it } from "vitest";
import { strongestStat } from "./stats";

const base = {
  hp: 60,
  attack: 65,
  defense: 60,
  specialAttack: 130,
  specialDefense: 75,
  speed: 110,
};

describe("strongestStat", () => {
  it("devuelve la estadística más alta con su nombre en español", () => {
    expect(strongestStat(base)).toEqual({
      key: "specialAttack",
      label: "Ataque especial",
      value: 130,
    });
  });

  it("ante un empate se queda con la primera", () => {
    expect(
      strongestStat({
        hp: 70,
        attack: 70,
        defense: 50,
        specialAttack: 50,
        specialDefense: 50,
        speed: 50,
      }).key,
    ).toBe("hp");
  });
});
