import Middlewares from "./route.middlewares";

const RouteGuard = {
  // Preset para Admin: Precisa estar logado e ser admin
  admin: [Middlewares.isAuthenticated, Middlewares.hasRole],
  
  // Preset para Seller: Autenticado, Role Seller e validação de Loja
  seller: [Middlewares.isAuthenticated, Middlewares.hasRole, Middlewares.needsSellerSetup],
  
  
  // Preset para Supplier: Autenticado, Role Supplier e validação de Setup
  supplier: [Middlewares.isAuthenticated, Middlewares.hasRole, Middlewares.needsSupplierSetup],
  
  // Presets de Transição: Autenticam, mas NÃO chamam a validação que gera o loop
  sellerSetup: [Middlewares.isAuthenticated, Middlewares.hasRole],
  supplierSetup: [Middlewares.isAuthenticated, Middlewares.hasRole],
  newUser: [Middlewares.isAuthenticated]
};

export default RouteGuard;