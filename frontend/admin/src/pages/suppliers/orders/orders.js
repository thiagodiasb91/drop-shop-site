import html from "./orders.html?raw"

export function getData() {
  return {
    search: '',
    dateFrom: '',
    dateTo: '',
    orders: [],

    init() {
      this.orders = this.mockOrders(25)
    },

    get filteredOrders() {
      const search = this.search.toLowerCase();
      const from = this.parseDate(this.dateFrom);
      const to = this.parseDate(this.dateTo);

      return this.orders.filter(order => {

        /* ---------- FILTRO TEXTO ---------- */
        const textMatch =
          order.code.toLowerCase().includes(search) ||
          order.seller.toLowerCase().includes(search) ||
          order.customer.name.toLowerCase().includes(search) ||
          order.response.some(item =>
            item.productName.toLowerCase().includes(search) ||
            item.productId.toLowerCase().includes(search) ||
            item.skuId.toLowerCase().includes(search)
          );

        if (search && !textMatch) return false;

        /* ---------- FILTRO DATA ---------- */
        if (from || to) {
          const orderDate = this.parseDate(order.date);

          if (from && orderDate < from) return false;
          if (to && orderDate > to) return false;
        }

        return true;
      });
    },
    clearFilters() {
      this.search = '';
      this.dateFrom = '';
      this.dateTo = '';
    },

    printLabel(order) {
      console.log('Imprimir etiqueta do pedido:', order.id)
      alert(`Imprimindo etiqueta do pedido ${order.code}`)
    },

    printAllPending() {
      const pending = this.orders.filter(o => o.status === 'pending')
      console.log('Imprimir etiquetas pendentes:', pending)
      alert(`Imprimindo ${pending.length} etiquetas pendentes`)
    },

    mockOrders(count) {
      return Array.from({ length: count }).map((_, i) => ({
        id: `order-${i}`,
        code: `PED-${1000 + i}`,
        date: '2026-02-08',
        seller: `Vendedor ${i % 5 + 1}`,
        status: i % 3 === 0 ? 'sent' : 'pending',
        customer: {
          name: `Cliente ${i}`,
          address: 'Rua das Flores, 123 - São Paulo/SP'
        },
        items: [
          {
            productId: `PROD-${i}`,
            productName: 'Tênis Esportivo',
            skuId: `SKU-${i}-AZ-43`,
            skuLabel: 'Cor Azul • Tam 43',
            qty: 1
          },
          {
            productId: `PROD-${i}`,
            productName: 'Tênis Esportivo',
            skuId: `SKU-${i}-PR-42`,
            skuLabel: 'Cor Preto • Tam 42',
            qty: 2
          }
        ]
      }))
    },
    parseDate(date) {
      return date ? new Date(date).setHours(0, 0, 0, 0) : null;
    },
  }

}

export function render() {
  return html
}
