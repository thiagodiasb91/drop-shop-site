import html from "./orders-to-send.html?raw";
import { renderGlobalLoader } from "../../../components/index";
import logger from "../../../utils/logger.js";
import SupplierOrdersService from "../../../services/suppliers-orders.service.js";
import stateeHelper from "../../../utils/state.helper.js";

// const OrdersService = {
//   async getPendingOrders() {
//     // Simulando delay de rede
//     await new Promise(resolve => setTimeout(resolve, 800));
//     const count = 20;

//     return Array.from({ length: count }).map((_, i) => (
//       {
//         id: `ord_${i + 1}`,
//         code: `PED-${1000 + i}`,
//         date: new Date(Date.now() - Math.random() * 1e10).toISOString(),
//         seller: `Vendedor ${i % 5 + 1}`,
//         customer: {
//           name: `Cliente ${i + 1}`,
//           address: `Rua Exemplo, ${i + 10}, Bairro Teste, Cidade ${i % 3 + 1}`,
//         },
//         status: "pending",
//         expanded: false, // Controle de UI
//         items: Array.from({ length: Math.ceil(Math.random() * 5) }).map((_, j) => ({
//           id: `item_${i + 1}_${j + 1}`,
//           productName: `Produto ${j + 1}`,
//           imageUrl: `https://picsum.photos/${Math.floor(Math.random() * 500)}`,
//           price: (Math.random() * 100).toFixed(2),
//           skuId: `sku_${j + 1}`,
//           skuLabel: `SKU ${j + 1}`,
//           quantity: Math.ceil(Math.random() * 3),
//         }))
//       }
//     ));
//   }
// };

export function getData() {
  return {
    search: "",
    dateFrom: "",
    dateTo: "",
    orders: [],
    loading: true,

    async init() {
      this.orders = await this.getOrdersToSend();
      this.loading = false;
    },
    async getOrdersToSend(){
      const res = await SupplierOrdersService.getOrdersToSend();

      if (!res.ok) {
        logger.error("SupplierOrdersService.getOrdersToSend.error", res.response);
        stateeHelper.toast("Erro ao obter pedidos", "error");
        return [];
      }

      return res.response;
    },
    get filteredOrders() {
      const search = this.search.toLowerCase();
      return this.orders.filter(order => {
        return order.orderId?.toLowerCase().includes(search) ||
          order.sellerName?.toLowerCase().includes(search) ||
          order.customerName.toLowerCase().includes(search);
      });
    },
    clearFilters() {
      this.search = "";
      this.dateFrom = "";
      this.dateTo = "";
    },
    toggleOrder(orderId) {
      this.orders = this.orders.map(o => {
        if (o.id === orderId) o.expanded = !o.expanded;
        return o;
      });
    },

    parseDate(dateStr) {
      return dateStr ? new Date(dateStr) : null;
    },

    printLabel(order) {
      logger.local("Imprimir etiqueta do pedido:", order.id);
      alert(`Imprimindo etiqueta do pedido ${order.code}`);
    },
    labelStatus(status) {
      const map = {
        pending: "Pendente",
        printed: "Impressa",
        shipped: "Enviada"
      };
      return map[status] || status;
    },
    printAllPending() {
      const pending = this.orders.filter(o => o.status === "pending");
      logger.local("Imprimir etiquetas pendentes:", pending);
      alert(`Imprimindo ${pending.length} etiquetas pendentes`);
    },
    renderLoader() {
      return renderGlobalLoader("Carregando pedidos...");
    }
  };

}

export function render() {
  return html;
}
