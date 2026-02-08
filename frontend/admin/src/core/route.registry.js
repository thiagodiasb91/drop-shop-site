export const routes = {
  "/": {
    title: "Home",
    js: () => import("../pages/home/home.js"),
  },
  "/new-user": {
    title: "Usuário Novo",
    js: () => import("../pages/new-user/new-user.js"),
    layout: "clean",
    hideMenu: true,
    skipNewUserValidation: true
  },
  "/login": {
    title: "Login",
    js: () => import("../pages/auth/login/login.js"),
    public: true,
  },
  "/dashboard": {
    title: "Dashboard",
    js: () => import("../pages/dashboard/dashboard.js"),
  },
  "/callback": {
    title: "Callback",
    js: () => import("../pages/auth/callback/callback.js"),
    public: true,
  },
  "/products": {
    title: "Produtos",
    allowedRoles: ['admin'],
    js: () => import("../pages/products/products.js"),
  },
  "/orders": {
    title: "Pedidos",
    js: () => import("../pages/orders/list/list.js"),
  },
  "/orders-group": {
    title: "Pedidos Agrupados",
    js: () => import("../pages/orders/list-group/list-group.js"),
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
    layout: "clean",
    js: () => import("../pages/suppliers/initial-setup/initial-setup.js"),
    skipSupplierValidation: true,
    hideMenu: true
  },
  "/sellers/store/setup": {
    allowedRoles: ['seller'],
    title: "Configuração de Código de Loja",
    layout: "clean",
    js: () => import("../pages/sellers/store-setup/store-setup.js"),
    skipStoreValidation: true,
    hideMenu: true,
  },
  "/sellers/:email/store/code": {
    allowedRoles: ['seller'],
    title: "Retorno de Código de loja",
    hideMenu: true,
    layout: "clean",
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
  "/admin/users": {
    title: "Admin - Usuários",
    allowedRoles: ['admin'],
    js: () => import("../pages/users/users.js"),
  },
  "/settings": {
    title: "Configurações",
    js: () => import("../pages/settings/settings.js"),
  },
  "*": {
    title: "Página não encontrada",
    js: () => import("../pages/not-found/not-found.js"),
    skipAuth: true,
    layout: "clean",
  },
};