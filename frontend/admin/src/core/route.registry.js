import RouteGuard from "./route.guard.js";

export const routes = {
  "/": {
    title: "Home",
    middlewares: RouteGuard.authenticated,
    hideMenu: true,
    js: () => import("../pages/dashboard/dashboard.js"),
  },
  "/new-user": {
    title: "Usuário Novo",
    js: () => import("../pages/commons/new-user/new-user.js"),
    layout: "public",
    hideMenu: true,
    middlewares: RouteGuard.newUser,
  },
  "/login": {
    title: "Login",
    js: () => import("../pages/auth/login/login.js"),
    public: true,
  },
  "/callback": {
    title: "Callback",
    js: () => import("../pages/auth/callback/callback.js"),
    public: true,
  },

  // --- ADMIN ---
  "/admin/dashboard": {
    allowedRoles: ['admin'],
    title: "Painel Geral",
    middlewares: RouteGuard.admin,
    group: "Principal",
    icon: "ph ph-chart-line-up",
    js: () => import("../pages/admin/dashboard/dashboard.js"),
  },
  "/admin/users": {
    title: "Usuários",
    allowedRoles: ['admin'],
    middlewares: RouteGuard.admin,
    group: "Cadastros",
    icon: "ph ph-users",
    js: () => import("../pages/admin/users/users.js"),
  },
  "/admin/products": {
    title: "Catálogo Global",
    allowedRoles: ['admin'],
    middlewares: RouteGuard.admin,
    group: "Cadastros",
    icon: "ph ph-package",
    js: () => import("../pages/admin/products/products.js"),
  },

  // --- SUPPLIERS ---
  "/suppliers/dashboard": {
    allowedRoles: ['supplier'],
    middlewares: RouteGuard.supplier,
    title: "Painel do Fornecedor",
    group: "Principal",
    icon: "ph ph-gauge",
    js: () => import("../pages/suppliers/dashboard/dashboard.js"),
  },
  "/suppliers/products": {
    allowedRoles: ['supplier'],
    middlewares: RouteGuard.supplier,
    title: "Meus Produtos",
    group: "Inventário",
    icon: "ph ph-package",
    js: () => import("../pages/suppliers/link-products/link-products.js"),
  },
  "/suppliers/stock": {
    allowedRoles: ['supplier'],
    middlewares: RouteGuard.supplier,
    title: "Estoque",
    group: "Inventário",
    icon: "ph ph-stack",
    js: () => import("../pages/suppliers/update-stock/update-stock.js"),
  },
  "/suppliers/orders-to-send": {
    allowedRoles: ['supplier'],
    middlewares: RouteGuard.supplier,
    title: "Pedidos para Envio",
    group: "Operações",
    icon: "ph ph-list-checks",
    js: () => import("../pages/suppliers/orders-to-send/orders-to-send.js"),
  },
  "/suppliers/setup": {
    allowedRoles: ['supplier'],
    middlewares: RouteGuard.supplierSetup,
    title: "Configurar Fornecedor",
    hideMenu: true,
    layout: "public",
    js: () => import("../pages/suppliers/initial-setup/initial-setup.js"),
  },

  // --- SELLERS ---
  "/sellers/dashboard": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.seller,
    title: "Meu Painel",
    group: "Principal",
    icon: "ph ph-house-line",
    js: () => import("../pages/sellers/dashboard/dashboard.js"),
  },
  "/sellers/:email/store/code": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.sellerSetup,
    title: "Retorno de Código de loja",
    hideMenu: true,
    layout: "public",
    js: () => import("../pages/sellers/store-code/store-code.js"),
  },
  "/sellers/store/setup": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.sellerSetup,
    title: "Configuração de Código de Loja",
    hideMenu: true,
    layout: "public",
    js: () => import("../pages/sellers/store-setup/store-setup.js"),
  },
  "/sellers/products": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.seller,
    title: "Meus Produtos",
    group: "Inventário",
    icon: "ph ph-package",
    js: () => import("../pages/sellers/link-products/link-products.js"),
  },
  "/sellers/view-stock": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.seller,
    title: "Ver Estoque",
    group: "Inventário",
    icon: "ph ph-stack",
    hideMenu: true,
    js: () => import("../pages/sellers/view-stock/view-stock.js"),
  },
  "/sellers/payments/pending": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.seller,
    title: "Repasses",
    group: "Financeiro",
    icon: "ph ph-hand-coins",
    js: () => import("../pages/sellers/payments-pending/payments-pending.js"),
  },
  "/sellers/billing": {
    allowedRoles: ['seller'],
    middlewares: RouteGuard.seller,
    title: "Faturamento",
    group: "Financeiro",
    icon: "ph ph-receipt",
    js: () => import("../pages/sellers/billing/billing.js"),
  },

  // --- SELLERS ---
  "/distribution-center/new": {
    allowedRoles: ['distribution_center'],
    middlewares: RouteGuard.distributionCenterSetup,
    title: "Central de Distribuição sem vínculo com Fornecedor",
    hideMenu: true,
    layout: "public",
    js: () => import("../pages/commons/new-distribution-center/new-distribution-center.js"),
  },
  "/distribution-center/dashboard": {
    allowedRoles: ['distribution_center'],
    middlewares: RouteGuard.distributionCenter,
    title: "Meu Painel",
    group: "Principal",
    icon: "ph ph-house-line",
    js: () => import("../pages/distribution-center/dashboard/dashboard.js"),
  },
  "/distribution-center/orders-to-send": {
    allowedRoles: ['distribution_center'],
    middlewares: RouteGuard.distributionCenter,
    title: "Pedidos para Envio",
    group: "Operações",
    icon: "ph ph-list-checks",
    js: () => import("../pages/distribution-center/orders-to-send/orders-to-send.js"),
  },

  // --- OPERACIONAL ---
  "/orders": {
    title: "Pedidos",
    group: "Operações",
    icon: "ph ph-list-checks",
    hideMenu: true,
    js: () => import("../pages/orders/list/list.js"),
  },
  "/orders-group": {
    title: "Agrupados",
    group: "Operações",
    icon: "ph ph-folders",
    hideMenu: true,
    js: () => import("../pages/orders/list-group/list-group.js"),
  },

  // --- WILDCARD ---
  "*": {
    title: "404",
    hideMenu: true,
    js: () => import("../pages/commons/not-found/not-found.js"),
  }
};