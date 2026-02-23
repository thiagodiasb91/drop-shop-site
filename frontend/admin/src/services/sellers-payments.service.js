import { ENV } from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"
import CacheHelper from "../utils/cache.helper.js";

const SellersPaymentsService = {
  basePath: `${ENV.API_BASE_URL}/sellers/payments`,
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
    const res = await fetch(
      `${this.basePath}/summary`,
      {
        method: "GET",
        headers: {
          "Authorization": `Bearer ${CacheHelper.get("session_token")}`
        }
      });
    return responseHandler(res);


    // // MOCK: Lista de fornecedores que o vendedor precisa pagar
    // const pending = Array.from({ length: 2 }, (_, i) => ({
    //   supplierId: `SP-${i + 1}`,
    //   supplierName: `Fornecedor Pendente ${i + 1}`,
    //   totalAmount: Math.floor(Math.random() * 2000) + 100,
    //   totalItems: Math.floor(Math.random() * 10) + 1,
    //   status: "pending",
    //   dueDate: "2026-02-28"
    // }));

    // // Gerando 50 itens concluídos
    // const paid = Array.from({ length: 50 }, (_, i) => ({
    //   supplierId: `SC-${i + 1}`,
    //   supplierName: `Fornecedor Histórico ${i + 1}`,
    //   totalAmount: Math.floor(Math.random() * 1500) + 50,
    //   totalItems: Math.floor(Math.random() * 5) + 1,
    //   status: "paid",
    //   paidAt: "2026-01-20"
    // }));

    // return {
    //   ok: true,
    //   response: [...pending, ...paid]
    // };
  },

  // Simula o endpoint: GET /sellers/payments/supplier/{id}
  // Carregamento sob demanda (Lazy Loading)
  async getSupplierPaymentDetails(supplierId) {
    /* const res = await fetch(`${this.basePath}/supplier/${supplierId}`, { method: "GET" });
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
  async getBillingInformations() {
    // Simula o acumulado do mês atual e faturas anteriores
    return {
      ok: true,
      response: {
        currentCycle: {
          period: "01/02 a 28/02",
          feePerOrder: 1.00, // Sua taxa fixa
          ordersCount: 142,
          totalAmount: 142.00,
          status: 'opened', // Vendedor não pode pagar ainda
          closingDate: "28/02/2026"
        },
        invoices: [
          {
            id: "INV-2026-02",
            month: "Fevereiro/2026",
            amount: 4.00,
            status: "opened",
            closingDate: "28/02/2026",
            itemsCount: 4
          },
          {
            id: "INV-2026-01",
            month: "Janeiro/2026",
            amount: 4.00,
            status: "overdue",
            dueDate: "31/01/2026",
            itemsCount: 4
          },
          {
            id: "INV-2025-12",
            month: "Dezembro/2025",
            amount: 4.00,
            status: "paid",
            paidAt: "2025-12-15",
            itemsCount: 4
          }
        ]
      }
    };
  },
  async getInvoiceDetails(invoiceId) {
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve({
          ok: true,
          response: [
            {
              orderId: 'SHP-9981',
              totalAmount: 45.90,
              itemsCount: 2,
              date: '10/01'
            },
            {
              orderId: 'SHP-9982',
              totalAmount: 66.00,
              itemsCount: 3,
              date: '12/01'
            },
            {
              orderId: 'SHP-9983',
              totalAmount: 15.00,
              itemsCount: 1,
              date: '15/01'
            },
            {
              orderId: 'SHP-9984',
              totalAmount: 35.00,
              itemsCount: 1,
              date: '18/01'
            }
          ]
        });
      }, 500);
    });
  },
  async generatePixPayment(amount) {
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve({
          ok: true,
          response: {
            qrcode: `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=PIX_MOCK_VALUE_${amount}`,
            copyPaste: "00020126580014br.gov.bcb.pix013660f95b32-3435-424d-b636-23112345678952040000530398654071450.805802BR..."
          }
        });
      }, 1200);
    });
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