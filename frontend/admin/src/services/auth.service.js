import { ENV } from "../config/env.js"

const TOKEN_KEY = "auth.token"

function setToken(token) {
  if (token) localStorage.setItem(TOKEN_KEY, token)
  else localStorage.removeItem(TOKEN_KEY)
}

function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

function getAuthHeader() {
  const t = getToken()
  return t ? { Authorization: `Bearer ${t}` } : {}
}

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

    const data = await res.json()
    if (data && data.sessionToken) setToken(data.sessionToken)
    return data
  },

  async me() {
    const res = await fetch(
      `${ENV.API_BASE_URL}/bff/auth/me`,
      {
        credentials: "include",
        headers: { ...getAuthHeader() },
      }
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
        headers: { ...getAuthHeader() },
      }
    )
    setToken(null)
  },

  getAuthHeader,
  getToken,
  setToken,
}
