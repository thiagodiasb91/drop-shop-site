import { responseHandler } from "../utils/response.handler.js"

const SellersPaymentsService = {
  basePath: "/sellers/payments",
  async getFinancialSummary() {
    // Simularia: GET /sellers/payments/dashboard-summary
    return {
      ok: true,
      response: {
        totalPending: 2450.80,
        totalPaidMonth: 12400.50,
        totalOrdersMonth: 142,
        averageTicket: 87.32,
        nextDueDate: "2026-02-28",
        pendingSuppliersCount: 15
      }
    };
  },

  // Simula o endpoint: GET /sellers/payments/summary  
  async getPaymentSummary() {
    /* const res = await fetch(`${basePath}/payments/summary`, { method: "GET" });
    return responseHandler(res);
    */

    // MOCK: Lista de fornecedores que o vendedor precisa pagar
    const pending = Array.from({ length: 2 }, (_, i) => ({
      supplierId: `SP-${i + 1}`,
      supplierName: `Fornecedor Pendente ${i + 1}`,
      totalAmount: Math.floor(Math.random() * 2000) + 100,
      totalItems: Math.floor(Math.random() * 10) + 1,
      status: "pending",
      dueDate: "2026-02-28"
    }));

    // Gerando 50 itens concluídos
    const paid = Array.from({ length: 50 }, (_, i) => ({
      supplierId: `SC-${i + 1}`,
      supplierName: `Fornecedor Histórico ${i + 1}`,
      totalAmount: Math.floor(Math.random() * 1500) + 50,
      totalItems: Math.floor(Math.random() * 5) + 1,
      status: "paid",
      paidAt: "2026-01-20"
    }));

    return {
      ok: true,
      response: [...pending, ...paid]
    };
  },

  // Simula o endpoint: GET /sellers/payments/supplier/{id}
  // Carregamento sob demanda (Lazy Loading)
  async getSupplierPaymentDetails(supplierId) {
    /* const res = await fetch(`${basePath}/payments/supplier/${supplierId}`, { method: "GET" });
    return responseHandler(res);
    */

    const products = [
      {
        id: "P1",
        name: "Produto Exemplo A",
        quantity: 2,
        unitPrice: 50.0,
        total: 100.0,
        imageUrl: `https://picsum.photos/${Math.floor(Math.random() * 500)}`,
        orderId: "SHP-123"
      },
      { id: "P2", name: "Produto Exemplo B", quantity: 1, unitPrice: 150.0, total: 150.0, imageUrl: "https://picsum.photos/201", orderId: "SHP-124" }
    ];

    return {
      ok: true,
      response: products
    };
  },
  async paySupplier(supplierId, amount) {
    // POST /sellers/payments/suppliers/{supplier_id}/pay
    /* const res = await fetch(`${this.basePath}/pay`, { 
      method: "POST", 
      body: JSON.stringify({ supplierId, amount }) 
    });
    return responseHandler(res);
    */
    console.log(`Processando pagamento de R$ ${amount} para ${supplierId}`);
    return { ok: true };
  },
};

export default SellersPaymentsService;