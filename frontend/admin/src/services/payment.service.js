import { ENV } from "../config/env.js"

export const PaymentService = {
  async getPaymentQueue(sellerId) {
    try {
      if (!sellerId) throw new Error("sellerId é obrigatório")

      const url = `${ENV.API_BASE_URL}/bff/payment/${sellerId}`

      const res = await fetch(url, {
        credentials: "include",
      })

      if (!res.ok) {
        throw new Error(`Erro ao buscar fila de pagamentos: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("PaymentService.getPaymentQueue error:", error)
      throw error
    }
  },
}
