import AuthService from "../services/auth.service.js";
import { loadLayout } from "./layout.js";
import { routes } from "./route.registry.js";
import {
  canAccessRoute,
  redirectToSellerSetup,
  redirectToSupplierSetup,
  redirectToNewUserPage
} from "./route.control.js";

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
  console.log("router.render.route", route, params);

  router.current = {
    path,
    route,
    params
  }

  const logged = await AuthService.me()

  const requiresAuth = !route.public && !route.skipAuth;

  if (requiresAuth) {
    console.log("router.render.requiringAuth");

    if (!logged) {
      console.log("router.render.not-authenticated");
      navigate("/login");
      return;
    }

    if(await redirectToNewUserPage(logged, route)){
      console.log("router.render.redirectToNewUserPage");
      navigate(`/new-user`)
      return
    }

    if (await redirectToSellerSetup(logged, route, path)) {
      console.log("router.render.redirectToSellerSetup");
      navigate(`/sellers/store/setup`)
      return
    }

    if (await redirectToSupplierSetup(logged, route, path)) {
      console.log("router.render.redirectToSupplierSetup");
      navigate(`/suppliers/setup`)
      return
    }

    if (!canAccessRoute(route, logged.role)) {
      console.log("router.render.not-authorized");
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
