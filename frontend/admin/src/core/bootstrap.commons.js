import { AuthService } from "../services/auth.service";

export function commons() {
  console.log('bootstrap.module.loaded');
  return {
    logged: null,
    sidebarOpen: window.innerWidth >= 769,
    async init() {
      console.log('bootstrap.init.called');
      this.logged = await AuthService.me()
      console.log('bootstrap.init.logged', this.logged);
    },
    async logout() {
      console.log('bootstrap.logout.called');
      const confirm = window.confirm('Tem certeza que deseja sair?')
      if (!confirm) return
      await AuthService.logout()
      window.location.reload()
    },
    spaNavigate(e, path) {
      console.log('bootstrap.spaNavigate.called', e);
      e.preventDefault();
      import("/src/core/router.js").then(m => m.navigate(path));
    }
  }
}