import Alpine from "https://unpkg.com/alpinejs@3.x.x/dist/module.esm.js";
import { initRouter } from "/src/core/router.js";
import "/src/styles/buttons.css"
import "/src/styles/cards.css"
import "/src/styles/layout.css"
import "/src/styles/mobile.css"
import "/src/styles/site.css"
import "/src/styles/table.css"

initRouter();

window.Alpine = Alpine;
Alpine.start();

import Bootstrap from "./core/bootstrap.js";
const bootstrap = new Bootstrap(Alpine);
bootstrap.init();


document.addEventListener("DOMContentLoaded", initRouter);