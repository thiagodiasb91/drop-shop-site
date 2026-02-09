import html from "./dashboard.html?raw"

export function getData() {
    return {
        period: '7d',
        stats: {
            salesGmv: 0,
            orderCount: 0,
            pendingPayment: 0,
            lowStockAlerts: 0,
            estimatedProfit: 0,
            recentOrders: [],
            suppliers: []
        },

        async init() {
            await this.fetchData();
        },

        async fetchData() {
            console.log(`Buscando dados do vendedor para o período: ${this.period}`);
            
            // MOCK - Dados simulados para o vendedor
            this.stats = {
                salesGmv: 4500.00,       // Total vendido na Shopee
                orderCount: 85,
                pendingPayment: 1250.40, // O que ele deve pagar ao fornecedor
                lowStockAlerts: 4,       // Produtos vinculados que estão acabando no fornecedor
                estimatedProfit: 2150.00, // (GMV - Taxas Shopee - Preço de Custo)
                recentOrders: [
                    { id: 1, shopeeId: '231025ABC12', date: '25/10 14:30', status: 'READY_TO_SHIP', costPrice: 45.90 },
                    { id: 2, shopeeId: '231025DEF45', date: '25/10 13:15', status: 'SHIPPED', costPrice: 89.00 },
                    { id: 3, shopeeId: '231024XYZ88', date: '24/10 18:00', status: 'SHIPPED', costPrice: 12.50 },
                ],
                suppliers: [
                    { id: 1, name: 'Distribuidora Norte', activeProducts: 12, pendingValue: 850.40 },
                    { id: 2, name: 'Eletrônicos China BR', activeProducts: 5, pendingValue: 400.00 }
                ]
            };
        },

        formatCurrency(value) {
            return new Intl.NumberFormat('pt-BR', {
                style: 'currency',
                currency: 'BRL'
            }).format(value);
        }
    }
}

export function render() {
    return html;
}