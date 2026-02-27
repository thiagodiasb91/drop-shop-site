import html from "./store-code.html?raw";
import { router, navigate } from "../../../core/router.js";
import AuthService from "../../../services/auth.service.js";
import stateHelper from "../../../utils/state.helper.js";
import logger from "../../../utils/logger.js";

export function getData() {
  return {
    loading: true,
    message: "",
    async init() {
      const queryParams = new URLSearchParams(window.location.search);
      const pathParams = router.current?.params;

      const email = decodeURIComponent(pathParams.email);

      const code = queryParams.get("code");
      const shopId = queryParams.get("shop_id");

      logger.local(
        "pages.sellers.store-code.confirmCode.request",
        {
          code,
          shopId,
          email
        });
      const confirmResponse = await AuthService.confirmCode(code, shopId, email);
      logger.local("pages.sellers.store-code.confirmCode.response", confirmResponse);

      if (!confirmResponse.ok) {
        logger.error("pages.sellers.store-code.confirmCode.error", confirmResponse.response);
        this.message = "Houve um erro ao tentar configurar a loja. Tente novamente.";
        this.loading = false;
        return;
      }

      const renewResponse = await AuthService.renewToken();
      logger.local("pages.sellers.store-code.renewToken.response", renewResponse);

      if (!renewResponse.ok) {
        logger.error("pages.sellers.store-code.renewToken.error", renewResponse.response);
        this.message = "Houve um erro ao tentar renovar o seu token. Tente novamente.";
        this.loading = false;
        return;
      }

      stateHelper.setSession(renewResponse.response.sessionToken);

      logger.local("pages.sellers.store-code.sessionToken.set");

      await AuthService.me(true);

      navigate("/");
    },
  };
}

export function render() {
  logger.local("page.store-setup.render.loaded");
  return html;
}
