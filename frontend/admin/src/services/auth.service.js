import { ENV } from "../config/env.js"
import CacheHelper from "../utils/cache.helper.js"
import { responseHandler } from "../utils/response.handler.js"

console.log("AuthService.loaded");
console.log("ENV.API_BASE_URL", ENV.API_BASE_URL);



const AuthService = {
  basePath: `${ENV.API_BASE_URL}/auth`,
  async login() {
    console.log("AuthService.login.request")
    window.location.href = `${this.basePath}/login`
  },
  async callback(code) {
    console.log("AuthService.callback.request", code)
    const res = await fetch(
      `${this.basePath}/callback`,
      {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ code }),
      }
    )

    return responseHandler(res)
  },

  async me(force = false) {
    const now = Date.now()
    console.log("AuthService.me.request", force, now)
    const cachedMe = CacheHelper.get("me.data")
    const expiresAt = CacheHelper.get("me.expiresAt")
    const isValid = cachedMe && expiresAt && Date.now() < expiresAt * 1000
    console.log("AuthService.me.cached", cachedMe, expiresAt, isValid)

    if (!force && isValid) {
      console.log("AuthService.me.returningCached", cachedMe)
      return cachedMe
    }

    const sessionToken = CacheHelper.get("session_token")
    console.log("AuthService.me.api.call", sessionToken)

    const res = await fetch(
      `${this.basePath}/me`,
      {
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${sessionToken}`
        },
      }
    )

    if (res.status === 401) {
      console.log("AuthService.me.unauthorized")
      this.logout()
      return null
    }

    if(!res.ok){
      throw new Error("Erro ao buscar usuário")
    }

    const data = (await res.json())
    console.log("AuthService.me.api.response", data)

    // Simula resposta da API
    // data.role = 'supplier';
    // data.role = 'seller';

    CacheHelper.set("me.data", data)
    CacheHelper.set("me.expiresAt", data.session?.exp)

    console.log("AuthService.me.new-session", data, data.session?.exp)

    return data
  },

  async confirmCode(code, shopId, email) {
    console.log("AuthService.confirmCode.request", code, shopId, email)
    const res = await fetch(
      `${this.basePath}/confirm-shopee`,
      {
        method: "POST",
        body: JSON.stringify({ code, shopId, email }),
        headers: { "Content-Type": "application/json" },
      }
    )

    return responseHandler(res)
  },
  
  async renewToken() {
    console.log("AuthService.renewToken.request")
    const res = await fetch(
      `${this.basePath}/renew`,
      {
        method: "POST",
        headers: { 
          "Authorization": `Bearer ${CacheHelper.get("session_token")}`,
          "Content-Type": "application/json" 
        },
      }
    )

    return responseHandler(res)
  },

  async logout() {
    console.log("AuthService.logout.request")
    CacheHelper.remove("session_token")
    CacheHelper.remove("me.data")
    CacheHelper.remove("me.expiresAt")
  },
}

export default AuthService