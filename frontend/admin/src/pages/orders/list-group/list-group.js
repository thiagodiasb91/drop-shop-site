import html from "./list-group.html?raw"
import { currency } from "../../../utils/format.utils";


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
      await new Promise(r => setTimeout(r, 400));

      this.suppliers = [
        {
          id: "SUP-01",
          name: "Fornecedor Alpha",
          totalDue: 1290.80,
          productsCount: 12,
          orders: [
            {
              sku: "SKU-001",
              qty: 3,
              unitPrice: 99.90,
              orderId: "ORD-1001",
              date: "2026-01-10",
              status: "paid"
            },
            {
              sku: "SKU-002",
              qty: 5,
              unitPrice: 49.90,
              orderId: "ORD-1002",
              date: "2026-01-12",
              status: "pending"
            }
          ]
        },
        {
          id: "SUP-02",
          name: "Fornecedor Beta",
          totalDue: 430.00,
          productsCount: 5,
          orders: [
            {
              sku: "SKU-010",
              qty: 2,
              unitPrice: 215.00,
              orderId: "ORD-1003",
              date: "2026-01-11",
              status: "pending"
            }
          ]
        }
      ];

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
