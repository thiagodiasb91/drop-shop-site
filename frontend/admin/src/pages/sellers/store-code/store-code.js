import html from "./store-code.html?raw"
import { router, navigate } from "../../../core/router.js";
import AuthService from "../../../services/auth.service.js"
import CacheHelper from "../../../utils/cache.helper.js";

export function getData() {
  return {
    loading: true,
    message: "",
    async init() {
      const queryParams = new URLSearchParams(window.location.search);
      const pathParams = router.current?.params

      const email = decodeURIComponent(pathParams.email)

      const code = queryParams.get('code');
      const shopId = queryParams.get('shop_id');

      console.log(
        "pages.sellers.store-code.confirmCode.request",
        {
          code,
          shopId,
          email
        })
      const confirmResponse = await AuthService.confirmCode(code, shopId, email)
      console.log("pages.sellers.store-code.confirmCode.response", confirmResponse)

      if (!confirmResponse.ok) {
        console.error("pages.sellers.store-code.confirmCode.error", confirmResponse.response)
        this.message = "Houve um erro ao tentar configurar a loja. Tente novamente."
        this.loading = false
        return
      }

      const renewResponse = await AuthService.renewToken()
      console.log("pages.sellers.store-code.renewToken.response", renewResponse)

      if (!renewResponse.ok) {
        console.error("pages.sellers.store-code.renewToken.error", renewResponse.response)
        this.message = "Houve um erro ao tentar renovar o seu token. Tente novamente."
        this.loading = false
        return
      }

      CacheHelper.set("session_token", renewResponse.response.sessionToken)

      console.log("pages.sellers.store-code.sessionToken.set")

      await AuthService.me(true)

      navigate('/')
    },
  }
}

export function render() {
  console.log("page.store-setup.render.loaded");
  return html;
}
