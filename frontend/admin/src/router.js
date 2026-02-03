import { requireAuth } from "./auth/auth.guard.js";
import { AuthService } from "./services/auth.service.js";
import { loadLayout } from "./layout.js";

export const router = {
  current: {
    path: '',
    params: {}
  }
};

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
  "/orders": {
    title: "Pedidos",
    js: () => import("./pages/orders/list/list.js"),
  },
  "/orders-group": {
    title: "Pedidos",
    js: () => import("./pages/orders/list-group/list-group.js"),
  },
  "/dashboard": {
    title: "Dashboard",
    js: () => import("./pages/dashboard/dashboard.js"),
  },
  "/suppliers/:supplierId/products": {
    allowedRoles: ['supplier'],
    title: "Fornecedor x Produtos",
    js: () => import("./pages/supplier-products/supplier-products.js"),
  },
  "/admin/users": {
    title: "Admin - Usuários",
    allowedRoles: ['admin'],
    js: () => import("./pages/users/users.js"),
  },
  "/settings": {
    title: "Configurações",
    js: () => import("./pages/settings/settings.js"),
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

export function back() {
  history.back();
}

export async function navigate(path) {
  console.log("router.navigate.request", path);
  history.pushState({}, "", path);
  // window.location.reload();

  await render();
}

function userHasAccessToRoute(route, userRole) {
  if (!route.allowedRoles) return true;

  if (userRole == 'admin') return true;
  
  return route.allowedRoles.includes(userRole);
}

async function render() {
  console.log("router.render.request", location.pathname);
  const path = location.pathname;
  const {route, params} = matchRoute(path);
  router.current.path = path;
  router.current.params = params;

  const user = await AuthService.me()

  if (!route.public) {
    console.log("router.render.requiringAuth");
    await requireAuth(user);
  }

  if (!userHasAccessToRoute(route, user.roles)) {
    Alpine.store('toast').open(
      'Você não tem permissão para acessar essa página',
      'error'
    )
    navigate('/')
    return
  }

  const app = document.getElementById("app");
  app.innerHTML = "";

  console.log("router.render.loadingLayoutForRoute", path, route);
  await loadLayout(app, route);
}

function matchRoute(path) {
  for (const routePath in routes) {
    if (routePath === "*") continue;

    const paramNames = [];
    const regexPath = routePath
      .replace(/:([^/]+)/g, (_, key) => {
        paramNames.push(key);
        return "([^/]+)";
      });

    const regex = new RegExp(`^${regexPath}$`);
    const match = path.match(regex);

    if (match) {
      const params = {};
      paramNames.forEach((name, i) => {
        params[name] = match[i + 1];
      });

      return {
        route: routes[routePath],
        params,
      };
    }
  }

  return {
    route: routes["*"],
    params: {},
  };
}
