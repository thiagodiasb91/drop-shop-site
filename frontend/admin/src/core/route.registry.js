export const routes = {
  "/": {
    title: "Home",
    hideMenu: true,
    js: () => import("../pages/dashboard/dashboard.js"),
  },
  "/new-user": {
    title: "Usuário Novo",
    js: () => import("../pages/new-user/new-user.js"),
    layout: "public",
    hideMenu: true,
    skipNewUserValidation: true
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
  "/admin/dashboard": {
    allowedRoles: ['admin'],
    title: "Painel do Admin",
    js: () => import("../pages/admin/dashboard/dashboard.js"),
  },
  "/admin/products": {
    title: "Produtos",
    allowedRoles: ['admin'],
    js: () => import("../pages/admin/products/products.js"),
  },
  "/admin/users": {
    title: "Usuários",
    allowedRoles: ['admin'],
    js: () => import("../pages/admin/users/users.js"),
  },
  "/suppliers/dashboard": {
    allowedRoles: ['supplier'],
    title: "Painel do Fornecedor",
    js: () => import("../pages/suppliers/dashboard/dashboard.js"),
  },
  "/suppliers/products": {
    allowedRoles: ['supplier'],
    title: "Meus Produtos",
    js: () => import("../pages/suppliers/link-products/link-products.js"),
  },
  "/suppliers/stock": {
    allowedRoles: ['supplier'],
    title: "Estoque",
    js: () => import("../pages/suppliers/update-stock/update-stock.js"),
  },
  "/suppliers/orders": {
    allowedRoles: ['supplier'],
    title: "Pedidos para Envio",
    js: () => import("../pages/suppliers/orders/orders.js"),
  },
  "/suppliers/setup": {
    allowedRoles: ['supplier'],
    title: "Configuração de Fornecedor",
    layout: "public",
    js: () => import("../pages/suppliers/initial-setup/initial-setup.js"),
    skipSupplierValidation: true,
    hideMenu: true
  },
  "/sellers/dashboard": {
    allowedRoles: ['seller'],
    title: "Painel do Vendedor",
    js: () => import("../pages/sellers/dashboard/dashboard.js"),
  },
  "/sellers/store/setup": {
    allowedRoles: ['seller'],
    title: "Configuração de Código de Loja",
    layout: "public",
    js: () => import("../pages/sellers/store-setup/store-setup.js"),
    skipStoreValidation: true,
    hideMenu: true,
  },
  "/sellers/:email/store/code": {
    allowedRoles: ['seller'],
    title: "Retorno de Código de loja",
    hideMenu: true,
    layout: "public",
    js: () => import("../pages/sellers/store-code/store-code.js"),
    skipStoreValidation: true,
  },
  "/sellers/stock": {
    allowedRoles: ['seller'],
    title: "Estoque",
    js: () => import("../pages/sellers/view-stock/view-stock.js"),
  },
  "/sellers/products": {
    allowedRoles: ['seller'],
    title: "Meus Produtos",
    js: () => import("../pages/sellers/link-products/link-products.js"),
  },
  "/orders": {
    title: "Pedidos",
    hideMenu: true,
    js: () => import("../pages/orders/list/list.js"),
  },
  "/orders-group": {
    title: "Pedidos Agrupados",
    hideMenu: true,
    js: () => import("../pages/orders/list-group/list-group.js"),
  },
  "/settings": {
    title: "Configurações",
    hideMenu: true,
    js: () => import("../pages/settings/settings.js"),
  },
  "*": {
    title: "Página não encontrada",
    js: () => import("../pages/not-found/not-found.js"),
    skipAuth: true,
    layout: "public",
  },
};