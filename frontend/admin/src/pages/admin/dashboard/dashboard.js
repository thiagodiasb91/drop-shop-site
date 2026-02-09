import html from "./dashboard.html?raw"

export function getData() {
    return {
        period: '7d',
        stats: {
            totalVolume: 0,
            totalOrders: 0,
            healthScore: 0,
            newUsers: 0,
            pendingErrors: 0,
            pendingApproval: 0,
            activeSellers: 0,
            topSellers: [],
            recentLogs: []
        },

        async init() {
            await this.fetchData();
        },

        async fetchData() {
            // Aqui você faria: const res = await AdminService.getStats(this.period)
            // Simulando retorno do backend (MOCK)
            this.stats = {
                totalVolume: 125450.80,
                totalOrders: 1420,
                healthScore: 98.2,
                newUsers: 24,
                pendingErrors: 3,
                pendingApproval: 5,
                activeSellers: 112,
                topSellers: [
                    { id: 1, name: 'Loja Variedades SP', orders: 450, volume: 45200.00 },
                    { id: 2, name: 'Importados Express', orders: 320, volume: 31000.50 },
                    { id: 3, name: 'Tech Store BR', orders: 120, volume: 15400.20 },
                ],
                recentLogs: [
                    { id: 101, type: 'error', source: 'Webhook Shopee', time: '14:20', message: 'Falha ao processar ordem #223 - SKU não mapeado' },
                    { id: 102, type: 'warning', source: 'Auth Service', time: '12:05', message: 'Token de acesso do vendedor "Importados Express" expira em 2h' },
                    { id: 103, type: 'error', source: 'Kardex Sync', time: '10:00', message: 'Divergência de estoque detectada no Fornecedor "Distribuidora ABC"' }
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
  console.log("page.dashboard.render.loaded");
  return html;
}
