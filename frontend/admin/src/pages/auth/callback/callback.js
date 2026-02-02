import html from "./index.html?raw";
import { AuthService } from "../../../services/auth.service.js"

export function getData() {
  return {
    message: "Autenticando...",
    executing: true,
    called: false,

    async init() {
      console.log("page.callback.init.executing");
      if (this.called) {
        console.log("page.callback.init.alreadyCalled");
        return;
      }

      this.called = true;

      const code =
        new URLSearchParams(window.location.search)
          .get("code")

      if (!code) {
        console.log("page.callback.init.noCode");
        this.message = "Código ausente"
        this.executing = false
        return
      }

      try {
        console.log("page.callback.init.callingAuthService");
        await AuthService.callback(code)
        this.message = "Login realizado"
        console.log("page.callback.init.redirecting");
        window.location.href = "/"
      } catch (e) {
        console.error("page.callback.init.error", e);
        this.message = "Erro ao autenticar"
      }
      finally {
        this.executing = false
      }
    },
    async goToLogin() {
      console.log("page.callback.goToLogin.redirecting");
      window.location.href = "/pages/auth/login.html"
      this.executing = false
    }
  }
}

export function render() {
  console.log("page.callback.render.loaded");
  return html;
}