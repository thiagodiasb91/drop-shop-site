import html from "./payments-pending.html?raw";
import SellersPaymentsService from "../../../services/sellers-payments.service";
import stateHelper from "../../../utils/state.helper.js";
import { renderGlobalLoader } from "../../../components";

export function getData() {
  return {
    loading: true,
    allOrders: [],
    selectedOrders: [],
    search: "",
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
      return this.allOrders.filter(o => ["pending", "waiting-payment"].includes(o.status));
    },
    get pendingOrders() {
      return this.allOrders.filter(o => ["pending"].includes(o.status));
    },

    get paidOrders() {
      const q = this.search.toLowerCase();
      return this.allOrders.filter(o =>
        o.status === "paid" &&
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

    openPaymentLink(infinityPayUrl) {
      if (!infinityPayUrl) {
        stateHelper.toast("Link de pagamento não disponível", "error");
        return;
      }
      window.open(infinityPayUrl, "_blank");
    },

    async paySelected() {
      if (this.selectedOrders.length === 0) return;
      const selected = this.notPaidOrders.filter(o => this.selectedOrders.includes(o.paymentId));
      const paymentIds = selected.map(o => o.paymentId);
      const amount = selected.reduce((sum, o) => sum + o.totalAmount, 0);

      const hasDifferentSuppliers = selected.some(o => o.supplierId !== selected[0].supplierId);

      if(hasDifferentSuppliers){
        stateHelper.toast("Multiplos pagamentos devem ser feitos para um único fornecedor", "error");
        return;
      }

      if (paymentIds.length === 0 || amount === 0) {
        stateHelper.toast("Nenhum pedido selecionado para pagamento", "error");
        return;
      }

      if (!confirm(`Confirma o pagamento dos ${selected.length} pedidos selecionados, totalizando R$ ${amount.toFixed(2)}?`)) {
        return;
      }

      await this.generatePaymentLink(paymentIds, amount);
    },
    async payIndividual(order) {
      if (!confirm(`Confirma o pagamento do pedido ${order.orderSn}, no valor de R$ ${order.totalAmount.toFixed(2)}?`)) {
        return;
      }

      await this.generatePaymentLink([order.paymentId], order.totalAmount);
    },
    async generatePaymentLink(paymentIds, amount) {
      const res = await SellersPaymentsService.createPaymentLink(paymentIds, amount);
      if (res.ok) {
        stateHelper.toast("Link de pagamento criado com sucesso", "success");
        this.openPaymentLink(res.response.url);
        await this.refresh();
      }
      else {
        stateHelper.toast("Erro ao criar link de pagamento", "error");
      }
    },
    renderLoader() {
      return renderGlobalLoader("Sincronizando repasses...");
    },
    statusLabel(status) {
      const map = {
        pending: {
          text: "Pendente", color: "bg-yellow-100 text-yellow-800"
        },
        "waiting-payment": {
          text: "Link gerado", color: "bg-blue-100 text-blue-800"
        },
      };
      return map[status] || { text: "Desconhecido", color: "bg-slate-100 text-slate-800" };
    }
  };
}

export function render() {
  return html;
}
