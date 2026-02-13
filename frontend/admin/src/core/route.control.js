export function canAccessRoute(route, userRole) {
  console.log("route.control.canAccessRoute.request", route, userRole)

  if (!route.allowedRoles) return true;

  if (userRole == 'admin') return true;

  return route.allowedRoles.includes(userRole);
}

export async function redirectToSellerSetup(loggedInfo, route, path) {
  console.log("route.control.redirectToSellerSetup.request", loggedInfo, route, path)
  let redirect = loggedInfo.role === 'seller' &&
    !route.skipStoreValidation &&
    // path !== '/dashboard' && // bypass teste
    !loggedInfo.resourceId

  console.log("route.control.redirectToSellerSetup.redirect", redirect)
  return redirect
}


export async function redirectToSupplierSetup(loggedInfo, route, path) {
  console.log("route.control.redirectToSupplierSetup.request", loggedInfo, route, path)
  let redirect = loggedInfo.role === 'supplier' &&
    !route.skipSupplierValidation &&
    !loggedInfo.resourceId

  console.log("route.control.redirectToSupplierSetup.redirect", redirect)
  return redirect
}

export function redirectToNewUserPage(loggedInfo, route) {
  console.log("route.control.redirectToNewUserPage.request", loggedInfo, route)
  let redirect = loggedInfo.role === 'new-user' &&
    !route.skipNewUserValidation

  console.log("route.control.redirectToNewUserPage.redirect", redirect)
  return redirect
}



