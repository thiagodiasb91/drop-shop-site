import Alpine from "https://unpkg.com/alpinejs@3.x.x/dist/module.esm.js";
import collapse from '@alpinejs/collapse'
import { initRouter } from "/src/core/router.js";

window.Alpine = Alpine;
Alpine.plugin(collapse)
Alpine.start();

import Bootstrap from "./core/bootstrap.js";
const bootstrap = new Bootstrap(Alpine);
bootstrap.init();


document.addEventListener("DOMContentLoaded", initRouter);