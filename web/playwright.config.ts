import { defineConfig, devices } from "@playwright/test";

// Tests de punta a punta: un navegador real recorre la app armada como en producción (vite build +
// vite preview), con los mocks de MSW en lugar del backend.
export default defineConfig({
  testDir: "e2e",
  testMatch: "**/*.e2e.ts", // no *.spec.ts: Vitest los tomaría como tests unitarios
  fullyParallel: true,
  forbidOnly: !!process.env.CI, // un test.only olvidado no puede pasar en CI
  retries: process.env.CI ? 1 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: "http://localhost:4173",
    // Si algo falla, queda la evidencia: captura, video y la traza paso a paso.
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "pnpm exec vite build --mode mocks && pnpm exec vite preview --port 4173 --strictPort",
    env: { VITE_HASH_ROUTER: "true" },
    url: "http://localhost:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
