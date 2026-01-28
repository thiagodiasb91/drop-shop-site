import { StockService } from "../../services/stock.service.js"

export function getStockUpdateData() {
  return {
    form: {
      supplierId: "",
      sku: "",
      quantity: "",
    },
    loading: false,
    error: null,
    success: null,

    resetForm() {
      this.form = {
        supplierId: "",
        sku: "",
        quantity: "",
      }
      this.error = null
      this.success = null
    },

    async updateStock() {
      this.loading = true
      this.error = null
      this.success = null

      try {
        if (!this.form.supplierId || !this.form.sku || !this.form.quantity) {
          throw new Error("Todos os campos são obrigatórios")
        }

        const result = await StockService.updateStock(
          this.form.supplierId,
          this.form.sku,
          this.form.quantity
        )

        this.success = `Estoque atualizado com sucesso! Quantidade: ${this.form.quantity}`
        this.form = {
          supplierId: "",
          productId: "",
          sku: "",
          quantity: "",
        }
        
        return result;
      } catch (error) {
        this.error = error.message
        console.error("Erro ao atualizar estoque:", error)
        throw error
      } finally {
        this.loading = false
      }
    },

    handleInputChange(fieldName, value) {
      this.form[fieldName] = value
    },

    handleFormSubmit(event) {
      event.preventDefault()
      return this.updateStock()
    },
  }
}
