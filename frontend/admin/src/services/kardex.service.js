import { ENV } from "../config/env.js"
import { AuthService } from "./auth.service.js"

export const KardexService = {
  async getKardex(filters = {}) {
    try {
      const queryParams = new URLSearchParams()
      
      if (filters.productId) queryParams.append("productId", filters.productId)
      if (filters.startDate) queryParams.append("startDate", filters.startDate)
      if (filters.endDate) queryParams.append("endDate", filters.endDate)
      if (filters.limit) queryParams.append("limit", filters.limit)
      if (filters.offset) queryParams.append("offset", filters.offset)

      const url = `${ENV.API_BASE_URL}/bff/kardex${queryParams.toString() ? "?" + queryParams.toString() : ""}`

      const res = await fetch(url, {
        method: "GET",
        credentials: "include",
        headers: { 
          "Content-Type": "application/json",
          ...AuthService.getAuthHeader()
        },
      })

      if (!res.ok) {
        throw new Error(`Erro ao buscar kardex: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("Erro ao buscar kardex:", error)
      throw error
    }
  },

  async getKardexByProduct(productId, filters = {}) {
    return this.getKardex({ productId, ...filters })
  },

  async getKardexBySku(sku, filters = {}) {
    try {
      if (!sku) {
        throw new Error("SKU é obrigatório")
      }

      const queryParams = new URLSearchParams()
      
      if (filters.limit) queryParams.append("limit", filters.limit)
      if (filters.offset) queryParams.append("offset", filters.offset)
      if (filters.operation) queryParams.append("operation", filters.operation)
      if (filters.startDate) queryParams.append("startDate", filters.startDate)
      if (filters.endDate) queryParams.append("endDate", filters.endDate)

      const url = `${ENV.API_BASE_URL}/bff/sku/${sku}/kardex${queryParams.toString() ? "?" + queryParams.toString() : ""}`

      const res = await fetch(url, {
        method: "GET",
        credentials: "include",
        headers: { 
          "Content-Type": "application/json",
          ...AuthService.getAuthHeader()
        },
      })

      if (!res.ok) {
        throw new Error(`Erro ao buscar kardex: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("Erro ao buscar kardex:", error)
      throw error
    }
  },

  formatDate(dateString) {
    if (!dateString) return "-"
    const date = new Date(dateString)
    return date.toLocaleString("pt-BR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit"
    })
  },

  getOperationBadgeColor(operation) {
    switch (operation?.toLowerCase()) {
      case "add":
        return "#10b981" // verde
      case "remove":
        return "#ef4444" // vermelho
      default:
        return "#6b7280" // cinza
    }
  }
}
