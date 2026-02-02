import { ENV } from "../config/env.js"

console.log("AuthService.loaded");
console.log("ENV.API_BASE_URL", ENV.API_BASE_URL);

export const AuthService = {
  async login() {
    console.log("AuthService.login.request")
    window.location.href = ENV.API_BASE_URL + "/auth/login";
  },
  async callback(code) {
    console.log("AuthService.callback.request", code)
    const res = await fetch(
      `${ENV.API_BASE_URL}/auth/callback`,
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

  async me() {
    const res = await fetch(
      `${ENV.API_BASE_URL}/auth/me`,
      { 
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${sessionStorage.getItem("session_token")}`
        },
       }
    )

    if (res.status === 401) return null
    return res.json()
  },

  async logout() {
    // await fetch(
    //   `${ENV.API_BASE_URL}/auth/logout`,
    //   {
    //     method: "POST",
    //     credentials: "include",
    //   }
    // )
    sessionStorage.removeItem("session_token")
  },
}
