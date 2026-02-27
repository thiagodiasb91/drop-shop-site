import { navigate } from "../core/router";
import { roleName } from "./../utils/format.helper";
import AuthService from "../services/auth.service";
import stateHelper from "../utils/state.helper";
import logger from "../utils/logger.js";

export function commons() {
  logger.local("bootstrap.module.loaded");
  return {
    logged: null,
    sidebarOpen: window.innerWidth >= 769,
    async init() {
      logger.local("bootstrap.commons.init.called");
      
      this.logged = await stateHelper.refresh();
      if (this.logged)
        this.logged.roleName = roleName(this.logged.role);
      logger.local("bootstrap.commons.init.logged", this.logged);
    },
    async logout() {
      logger.local("bootstrap.commons.logout.called");
      const confirm = window.confirm("Tem certeza que deseja sair?");
      if (!confirm) return;
      await AuthService.logout();
      navigate("/login");
    },
    spaNavigate(e, path) {
      e.preventDefault();
      navigate(path);
    }
  };
}