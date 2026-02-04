
export const routes = {
  "/": {
    title: "Home",
    js: () => import("../pages/home/home.js"),
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
  "/suppliers/:supplierId/products": {
    allowedRoles: ['supplier'],
    title: "Fornecedor x Produtos",
    js: () => import("../pages/supplier-products/supplier-products.js"),
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
  },
};