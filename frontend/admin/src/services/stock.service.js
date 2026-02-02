import { ENV } from "../config/env.js"
import { AuthService } from "./auth.service.js"

export const StockService = {
  async updateStock(supplierId, sku, quantity) {
    try {
      if (!supplierId || !sku || quantity === undefined) {
        throw new Error("Parâmetros obrigatórios: supplierId, sku e quantity")
      }

      const url = `${ENV.API_BASE_URL}/suppliers/${supplierId}/sku/${sku}/stock`

      const res = await fetch(url, {
        method: "PUT",
        credentials: "include",
        headers: { 
          "Content-Type": "application/json",
          ...AuthService.getAuthHeader()
        },
        body: JSON.stringify({ quantity: Number(quantity) }),
      })

      if (!res.ok) {
        throw new Error(`Erro ao atualizar estoque: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("Erro ao atualizar estoque:", error)
      throw error
    }
  },

  async bulkUpdateStock(updates = []) {
    try {
      const results = []
      
      for (const update of updates) {
        try {
          const result = await this.updateStock(
            update.supplierId,
            update.sku,
            update.quantity
          )
          results.push({ ...update, success: true, result })
        } catch (error) {
          results.push({ ...update, success: false, error: error.message })
        }
      }

      return results
    } catch (error) {
      console.error("Erro ao atualizar estoque em lote:", error)
      throw error
    }
  },
}
