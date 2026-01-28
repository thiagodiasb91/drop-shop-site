import { ENV } from "../config/env.js"

export const StockService = {
  async updateStock(supplierId, productId, sku, quantity) {
    try {
      if (!supplierId || !productId || !sku || quantity === undefined) {
        throw new Error("Parâmetros obrigatórios: supplierId, productId, sku e quantity")
      }

      const url = `${ENV.API_BASE_URL}/suppliers/${supplierId}/product/${productId}/sku/${sku}/stock`

      const res = await fetch(url, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
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
            update.productId,
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
