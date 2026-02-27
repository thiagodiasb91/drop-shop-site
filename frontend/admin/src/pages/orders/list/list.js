import html from "./list.html?raw";
import logger from "./utils/logger.js";


export function getData() {
  return {
    orders: [],
    loading: false,
    filter: "",
    page: 1,
    pageSize: 10,
    totalPages: 1,
    totalItens: 0,

    init() {
      this.fetchOrders();
    },

    calculatePagination(orders) {
      this.totalItens = orders.length;
      this.totalPages = Math.ceil(this.totalItens / this.pageSize);

      this.start = (this.page - 1) * this.pageSize;
      this.end = this.start + this.pageSize;
      logger.local("calculatePagination", this.totalItens, this.totalPages, this.start, this.end);
    },

    async fetchOrders() {
      this.loading = true;

      // SIMULANDO CHAMADA PARA API

      await new Promise(resolve => setTimeout(resolve, 1000));

      const allOrders = [
        { id: "ORD-1001", customer: "João Silva", status: "paid", total: "R$ 199,90" },
        { id: "ORD-1002", customer: "Maria Souza", status: "pending", total: "R$ 89,00" },
        { id: "ORD-1003", customer: "Carlos Pereira", status: "cancelled", total: "R$ 59,90" },
        { id: "ORD-1004", customer: "Ana Lima", status: "paid", total: "R$ 129,00" },
        { id: "ORD-1005", customer: "Pedro Rocha", status: "pending", total: "R$ 49,90" },
        { id: "ORD-1006", customer: "Lucas Mendes", status: "paid", total: "R$ 299,00" },
        { id: "ORD-1007", customer: "Juliana Alves", status: "paid", total: "R$ 79,90" },
        { id: "ORD-1008", customer: "Fernanda Santos", status: "pending", total: "R$ 149,00" },
        { id: "ORD-1009", customer: "Ricardo Oliveira", status: "cancelled", total: "R$ 99,90" },
        { id: "ORD-1010", customer: "Isabela Costa", status: "paid", total: "R$ 179,00" },
        { id: "ORD-1011", customer: "Gustavo Ferreira", status: "pending", total: "R$ 39,90" },
        { id: "ORD-1012", customer: "Mariana Almeida", status: "paid", total: "R$ 119,00" },
      ];

      const filtered = this.filter
        ? allOrders.filter(o =>
          o.id.toLowerCase().includes(this.filter.toLowerCase()) ||
          o.customer.toLowerCase().includes(this.filter.toLowerCase())
        )
        : allOrders;

      // FIM - SIMULANDO CHAMADA PARA API

      this.calculatePagination(filtered);
      this.orders = filtered.slice(this.start, this.end);

      this.loading = false;
    },

    search() {
      this.page = 1;
      this.fetchOrders();
    },

    goToPage(page) {
      if (page < 1 || page > this.totalPages) return;
      this.page = page;
      this.fetchOrders();
    },


    edit(order) {
      alert(`Editar pedido ${order.id}`);

    },

    remove(order) {
      if (!confirm(`Deseja excluir o pedido ${order.id}?`)) return;
      this.orders = this.orders.filter(o => o.id !== order.id);
      this.fetchOrders();
    },

    statusLabel(status) {
      return {
        paid: "Pago",
        pending: "Pendente",
        cancelled: "Cancelado"
      }[status] || status;
    }
  };
}

export function render() {
  logger.local("page.orders-list.render.loaded");
  return html;
}
