import { expect, test } from "@playwright/test";

// Los datos vienen de los mocks (37 Pokémon grabados de PokeAPI).

test("@smoke el listado muestra la primera página", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Pokédex" })).toBeVisible();
  await expect(page.getByRole("link", { name: /Bulbasaur/ })).toBeVisible();
});

test("buscar un Pokémon y abrir su ficha", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("searchbox", { name: "Buscar" }).fill("gengar");
  await page.getByRole("link", { name: /Gengar/ }).click();

  await expect(page).toHaveURL(/#\/pokemon\/94$/);
  await expect(page.getByRole("heading", { level: 1, name: "Gengar" })).toBeVisible();
  // La regla del dominio que rompió la fase 0: Gengar es inmune a Normal.
  const weaknesses = page.getByRole("region", { name: "Debilidades y resistencias" });
  await expect(weaknesses.getByText("Inmune")).toBeVisible();
  await expect(weaknesses.getByText("Normal")).toBeVisible();
});

test("la ficha se abre directo por URL y vuelve al listado", async ({ page }) => {
  await page.goto("/#/pokemon/94");
  await expect(page.getByRole("heading", { level: 1, name: "Gengar" })).toBeVisible();
  await page.getByRole("link", { name: "← Volver al listado" }).click();
  await expect(page.getByRole("heading", { name: "Pokédex" })).toBeVisible();
});
