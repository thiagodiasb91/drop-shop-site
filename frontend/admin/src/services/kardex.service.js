import { ENV } from "../config/env.js"

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
        headers: { "Content-Type": "application/json" },
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
}
