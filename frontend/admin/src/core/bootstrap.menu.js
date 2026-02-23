import { routes } from "./route.registry.js";
import stateHelper from "../utils/state.helper.js";

console.log("core.menu.load")

export function menu() {
  return {
    sidebarOpen: false,
    menuItems: [],
    currentPath: window.location.pathname,
    async init() {
      console.log("core.menu.init.called")
      const user = stateHelper.user
      this.menuItems = this.buildMenu(user)
      console.log("core.menu.init.response", this.menuItems)
    },
    buildMenu(user) {
      console.log("core.menu.buildMenu.called", routes)
      const entries = Object.entries(routes).filter(([path, route]) => {
        if (route.public || route.hideMenu || path === "*") {
          return false;
        }

        if (!route.allowedRoles) {
          return true;
        }
        return route.allowedRoles.includes(user?.role);
      });

      return entries.reduce((acc, [path, route]) => {
        const group = route.group ?? "Geral";

        if (!acc[group]) {
          acc[group] = [];
        }

        acc[group].push({
          path,
          title: route.title ?? path,
          icon: route.icon ?? 'ph ph-squares-four'
        });

        return acc;
      }, {});
    },
  }
}

