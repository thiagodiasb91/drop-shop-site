import { ENV } from "../config/env.js"

console.log("AuthService.loaded");
console.log("ENV.API_BASE_URL", ENV.API_BASE_URL);

let cachedMe = null;
let cacheExpiresAt = 0;

export const AuthService = {
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

    if (!res.ok) {
      console.error("AuthService.callback.error", res)
      throw new Error("Auth callback failed")
    }

    const response = await res.json()
    console.log("AuthService.callback.response", response)

    sessionStorage.setItem("session_token", response.sessionToken)

    return response
  },

  async me(force  = false) {
    const now = Date.now()

    if (!force && cachedMe && now < cacheExpiresAt) {
      console.log("AuthService.me.cached")
      return cachedMe
    }

    const res = await fetch(
      `${this.basePath}/me`,
      { 
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${sessionStorage.getItem("session_token")}`
        },
       }
    )

    if (res.status === 401) {
      console.log("AuthService.me.unauthorized")
      cachedMe = null
      cacheExpiresAt = 0
      return null
    }

    const response = await res.json()
    // Simula resposta da API
    response.user.roles = 'seller';
    response.expires_at = 15 * 60 * 1000;

    cachedMe = response
    cacheExpiresAt = now + response.expires_at

    console.log("AuthService.me.new-session", response)

    return response
  },

  async logout() {
    sessionStorage.removeItem("session_token")
  },
}
