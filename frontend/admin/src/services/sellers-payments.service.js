import logger from "../utils/logger.js";
import BaseApi from "./base.api.js";

const api = new BaseApi("/sellers/payments");

const SellersPaymentsService = {
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
    return api.call(
      "/summary",
      {
        method: "GET",
      });
  },

  async getSupplierPaymentDetails(supplierId) {
    logger.local("SellersPaymentsService.getSupplierPaymentDetails.request", supplierId);
    /* const res = await fetch(`${this.basePath}/supplier/${supplierId}`, { method: "GET" });
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
          status: "opened", // Vendedor não pode pagar ainda
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
    logger.local("SellersPaymentsService.getInvoiceDetails.request", invoiceId);

    return new Promise((resolve) => {
      setTimeout(() => {
        resolve({
          ok: true,
          response: [
            {
              orderId: "SHP-9981",
              totalAmount: 45.90,
              itemsCount: 2,
              date: "10/01"
            },
            {
              orderId: "SHP-9982",
              totalAmount: 66.00,
              itemsCount: 3,
              date: "12/01"
            },
            {
              orderId: "SHP-9983",
              totalAmount: 15.00,
              itemsCount: 1,
              date: "15/01"
            },
            {
              orderId: "SHP-9984",
              totalAmount: 35.00,
              itemsCount: 1,
              date: "18/01"
            }
          ]
        });
      }, 500);
    });
  },
  async createPaymentLink(paymentIds, amount) {
    // POST /sellers/payments/create-link
    return api.call(
      "/create-link",
      {
        method: "POST",
        body: JSON.stringify({ paymentIds, amount }),
      }
    );
  },
};

export default SellersPaymentsService;