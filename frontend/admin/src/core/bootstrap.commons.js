import AuthService from "../services/auth.service";
import { navigate } from "../core/router"
import { roleName } from "./../utils/format.utils"

export function commons() {
  console.log('bootstrap.module.loaded');
  return {
    logged: null,
    sidebarOpen: window.innerWidth >= 769,
    async init() {
      console.log('bootstrap.commons.init.called');
      const logged = await AuthService.me()
      if (logged)
        logged.roleName = roleName(logged.role)
      console.log('bootstrap.commons.init.logged', logged);
      this.logged = logged
    },
    async logout() {
      console.log('bootstrap.commons.logout.called');
      const confirm = window.confirm('Tem certeza que deseja sair?')
      if (!confirm) return
      await AuthService.logout()
      navigate("/login")
    },
    spaNavigate(e, path) {
      e.preventDefault();
      navigate(path)
    }
  }
}