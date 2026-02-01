import { AuthService } from "../../services/auth.service.js"
import { PaymentService } from "../../services/payment.service.js"

export function getData() {
  return {
    user: null,
    sellerId: null,
    payments: [],
    loading: false,
    error: null,
    success: null,

    async init() {
      try {
        const me = await AuthService.me()
        if (!me) {
          window.location.href = "/pages/auth/login.html"
          return
        }

        this.user = me.user
        // tenta encontrar seller id em várias propriedades comuns
        this.sellerId =
          me.user?.seller_id || me.user?.sellerId || me.user?.id || me.seller_id

        if (!this.sellerId) {
          this.error = "Não foi possível identificar o seller_id do usuário"
          return
        }

        await this.loadPayments()
      } catch (err) {
        this.error = err.message
      }
    },

    async loadPayments() {
      this.loading = true
      this.error = null
      this.success = null

      try {
        const data = await PaymentService.getPaymentQueue(this.sellerId)
        this.payments = data || []
        if (this.payments.length === 0) {
          this.success = null
        }
      } catch (err) {
        console.error("Erro ao carregar pagamentos:", err)
        this.error = err.message || "Erro ao carregar pagamentos"
        this.payments = []
      } finally {
        this.loading = false
      }
    },

    formatDate(dateString) {
      try {
        const d = new Date(dateString)
        return d.toLocaleString()
      } catch (e) {
        return dateString
      }
    },

    formatValue(val) {
      return Number(val).toLocaleString(undefined, { style: "currency", currency: "BRL" })
    },

    pay(payment) {
      // aqui ficará a integração com InfinityPay no futuro
      // por enquanto apenas demonstração
      console.log("Solicitação de pagamento:", payment)
      // Exemplo: abrir nova janela ou redirecionar para fluxo de checkout
      alert(`Iniciar pagamento para pedido ${payment.ordersn} — R$ ${payment.value}`)
    },

    async logout() {
      await AuthService.logout()
      window.location.reload()
    },
  }
}
