import { ENV } from "../config/env.js"
import { AuthService } from "./auth.service.js"

export const UserService = {
  async getProfile() {
    try {
      const res = await fetch(`${ENV.API_BASE_URL}/bff/user/profile`, {
        credentials: "include",
        headers: {
          ...AuthService.getAuthHeader()
        },
      })

      if (!res.ok) {
        throw new Error(`Erro ao buscar perfil: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("UserService.getProfile error:", error)
      throw error
    }
  },

  async updateProfile(profileData) {
    try {
      const res = await fetch(`${ENV.API_BASE_URL}/bff/user/profile`, {
        method: "PUT",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          ...AuthService.getAuthHeader()
        },
        body: JSON.stringify(profileData),
      })

      if (!res.ok) {
        throw new Error(`Erro ao atualizar perfil: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("UserService.updateProfile error:", error)
      throw error
    }
  },
}