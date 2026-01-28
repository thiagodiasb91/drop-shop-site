import { AuthService } from "../../services/auth.service.js"
import { KardexService } from "../../services/kardex.service.js"

export function getData() {
  return {
    user: null,
    kardexData: [],
    loading: false,
    error: null,

    async init() {
      const me = await AuthService.me()

      if (!me) {
        window.location.href = "/pages/auth/login.html"
        return
      }

      this.user = me.user
      await this.loadKardex()
    },

    async loadKardex(filters = {}) {
      this.loading = true
      this.error = null

      try {
        const data = await KardexService.getKardex(filters)
        this.kardexData = data.items || []
        return this.kardexData
      } catch (error) {
        this.error = error.message
        console.error("Erro ao carregar kardex:", error)
        throw error
      } finally {
        this.loading = false
      }
    },

    async loadKardexByProduct(productId, filters = {}) {
      return this.loadKardex({ productId, ...filters })
    },

    async logout() {
      await AuthService.logout()
      window.location.reload()
    }
  }
}
