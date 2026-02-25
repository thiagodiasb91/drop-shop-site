import html from "./orders.html?raw"
import { renderGlobalLoader } from "../../../components/index"

const OrdersService = {
  async getPendingOrders() {
    // Simulando delay de rede
    await new Promise(resolve => setTimeout(resolve, 800));
    const count = 20

    return Array.from({ length: count }).map((_, i) => (
      {
        id: `ord_${i + 1}`,
        code: `PED-${1000 + i}`,
        date: new Date(Date.now() - Math.random() * 1e10).toISOString(),
        seller: `Vendedor ${i % 5 + 1}`,
        customer: {
          name: `Cliente ${i + 1}`,
          address: `Rua Exemplo, ${i + 10}, Bairro Teste, Cidade ${i % 3 + 1}`,
        },
        status: 'pending',
        expanded: false, // Controle de UI
        items: Array.from({ length: Math.ceil(Math.random() * 5) }).map((_, j) => ({
          id: `item_${i + 1}_${j + 1}`,
          productName: `Produto ${j + 1}`,
          image: `https://picsum.photos/${Math.floor(Math.random() * 500)}`,
          price: (Math.random() * 100).toFixed(2),
          skuId: `sku_${j + 1}`,
          skuLabel: `SKU ${j + 1}`,
          quantity: Math.ceil(Math.random() * 3),
        }))
      }
    ))
  }
}

export function getData() {
  return {
    search: '',
    dateFrom: '',
    dateTo: '',
    orders: [],
    loading: true,

    async init() {
      this.orders = await OrdersService.getPendingOrders();
      this.loading = false;
    },
    get filteredOrders() {
      const search = this.search.toLowerCase();
      return this.orders.filter(order => {
        return order.code?.toLowerCase().includes(search) ||
          order.seller?.toLowerCase().includes(search) ||
          order.customer?.name.toLowerCase().includes(search);
      });
    },
    clearFilters() {
      this.search = '';
      this.dateFrom = '';
      this.dateTo = '';
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
      console.log('Imprimir etiqueta do pedido:', order.id)
      alert(`Imprimindo etiqueta do pedido ${order.code}`)
    },
    labelStatus(status) {
      const map = {
        pending: 'Pendente',
        printed: 'Impressa',
        shipped: 'Enviada'
      }
      return map[status] || status
    },
    printAllPending() {
      const pending = this.orders.filter(o => o.status === 'pending')
      console.log('Imprimir etiquetas pendentes:', pending)
      alert(`Imprimindo ${pending.length} etiquetas pendentes`)
    },
    renderLoader() {
      return renderGlobalLoader("Carregando pedidos...")
    }
  }

}

export function render() {
  return html
}
