import { loadLayout } from "./layout.js";
import { routes } from "./route.registry.js";
import RouteGuard from "./route.guard.js";
import stateHelper from "../utils/state.helper.js";

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

let isRedirecting = false;

export async function navigate(path) {
  console.log("router.navigate.request", path, isRedirecting);

  if (location.pathname === path) {
    console.log("router.navigate.isAlreadyOnRoute");
    isRedirecting = false;
    return;
  }

  if (isRedirecting) {
    console.log("router.navigate.isAlreadyRedirecting");
    return;
  }
  isRedirecting = true;

  history.pushState({}, "", path);
  isRedirecting = false;
  await render();

  window.dispatchEvent(new Event('popstate'));
  console.log("router.navigate.completed");
}

async function render() {
  console.log("router.render.request", location.pathname);
  const path = location.pathname;
  const { route, params } = matchRoute(path);
  console.log("router.render.route", route, params, stateHelper.user);

  router.current = {
    path,
    route,
    params
  }

  const logged = stateHelper.user;

  if (route.middlewares && Array.isArray(route.middlewares)) {
    for (const middleware of route.middlewares) {
      const result = await middleware(logged, route, path);

      if (typeof result === 'string') {
        console.log(`[Router] Bloqueado por middleware. Redirecionando para: ${result}`);
        navigate(result);
        return;
      }
    }
  } else if (!route.public && !logged) {
    navigate("/login");
    return;
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
