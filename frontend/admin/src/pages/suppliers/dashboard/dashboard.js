import html from "./dashboard.html?raw"
import AuthService from "../../../services/auth.service.js";

export function getData() {
    return {
        stats: {
            pendingShipment: 0,
            monthlyRevenue: 0,
            outOfStockCount: 0,
            activeSellers: 0,
            recentKardex: [],
            topSellers: []
        },

        loggedInfo: null,
        steps: [
            {
                title: 'Atualizar Catálogo',
                desc: 'Cadastre seus produtos e variações para que os vendedores possam encontrá-los.',
                icon: 'ph-package',
                link: '/suppliers/products',
                done: false
            },
            {
                title: 'Gerenciar Estoque',
                desc: 'Mantenha as quantidades atualizadas para evitar cancelamentos de pedidos.',
                icon: 'ph-stack',
                link: '/suppliers/stock',
                done: false
            },
            {
                title: 'Pedidos Pendentes',
                desc: 'Verifique se há novas vendas da Shopee aguardando separação e envio.',
                icon: 'ph-truck',
                link: '/suppliers/orders',
                done: false
            }
        ],

        async init() {
            this.loggedInfo = await AuthService.me();
            await this.fetchData();
        },

        async fetchData() {
            console.log("Buscando métricas de fornecedor...");

            // MOCK - Simulação do que viria do seu backend
            this.stats = {
                pendingShipment: 14,       // Pedidos que o vendedor já pagou e o fornecedor deve enviar
                monthlyRevenue: 28450.00,  // Soma do (preço de custo x quantidade) vendidos
                outOfStockCount: 3,        // SKUs que atingiram 0 no estoque
                activeSellers: 18,         // Quantos vendedores únicos fizeram pedidos
                recentKardex: [
                    { id: 1, sku: 'TSHIRT-BLUE-L', type: 'OUT', quantity: 2, origin: 'Vendedor: João Silva' },
                    { id: 2, sku: 'TSHIRT-RED-M', type: 'IN', quantity: 50, origin: 'Ajuste de Inventário' },
                    { id: 3, sku: 'PHONE-CASE-X', type: 'OUT', quantity: 1, origin: 'Vendedor: Maria Importados' },
                    { id: 4, sku: 'PHONE-CASE-X', type: 'OUT', quantity: 5, origin: 'Vendedor: TechStore' }
                ],
                topSellers: [
                    { id: 1, name: 'João Silva Me', percentage: 45 },
                    { id: 2, name: 'TechStore BR', percentage: 30 },
                    { id: 3, name: 'Maria Importados', percentage: 25 }
                ]
            };
        },

        formatCurrency(value) {
            return new Intl.NumberFormat('pt-BR', {
                style: 'currency',
                currency: 'BRL'
            }).format(value);
        },
        openSupport() {
            const phone = "5511999999999";
            const message = encodeURIComponent(`Olá! Sou o fornecedor ${this.loggedInfo?.user.email} e preciso de ajuda com o painel.`);
            window.open(`https://wa.me/${phone}?text=${message}`, '_blank');
        }
    }
}

export function render() {
    return html;
}