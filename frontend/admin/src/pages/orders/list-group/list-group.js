import html from "./list-group.html?raw"
import { currency } from "../../../utils/format.utils";
import { SupplierService } from "../../../services/suppliers.services";

export function getData() {
  return {
    loading: false,
    openedSupplier: null,

    suppliers: [],

    init() {
      this.fetchSuppliers();
    },

    async fetchSuppliers() {

      this.loading = true;
      
      this.suppliers = await SupplierService.getPayments();
      
      this.loading = false;
    },

    toggleSupplier(id) {
      console.log("list-group.toggleSupplier", id);
      this.openedSupplier = this.openedSupplier === id ? null : id;
    },

    currency(v) {
      return currency(v)
    },

    statusLabel(status) {
      return {
        paid: "Pago",
        pending: "Pendente",
        cancelled: "Cancelado"
      }[status] || status;
    }
  }
}

export function render() {
  console.log("page.orders-list.render.loaded");
  return html;
}
