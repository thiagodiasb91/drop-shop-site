const Middlewares = {
  isAuthenticated: (user, route) => {
    if (route.public) return true;
    return !!user || "/login";
  },

  hasRole: (user, route) => {
    if (!route.allowedRoles) return true;
    if (user?.role === "admin") return true;
    if (!user) return "/login";
    return route.allowedRoles.includes(user.role) || "/";
  },

  needsSellerSetup: (user) => {
    const isSeller = user?.role === "seller";
    const missingResourceId = !user?.resourceId;

    if (isSeller && missingResourceId) {
      return "/sellers/store/setup";
    }
    return true;
  },

  needsSupplierSetup: (user,) => {
    if (user?.role === "supplier" && !user?.resourceId) {
      return "/suppliers/setup";
    }
    return true;
  },

  needsDistributionCenterSetup: (user) => {
    if (user?.role === "distribution_center" && !user?.resourceId) {
      return "/distribution-center/new";
    }
    return true;
  },

  isNewUser: (user) => {
    if (user?.role === "new-user") {
      return "/new-user";
    }
    return true;
  }
};

export default Middlewares;