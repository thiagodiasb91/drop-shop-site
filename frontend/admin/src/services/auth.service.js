import ENV from "../config/env";
import CacheHelper from "../utils/cache.helper.js";
import stateHelper from "../utils/state.helper.js";
import BaseApi from "./base.api.js";
import logger from "../utils/logger.js";

const api = new BaseApi("/auth");

const AuthService = {
  _mePromise: null,
  async login() {
    logger.local("AuthService.login.request");
    window.location.href = `${ENV.API_BASE_URL}/auth/login`;
  },
  async callback(code) {
    logger.local("AuthService.callback.request", code);
    return api.call("/callback", {
      method: "POST",
      credentials: "include",
      body: JSON.stringify({ code }),
    });
  },

  async me(force = false) {
    if (!force) {
      const cachedMe = CacheHelper.get("me.data");
      const expiresAt = CacheHelper.get("me.expiresAt");

      if (cachedMe && expiresAt && Date.now() < expiresAt * 1000) {
        logger.local("AuthService.me.cached", cachedMe, expiresAt);
        logger.local("AuthService.me.returningCached", cachedMe);
        return cachedMe;
      }
    }

    if (this._mePromise) { return this._mePromise; }

    this._mePromise = (async () => {
      try {
        const res = await api.call("/me");

        if (res.status === 401) {
          logger.local("AuthService.me.unauthorized");
          this.logout();
          return null;
        }

        if (!res.ok) {
          stateHelper.toast("Erro ao obter dados do usuário", "error");
          return null;
        }

        const data = await res.response;
        logger.local("AuthService.me.api.response", data);

        // Simula resposta da API
        // data.role = 'supplier';
        // data.role = 'seller';
        // data.role = 'distribution_center';

        stateHelper.setAuthenticated(data, data.session?.exp);

        return data;
      }
      finally {
        this._mePromise = null;
      }
    })();
    return this._mePromise;
  },

  async confirmCode(code, shopId, email) {
    logger.local("AuthService.confirmCode.request", code, shopId, email);
    return api.call("/confirm-shopee", {
      method: "POST",
      body: JSON.stringify({ code, shopId, email })
    });
  },

  async renewToken() {
    logger.local("AuthService.renewToken.request");
    return api.call(
      "/renew",
      {
        method: "POST"
      });
  },

  async logout() {
    logger.local("AuthService.logout.request");
    stateHelper.setLogout();
  },
};

export default AuthService;