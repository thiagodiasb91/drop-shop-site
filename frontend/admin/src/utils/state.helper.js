import CacheHelper from "./cache.helper.js";
import logger from "./logger.js";

const stateHelper = {
  setSession(token) {
    logger.local("stateHelper.setSession", { token });
    CacheHelper.set("session_token", token);
  },
  removeSession() {
    logger.local("stateHelper.removeSession");
    CacheHelper.remove("session_token");
  },
  setAuthenticated(user, expiresAt) {
    logger.local("stateHelper.setAuthenticated", { user, expiresAt });
    CacheHelper.set("me.data", user);
    CacheHelper.set("me.expiresAt", expiresAt);
  },
  setLogout() {
    logger.local("stateHelper.setLogout");
    CacheHelper.remove("me.data");
    CacheHelper.remove("me.expiresAt");
    CacheHelper.remove("session_token");
  },
  get user() {
    return window.Alpine?.store("auth")?.user || null;
  },

  get authLoading() {
    return window.Alpine?.store("auth")?.loading ?? true;
  },

  get isAuthenticated() {
    return !!this.user;
  },

  refresh() {
    return window.Alpine?.store("auth")?.refresh();
  },

  toast(message, type = "info") {
    window.Alpine?.store("toast")?.open(message, type);
  }
};

export default stateHelper;