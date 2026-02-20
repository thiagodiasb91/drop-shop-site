import html from "./payments-pending.html?raw"
import SellersPaymentsService from "../../../services/sellers-payments.service";

export function getData() {
  return {
    loading: true,
    suppliers: [],
    search: '',
    stats: {},

    currentPage: 1,
    pageSize: 5,

    async init() {
      await this.refresh();
    },

    async refresh() {
      this.loading = true;
      const [resSummary, resStats] = await Promise.all([
        SellersPaymentsService.getPaymentSummary(),
        SellersPaymentsService.getFinancialSummary()
      ]);

      if (resSummary.ok) {
        this.suppliers = resSummary.response.map(s => ({
          ...s,
          expanded: false,
          details: [],
          loadingDetails: false,
          processingPay: false
        }));
        if (resStats.ok) {
          this.stats = resStats.response;
        }
      }
      this.loading = false;
    },
    async toggleSupplier(supplier) {
      supplier.expanded = !supplier.expanded;

      // Carrega detalhes apenas na primeira vez que expandir
      if (supplier.expanded && supplier.details.length === 0) {
        supplier.loadingDetails = true;
        const res = await SellersPaymentsService.getSupplierPaymentDetails(supplier.supplierId);
        if (res.ok) {
          supplier.details = res.response;
        }
        supplier.loadingDetails = false;
      }
    },
    async confirmPayment(sup) {
      if (!confirm(`Confirmar pagamento de R$ ${sup.totalAmount.toFixed(2)} para ${sup.supplierName}?`)) return;

      sup.processingPay = true;
      const res = await SellersPaymentsService.paySupplier(sup.supplierId, sup.totalAmount);

      if (res.ok) {
        Alpine.store('toast').open('Pagamento registrado!', 'success');
        await this.refresh();
      }
      sup.processingPay = false;
    },
    // Grupos calculados
    get pendingSuppliers() {
      return this.suppliers.filter(s => s.status === 'pending');
    },

    get paidSuppliers() {
      return this.suppliers.filter(s => s.status === 'paid');
    },
    get filteredSuppliers() {
      const q = this.search.toLowerCase();
      return this.suppliers.filter(s => s.supplierName.toLowerCase().includes(q));
    },

    // Paginação em memória
    get pagedPaidList() {
      const start = (this.currentPage - 1) * this.pageSize;
      return this.paidSuppliers.slice(start, start + this.pageSize);
    },

    get totalPages() {
      return Math.ceil(this.paidSuppliers.length / this.pageSize);
    }
  }
}

export function render() {
  console.log("page.orders-list.render.loaded");
  return html;
}
