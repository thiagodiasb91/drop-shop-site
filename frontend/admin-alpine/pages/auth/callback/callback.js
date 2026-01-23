import { AuthService } from "../../../services/auth.service.js"

console.log("CALLBACK MODULE LOADED");
let alreadyCalled = false;
export function getData() {
  return {    
    message: "Autenticando...",

    async init() {
      console.log("Iniciando callback authentication...");
      if (alreadyCalled) {
        console.log("Callback já foi chamado, evitando chamada duplicada.");
        return;
      }

      alreadyCalled = true;

      const code =
        new URLSearchParams(window.location.search)
          .get("code")

      if (!code) {
        console.log("Código ausente");
        this.message = "Código ausente"
        return
      }

      try {
        console.log("Chamando AuthService.callback...");        
        await AuthService.callback(code)
        this.message = "Login realizado"
        console.log("Redirecionando para dashboard...");
        window.location.href = "/pages/dashboard/dashboard.html"
      } catch (e) {
        console.error("callback.js: Erro ao autenticar", e);
        this.message = "Erro ao autenticar"
      }
      finally {
        alreadyCalled = false;
      }
    },
    async goToLogin() {
      console.log("Redirecionando para login...");
      window.location.href = "/pages/auth/login.html"
    }
  }
}

