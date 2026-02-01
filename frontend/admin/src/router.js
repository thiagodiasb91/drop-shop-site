import { requireAuth } from "./auth/auth.guard.js";
import { loadLayout } from "./layout/layout.js";

const routes = {
  "/kardex-sku": {
    html: "/src/pages/kardex/kardex-sku.html",
    js: "/src/pages/kardex/kardex-sku.js",
  },
  "/stock": {
    html: "/src/pages/stock/index.html",
    js: "/src/pages/stock/stock-update-init.js",
  },
  "/kardex": {
    html: "/src/pages/kardex/index.html",
    js: "/src/pages/kardex/kardex.js",
  },
  "/payments": {
    html: "/src/pages/payments/index.html",
    js: "/src/pages/payments/payments.js",
  },
  "/login": {
    html: "/src/pages/auth/login/index.html",
    js: "/src/pages/auth/login/login.js",
    public: true,
  },
  "/pages/auth/callback.html": {
    html: "/src/pages/auth/callback/index.html",
    js: "/src/pages/auth/callback/callback.js",
    public: true,
  },
  "/callback": {
    html: "/src/pages/auth/callback/index.html",
    js: "/src/pages/auth/callback/callback.js",
    public: true,
  },
  "/orders": {
    html: "/src/pages/orders/list/index.html",
    js: "/src/pages/orders/list/list.js",
  },
  "/pages/dashboard/dashboard.html": {
    html: "/src/pages/dashboard/index.html",
    js: "/src/pages/dashboard/dashboard.js",
  },
};

export async function initRouter() {
  console.log("router.initRouter.request", location.pathname);
  window.addEventListener("popstate", render);
  await render();
}

export async function navigate(path) {
  console.log("router.navigate.request", path);
  history.pushState({}, "", path);
  await render();
}

async function render() {
  console.log("router.render.request", location.pathname);
  const path = location.pathname;
  const route = routes[path] || routes["/login"];

  if (!route.public) {
    console.log("Rota protegida, verificando autenticação...");
    await requireAuth();
  }

  const app = document.getElementById("app");
  app.innerHTML = "";

  console.log("Carregando layout para a rota:", path, route);
  await loadLayout(app, route);
}
