import html from "./callback.html?raw";
import AuthService from "../../../services/auth.service.js"
import { navigate } from "../../../core/router.js"; 
import CacheHelper from "../../../utils/cache.helper.js";
import stateHelper from "../../../utils/state.helper.js";

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

      console.log("page.callback.init.callingAuthService");
      const callbackResponse = await AuthService.callback(code)
      console.log("page.callback.init.callbackResponse", callbackResponse);


      if (!callbackResponse.ok) {
        console.error("page.callback.init.error", callbackResponse.response);
        stateHelper.removeSession()
        this.message = "Erro ao autenticar"
        this.executing = false
        return
      }

      stateHelper.setSession(callbackResponse.response.sessionToken)
      await stateHelper.refresh()

      this.message = "Login realizado"
      console.log("page.callback.init.redirecting");
      navigate("/")
      this.executing = false
    },
    async goToLogin() {
      console.log("page.callback.goToLogin.redirecting");
      navigate("/login")
      this.executing = false
    }
  }
}

export function render() {
  console.log("page.callback.render.loaded");
  return html;
}