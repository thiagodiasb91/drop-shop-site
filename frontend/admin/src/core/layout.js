import logger from "../utils/logger";

logger.local("layout.module.loaded");

const layouts = {
  "authenticaded": () => import("../layout/layout-authenticated.html?raw"),
  "public": () => import("../layout/layout-public.html?raw"),
};

async function getLayout(layout) {
  try {
    const l = (await layouts[layout]()).default;
    return l;
  } catch (err) {
    throw new Error(`Falha ao carregar o ficheiro de layout: ${layout} - Erro: ${err.message}`, { cause: err });
  }
}

export async function loadLayout(app, route) {
  try {
    logger.local("layout.loadLayout.request", route);

    Alpine.data("setPageTitle", () => ({
      pageTitle: route.title ?? "Nada"
    }));

    if (route.layout) {
      app.innerHTML = await getLayout(route.layout);
    }
    else if (route.public) {
      app.innerHTML = await getLayout("public");
      logger.local("layout.loadLayout.public");
    } else {
      app.innerHTML = await getLayout("authenticaded");
      logger.local("layout.loadLayout.authenticated");
    }

    const content = document.getElementById("content");
    if (!content) throw new Error("Elemento #content não encontrado no layout.");

    if (!route.js){
      Alpine.initTree(app);
      return;
    }

    logger.local("layout.loadLayout.importing", route.js);
    const module = await route.js().catch(error => {
      logger.error("layout.loadLayout.errorImporting", error);
      throw new Error(`Falha ao importar o módulo da rota - ${route.title}:${route.js}- Erro: ${error.message}`);
    });
    logger.local("layout.loadLayout.getData", module?.getData);

    if (module.render) {
      logger.local("layout.loadLayout.initializingAlpineForContent");
      content.innerHTML = module.render();
    }

    if (module.getData) {
      logger.local("layout.loadLayout.initializingGetData");
      Alpine.data("getData", () => module.getData());
    }

    logger.local("layout.loadLayout.initializingLayout");

    Alpine.initTree(content);
  }
  catch (error) {
    logger.error("ERRO_CRITICO_LAYOUT:", error);

    const app = document.getElementById("app");
    app.innerHTML = `
      <div class="flex flex-col items-center justify-center h-screen bg-[#0f172a] text-white p-6 text-center">
        <div class="bg-rose-500/10 p-5 rounded-full mb-6">
          <i class="ph ph-warning-octagon text-6xl text-rose-500"></i>
        </div>
        <h1 class="text-2xl font-bold mb-2">Ops! Algo correu mal</h1>
        <p class="text-slate-400 mb-8 max-w-md">
          ${error.message || "Ocorreu um erro inesperado ao montar a página."}
        </p>
        <div class="flex gap-4">
          <button onclick="window.location.reload()" 
            class="px-6 py-3 bg-indigo-600 hover:bg-indigo-500 rounded-xl font-bold transition-all shadow-lg shadow-indigo-600/20">
            Tentar Novamente
          </button>
          <a href="/" 
            class="px-6 py-3 bg-slate-800 hover:bg-slate-700 rounded-xl font-bold transition-all">
            Voltar ao Início
          </a>
        </div>
      </div>
    `;
  }

  logger.local("layout.loadLayout.completed");
}


