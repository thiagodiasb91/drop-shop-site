import { ENV } from "../config/env.js"

export const AuthService = {
  async callback(code) {
    const res = await fetch(
      `${ENV.API_BASE_URL}/bff/auth/callback`,
      {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ code }),
      }
    )

    if (!res.ok) {
      throw new Error("Auth callback failed")
    }

    return res.json()
  },

  async me() {
    const res = await fetch(
      `${ENV.API_BASE_URL}/bff/me`,
      { credentials: "include" }
    )

    if (res.status === 401) return null
    return res.json()
  },

  async logout() {
    await fetch(
      `${ENV.API_BASE_URL}/bff/auth/logout`,
      {
        method: "POST",
        credentials: "include",
      }
    )
  },
}
