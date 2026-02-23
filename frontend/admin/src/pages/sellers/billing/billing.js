import html from "./billing.html?raw"
import SellersPaymentsService from "../../../services/sellers-payments.service.js";
import stateHelper from "../../../utils/state.helper.js";

export function getData() {
  return {
    loading: true,
    showPaymentModal: false,
    stats: { currentCycle: null, invoices: [] },
    pixData: {
      qrcode: '',
      copyPaste: '',
      loading: false
    },
    async init() {
      this.loading = true;
      await new Promise(resolve => setTimeout(resolve, 800));
      const res = await SellersPaymentsService.getBillingInformations()

      if (res.ok) {
        this.stats = {
          ...res.response,
          invoices: res.response.invoices.map(inv => ({
            ...inv,
            orders: [],
            expanded: false,
            loadingDetails: false
          }))
        }
      }
      this.loading = false;
    },
    async generatePix() {
      this.showPaymentModal = true;
      this.pixData.loading = true;
      const res = await SellersPaymentsService.generatePixPayment(this.stats.currentCycle.totalAmount);
      if (res.ok) {
        this.pixData.qrcode = res.response.qrcode;
        this.pixData.copyPaste = res.response.copyPaste;
      }
      this.pixData.loading = false;
    },
    async toggleDetails(inv) {
      inv.expanded = !inv.expanded;
      if (inv.expanded && inv.orders.length === 0) {
        inv.loadingDetails = true;
        const res = await SellersPaymentsService.getInvoiceDetails(inv.id);
        if (res.ok) inv.orders = res.response;
        inv.loadingDetails = false;
      }
    },
    copyPix() {
      navigator.clipboard.writeText(this.pixData.copyPaste);
      stateHelper.toast('Código Copiado!', 'success');
    },
    statusLabel(status) {
      const map = {
        opened: { text: 'Aberto', color: 'bg-blue-500/10 text-blue-500' },
        pending: { text: 'Pendente', color: 'bg-yellow-500/10 text-yellow-500' },
        paid: { text: 'Pago', color: 'bg-green-500/10 text-green-500' },
        overdue: { text: 'Atrasado', color: 'bg-red-500/10 text-red-500' }
      };
      return map[status] || { text: status, color: 'bg-gray-500/10 text-gray-500' };
    }
  }
}

export function render() {
  console.log("page.payments-monthly.render.loaded");
  return html;
}