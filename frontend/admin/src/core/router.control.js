export function canAccessRoute(route, user) {
  if (!route.allowedRoles) return true;

  const userRole = user.roles || null;

  if (userRole == 'admin') return true;
  
  return route.allowedRoles.includes(userRole);
}
