import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import "./styles/fonts";
import "./styles/global.css";

// Con VITE_USE_MOCKS=true (pnpm dev:mocks) MSW responde /api en el navegador: la app anda sin backend.
async function startMocks() {
  if (import.meta.env.VITE_USE_MOCKS !== "true") return;
  const { worker } = await import("./mocks/browser");
  await worker.start({
    onUnhandledFrame: "bypass",
    quiet: true,
    // En una subcarpeta (las previews de Pages) el service worker está ahí, no en la raíz del dominio.
    serviceWorker: { url: `${import.meta.env.BASE_URL}mockServiceWorker.js` },
  });
}

const root = document.getElementById("root");
if (!root) throw new Error("No se encontró el elemento #root");

startMocks().then(() =>
  createRoot(root).render(
    <StrictMode>
      <App />
    </StrictMode>,
  ),
);
