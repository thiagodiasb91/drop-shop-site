import { routes } from "./route.registry.js";
import AuthService from "../services/auth.service.js";

console.log("core.menu.load")

export function menu() {
  return {    
    sidebarOpen: false,
    menuItems: [],
    async init() {
      console.log("core.menu.init.called")
      const user = await AuthService.me();
      this.menuItems = this.buildMenu(user)
      window.addEventListener('popstate', () => {
        this.currentPath = window.location.pathname;
      });
      console.log("core.menu.init.response", this.menuItems)
    },
    buildMenu(user) {
      console.log("core.menu.buildMenu.called", routes)
      // converte registry em array para percorrer, filtrar e mapear para { path, title }
      const entries = Object.entries(routes).filter(([path, route]) => {
        console.log("core.menu.buildMenu.route", path, route)
        // ignora páginas públicas
        if (route.public) {
          console.log("core.menu.buildMenu.route.public")
          return false;
        }

        if (route.hideMenu) {
          console.log("core.menu.buildMenu.route.hideMenu")
          return false;
        }

        // ignora rota curinga
        if (path === "*") {
          console.log("core.menu.buildMenu.route.wildcard")
          return false;
        }

        // se não tem allowedRoles, todo mundo pode ver
        if (!route.allowedRoles) {
          console.log("core.menu.buildMenu.route.noRoles")
          return true;
        }

        // admin vê tudo
        // if (user?.role === "admin") {
        //   console.log("core.menu.buildMenu.route.admin")
        //   return true;
        // }

        // se o userRole está na lista da rota
        const included = route.allowedRoles.includes(user?.role);
        console.log("core.menu.buildMenu.route.userRole", included, route.allowedRoles, user?.role)
        return included;
      });

      // Map to desired shape and return
      const menu = entries.map(([path, route]) => ({ path, title: route.title ?? path }));
      console.log("core.menu.buildMenu.result", menu);
      return menu;
    },
  }
}

