import { ENV } from "../config/env"
import CacheHelper from "../utils/cache.helper.js"
import stateHelper from "../utils/state.helper.js"
import BaseApi from "./base"

const api = new BaseApi("/auth")

const AuthService = {
  _mePromise: null,
  async login() {
    console.log("AuthService.login.request")
    window.location.href = `${ENV.API_BASE_URL}/login`
  },
  async callback(code) {
    console.log("AuthService.callback.request", code)
    return api.call("/callback", {
      method: "POST",
      credentials: "include",
      body: JSON.stringify({ code }),
    })
  },

  async me(force = false) {
    if (!force) {
      const cachedMe = CacheHelper.get("me.data")
      const expiresAt = CacheHelper.get("me.expiresAt")

      if (cachedMe && expiresAt && Date.now() < expiresAt * 1000) {
        console.log("AuthService.me.cached", cachedMe, expiresAt, isValid)
        console.log("AuthService.me.returningCached", cachedMe)
        return cachedMe
      }
    }

    if (this._mePromise) { return this._mePromise; }

    this._mePromise = (async () => {
      try {
        const res = await api.call("/me")

        if (res.status === 401) {
          console.log("AuthService.me.unauthorized")
          this.logout()
          return null
        }

        if (!res.ok) {
          stateHelper.toast("Erro ao obter dados do usuário", "error")
          return null
        }

        const data = await res.response;
        console.log("AuthService.me.api.response", data)

        // Simula resposta da API
        // data.role = 'supplier';
        // data.role = 'seller';

        CacheHelper.set("me.data", data)
        CacheHelper.set("me.expiresAt", data.session?.exp)

        console.log("AuthService.me.new-session", data, data.session?.exp)

        return data
      }
      finally {
        this._mePromise = null;
      }
    })()
    return this._mePromise
  },

  async confirmCode(code, shopId, email) {
    console.log("AuthService.confirmCode.request", code, shopId, email)
    return api.call("/confirm-shopee", {
      method: "POST",
      body: JSON.stringify({ code, shopId, email })
    })
  },

  async renewToken() {
    console.log("AuthService.renewToken.request")
    return api.call(
      `/renew`,
      {
        method: "POST"
      })
  },

  async logout() {
    console.log("AuthService.logout.request")
    CacheHelper.remove("session_token")
    CacheHelper.remove("me.data")
    CacheHelper.remove("me.expiresAt")
  },
}

export default AuthService