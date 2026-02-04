import { requireAuth } from "../auth/auth.guard.js";
import { AuthService } from "../services/auth.service.js";
import { loadLayout } from "./layout.js";
import { routes } from "./route.registry.js";
import { canAccessRoute } from "./router.control.js";

export const router = {
  current: {
    path: '',
    params: {},
    route: null
  }
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

async function render() {
  console.log("router.render.request", location.pathname);
  const path = location.pathname;
  const {route, params} = matchRoute(path);

  router.current = {
    path,
    route,
    params
  }

  const user = await AuthService.me()

  if (!route.public) {
    console.log("router.render.requiringAuth");
    await requireAuth(user);

    if (user && user.role === 'seller'){
      checkSellerStoreId(user)
    }

    if (!canAccessRoute(route, user)) {
      Alpine.store('toast').open(
        'Você não tem permissão para acessar essa página',
        'error'
      )
      navigate('/')
      return
    }
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
