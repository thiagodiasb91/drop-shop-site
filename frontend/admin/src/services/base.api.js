import ENV from "../config/env.js";
import CacheHelper from "../utils/cache.helper.js";
import stateHelper from "../utils/state.helper.js";
import logger from "../utils/logger.js";

const BASE_URL = ENV.API_BASE_URL;

function handleUnauthorized(res) {
  if (res.status === 401) {
    console.warn("Sessão expirada ou inválida. Limpando dados...");

    stateHelper.setLogout();

    if (window.Alpine) {
      const authStore = Alpine.store("auth");
      if (authStore) authStore.user = null;
    }

    // window.location.href = "/login";
    return false;
  }
  return true;
}

class BaseApi {
  constructor(baseResource) {
    this.baseResource = baseResource;
  }

  call = async (endpoint, options = {}) => {
    const url = `${BASE_URL}${this.baseResource}${endpoint}`;

    const token = CacheHelper.get("session_token");
    const defaultHeaders = {
      "Content-Type": "application/json"      
    };

    if (token) {
      defaultHeaders["Authorization"] = `Bearer ${token}`;
    }
    
    const config = {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
    };

    try {
      const res = await fetch(url, config);

      if (!handleUnauthorized(res)) {
        return { ok: false, status: 401 };
      }

      res.ok = res.status >= 200 && res.status < 300;
      
      if (!res.ok) {
        const errorData = await res.json().catch(() => ({}));
        throw { 
          ok: false, 
          message: res.statusText, 
          status: res.status, 
          ...errorData 
        };
      }

      try{
        const data = await res.json();
        const response = data.items ? data.items : data;
        
        return {
          ok: true,
          status: res?.status,
          response
        };
      }
      catch(err){
        logger.error("Error parsing JSON response:", err);
        return {
          ok: false,
          status: res?.status,
          response: null
        };
      }

    } catch (error) {
      logger.error(`API Error [${endpoint}]:`, error);
      throw error;
    }
  };
}

export default BaseApi;