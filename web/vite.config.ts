/// <reference types="vitest/config" />
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  server: {
    // En desarrollo, /api va al backend .NET local
    proxy: { "/api": "http://localhost:5032" },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    css: { modules: { classNameStrategy: "non-scoped" } },
    coverage: {
      provider: "v8",
      // Todo el código de la app, aunque ningún test lo importe: así un archivo nuevo sin tests cuenta.
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
        "src/**/*.test.{ts,tsx}",
        "src/**/*.stories.tsx",
        "src/test/**",
        "src/mocks/**",
        "src/main.tsx",
        "src/**/*.d.ts",
      ],
      reporter: ["text-summary", "text"],
      // Un poco por debajo de lo que hay hoy (91,5 % de líneas): la cobertura no puede bajar.
      thresholds: { lines: 90 },
    },
  },
});
