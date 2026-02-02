import { AuthService } from "../../services/auth.service.js"
import { KardexService } from "../../services/kardex.service.js"

export function getData() {
  return {
    user: null,
    sku: "",
    kardexItems: [],
    loading: false,
    error: null,
    success: null,
    currentPage: 1,
    itemsPerPage: 10,

    async init() {
      const me = await AuthService.me()

      if (!me) {
        window.location.href = "/pages/auth/login.html"
        return
      }

      this.user = me.user
    },

    async loadKardex() {
      if (!this.sku) {
        this.error = "Informe um SKU para buscar"
        return
      }

      this.loading = true
      this.error = null
      this.success = null

      try {
        const data = await KardexService.getKardexBySku(this.sku, {
          limit: 100,
          offset: 0
        })
        
        this.kardexItems = data.items || []
        
        if (this.kardexItems.length > 0) {
          this.success = `${this.kardexItems.length} registros encontrados para ${this.sku}`
        } else {
          this.error = "Nenhum registro encontrado para este SKU"
        }
      } catch (error) {
        this.error = error.message
        this.kardexItems = []
        console.error("Erro ao buscar kardex:", error)
      } finally {
        this.loading = false
        this.currentPage = 1
      }
    },

    get paginatedItems() {
      const start = (this.currentPage - 1) * this.itemsPerPage
      const end = start + this.itemsPerPage
      return this.kardexItems.slice(start, end)
    },

    get totalPages() {
      return Math.ceil(this.kardexItems.length / this.itemsPerPage)
    },

    nextPage() {
      if (this.currentPage < this.totalPages) {
        this.currentPage++
      }
    },

    prevPage() {
      if (this.currentPage > 1) {
        this.currentPage--
      }
    },

    formatDate(dateString) {
      return KardexService.formatDate(dateString)
    },

    getOperationBadgeClass(operation) {
      const op = operation?.toLowerCase()
      if (op === "add") return "badge bg-success"
      if (op === "remove") return "badge bg-danger"
      return "badge bg-secondary"
    },

    getTotalQuantityAdded() {
      return this.kardexItems
        .filter(item => item.Operation?.toLowerCase() === "add")
        .reduce((total, item) => total + (item.Quantity || 0), 0)
    },

    getTotalQuantityRemoved() {
      return this.kardexItems
        .filter(item => item.Operation?.toLowerCase() === "remove")
        .reduce((total, item) => total + (item.Quantity || 0), 0)
    },

    getCurrentQuantity() {
      return this.getTotalQuantityAdded() - this.getTotalQuantityRemoved()
    },

    clearSearch() {
      this.sku = ""
      this.kardexItems = []
      this.error = null
      this.success = null
      this.currentPage = 1
    },

    async logout() {
      await AuthService.logout()
      window.location.reload()
    }
  }
}
