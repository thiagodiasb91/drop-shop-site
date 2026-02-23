import Alpine from "https://unpkg.com/alpinejs@3.x.x/dist/module.esm.js";
import collapse from '@alpinejs/collapse'
import Bootstrap from "./core/bootstrap.js";
import { initRouter } from "/src/core/router.js";
import "./styles/main.css"
import stateHelper from "./utils/state.helper.js";

window.Alpine = Alpine;
Alpine.plugin(collapse)
Alpine.start();
const bootstrap = new Bootstrap(Alpine);
bootstrap.init();

stateHelper.refresh().then((user) => {
  console.log("Auth carregado, iniciando roteador...", user);
  initRouter();
});