import Alpine from "https://unpkg.com/alpinejs@3.x.x/dist/module.esm.js";
import collapse from "@alpinejs/collapse";
import Bootstrap from "./core/bootstrap.js";
import { initRouter } from "/src/core/router.js";
import "./styles/main.css";
import stateHelper from "./utils/state.helper.js";
import logger from "./utils/logger.js";


logger.local("App initializing...");

window.Alpine = Alpine;
Alpine.plugin(collapse);
Alpine.start();
const bootstrap = new Bootstrap(Alpine);
bootstrap.init();

stateHelper.refresh().then((user) => {
  logger.local("Auth carregado, iniciando roteador...", user);
  initRouter();
});