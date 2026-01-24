import Alpine from "https://unpkg.com/alpinejs@3.x.x/dist/module.esm.js";
import { initRouter } from "./router.js";
import "./styles/site.css"

window.Alpine = Alpine;
import Bootstrap from "./bootstrap.js";
const bootstrap = new Bootstrap(Alpine);
bootstrap.init();
Alpine.start();

document.addEventListener("DOMContentLoaded", async () => {
  await initRouter();
});