import html from "./payments-pending.html?raw"
import SellersPaymentsService from "../../../services/sellers-payments.service";
import stateHelper from "../../../utils/state.helper.js";
import {renderGlobalLoader} from "../../../components";

export function getData() {
  return {
    loading: true,
    allOrders: [],
    selectedOrders: [],
    search: '',
    stats: {},
    currentPage: 1,
    pageSize: 5,

    async init() {
      await this.refresh();
    },

    async refresh() {
      this.loading = true;
      this.selectedOrders = [];
      const [resSummary, resStats] = await Promise.all([
        SellersPaymentsService.getPaymentSummary(),
        SellersPaymentsService.getFinancialSummary()
      ]);

      if (resSummary.ok) {
        this.allOrders = resSummary.response?.map(o => ({
          ...o,
          expanded: false,
          status: ["pending", "pending_payment"][Math.floor(Math.random() * 2)],
          isSplit: resSummary.response.filter(item => item.orderSn === o.orderSn).length > 1
        })) || [];

        if (resStats.ok) {
          this.stats = resStats.response;
        }
      }
      this.loading = false;
    },
    toggleOrder(order) {
      order.expanded = !order.expanded;
    },

    // Filtros e Grupos
    get notPaidOrders() {
      return this.allOrders.filter(o => ["pending", "pending_payment"].includes(o.status));
    },
    get pendingOrders() {
      return this.allOrders.filter(o => ["pending"].includes(o.status));
    },

    get paidOrders() {
      const q = this.search.toLowerCase();
      return this.allOrders.filter(o =>
        o.status === 'paid' &&
        (o.orderSn.toLowerCase().includes(q) || o.paymentId.toLowerCase().includes(q))
      );
    },

    // Paginação do Histórico
    get totalPages() {
      return Math.ceil(this.paidOrders.length / this.pageSize) || 1;
    },

    get pagedPaidOrders() {
      const start = (this.currentPage - 1) * this.pageSize;
      return this.paidOrders.slice(start, start + this.pageSize);
    },

    get selectedTotal() {
      return this.notPaidOrders
        .filter(o => this.selectedOrders.includes(o.paymentId))
        .reduce((sum, o) => sum + o.totalAmount, 0);
    },

    // Ações
    toggleSelectAll() {
      if (this.selectedOrders.length === this.pendingOrders.length) {
        this.selectedOrders = [];
      } else {
        this.selectedOrders = this.pendingOrders.map(o => o.paymentId);
      }
    },

    payIndividual(order) {
      if (!order.infinityPayUrl) {
        stateHelper.toast('Link de pagamento não disponível', 'error');
        return;
      }
      window.location.href = order.infinityPayUrl;
    },

    async paySelected() {
      if (this.selectedOrders.length === 0) return;
      // Se houver mais de uma, aqui você decidiria se abre o link da primeira 
      // ou se sua API tem um endpoint de "lote". 
      // Baseado no seu JSON, seguiremos com o link individual ou primeiro do lote:
      const firstOrder = this.notPaidOrders.find(o => o.paymentId === this.selectedOrders[0]);
      this.payIndividual(firstOrder);
    },
    renderLoader() {
      return renderGlobalLoader("Sincronizando repasses...");
    },
    statusLabel(status) {
      const map = {
        pending: { text: "Pendente", color: "bg-yellow-100 text-yellow-800" },
        pending_payment: { text: "Link gerado", color: "bg-blue-100 text-blue-800" },
      }
      return map[status] || { text: "Desconhecido", color: "bg-slate-100 text-slate-800" };
    }
  }
}

export function render() {
  console.log("page.orders-list.render.loaded");
  return html;
}
