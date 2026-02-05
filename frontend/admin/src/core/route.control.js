import { getShopId } from "../services/sellers.services"

export function canAccessRoute(route, user) {
  if (!route.allowedRoles) return true;

  const userRole = user.roles || null;

  if (userRole == 'admin') return true;

  return route.allowedRoles.includes(userRole);
}

export function sellerHasStoreId(user, path) {
  console.log("route.control.sellerHasStoreId.called", user, path)
  if (path === "/dashboard") return true
  const shopId = getShopId(user.email)

  return shopId != null
}

