import { ENV } from "../config/env.js"
import { AuthService } from "./auth.service.js"

export const PaymentService = {
  async getPaymentQueue(sellerId) {
    try {
      
      const url = `${ENV.API_BASE_URL}/bff/payments`

      const res = await fetch(url, {
        credentials: "include",
        headers: {
          ...AuthService.getAuthHeader()
        },
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

  async processPayment(paymentId, sellerId) {
    try {
      if (!paymentId || !sellerId) {
        throw new Error("paymentId e sellerId são obrigatórios")
      }

      const url = `${ENV.API_BASE_URL}/bff/payment/${sellerId}/${paymentId}`

      const res = await fetch(url, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          ...AuthService.getAuthHeader()
        },
      })

      if (!res.ok) {
        throw new Error(`Erro ao processar pagamento: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("PaymentService.processPayment error:", error)
      throw error
    }
  },
}
