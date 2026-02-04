import { routes } from "./route.registry.js";
import { AuthService } from "../services/auth.service.js";

const user = await AuthService.me();

export function menu() {
  return {    
    sidebarOpen: false,
    menuItems: [],
    async init() {
      console.log("core.menu.init.called")
      this.menuItems = this.buildMenu()
      console.log("core.menu.init.items", this.menuItems)
    },
    buildMenu() {
      console.log("core.menu.buildMenu.called", routes)
      // converte registry em array para percorrer, filtrar e mapear para { path, title }
      const entries = Object.entries(routes).filter(([path, route]) => {
        console.log("core.menu.buildMenu.route", path, route)
        // ignora páginas públicas
        if (route.public) {
          console.log("core.menu.buildMenu.route.public")
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
        if (user?.role === "admin") {
          console.log("core.menu.buildMenu.route.admin")
          return true;
        }

        // se o userRole está na lista da rota
        const included = route.allowedRoles.includes(user?.roles);
        console.log("core.menu.buildMenu.route.userRole", included, route.allowedRoles, user?.roles)
        return included;
      });

      // Map to desired shape and return
      const menu = entries.map(([path, route]) => ({ path, title: route.title ?? path }));
      console.log("core.menu.buildMenu.result", menu);
      return menu;
    },
  }
}

