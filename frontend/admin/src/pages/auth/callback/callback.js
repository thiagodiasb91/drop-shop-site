import html from "./callback.html?raw";
import AuthService from "../../../services/auth.service.js";
import { navigate } from "../../../core/router.js"; 
import stateHelper from "../../../utils/state.helper.js";
import logger from "../../../utils/logger.js";

export function getData() {
  return {
    message: "Autenticando...",
    executing: true,
    called: false,

    async init() {
      logger.local("page.callback.init.executing");
      if (this.called) {
        logger.local("page.callback.init.alreadyCalled");
        return;
      }

      this.called = true;

      const code =
        new URLSearchParams(window.location.search)
          .get("code");

      if (!code) {
        logger.local("page.callback.init.noCode");
        this.message = "Código ausente";
        this.executing = false;
        return;
      }

      logger.local("page.callback.init.callingAuthService");
      const callbackResponse = await AuthService.callback(code);
      logger.local("page.callback.init.callbackResponse", callbackResponse);


      if (!callbackResponse.ok) {
        logger.error("page.callback.init.error", callbackResponse.response);
        stateHelper.removeSession();
        this.message = "Erro ao autenticar";
        this.executing = false;
        return;
      }

      stateHelper.setSession(callbackResponse.response.sessionToken);
      await stateHelper.refresh();

      this.message = "Login realizado";
      logger.local("page.callback.init.redirecting");
      navigate("/");
      this.executing = false;
    },
    async goToLogin() {
      logger.local("page.callback.goToLogin.redirecting");
      navigate("/login");
      this.executing = false;
    }
  };
}

export function render() {
  logger.local("page.callback.render.loaded");
  return html;
}