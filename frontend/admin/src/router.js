import { requireAuth } from "./auth/auth.guard.js";
import { loadLayout } from "./layout.js";

const routes = {
  "/": {
    title: "Home",
    js: () => import("./pages/home/home.js"),
  },
  "/login": {
    title: "Login",
    js: () => import("./pages/auth/login/login.js"),
    public: true,
  },
  "/callback": {
    title: "Callback",
    js: () => import("./pages/auth/callback/callback.js"),
    public: true,
  },
  "/products": {
    title: "Produtos",
    js: () => import("./pages/products/products.js"),
  },
  "/settings": {
    title: "Configurações",
    js: () => import("./pages/settings/settings.js"),
  },
  "/orders": {
    title: "Pedidos",
    js: () => import("./pages/orders/list/list.js"),
  },
  "/dashboard": {
    title: "Dashboard",
    js: () => import("./pages/dashboard/dashboard.js"),
  },
  "*": {
    title: "Página não encontrada",
    js: () => import("./pages/not-found/not-found.js"),
  },
};

export async function initRouter() {
  console.log("router.initRouter.app", document.getElementById("app"));
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
  const route = routes[path] ?? routes["*"];

  if (!route.public) {
    console.log("router.render.requiringAuth");
    await requireAuth();
  }

  const app = document.getElementById("app");
  app.innerHTML = "";

  console.log("router.render.loadingLayoutForRoute", path, route);
  await loadLayout(app, route);
}
