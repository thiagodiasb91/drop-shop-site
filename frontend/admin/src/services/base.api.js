import ENV from "../config/env.js";
import CacheHelper from "../utils/cache.helper.js";
import stateHelper from "../utils/state.helper.js";

const BASE_URL = ENV.API_BASE_URL;

function handleUnauthorized(res) {
  if (res.status === 401) {
    console.warn("Sessão expirada ou inválida. Limpando dados...");

    stateHelper.setLogout()

    if (window.Alpine) {
      const authStore = Alpine.store('auth');
      if (authStore) authStore.user = null;
    }

    // window.location.href = "/login";
    return false
  }
  return true
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

      if (!res.ok) {
        const errorData = await res.json().catch(() => ({}));
        throw { status: res.status, ...errorData };
      }

      try{
        const data = await res.json()
        const response = data.items ? data.items : data
        
        return {
          ok: true,
          status: res?.status,
          response
        };
      }
      catch(err){
        console.error("Error parsing JSON response:", err);
        return {
          ok: false,
          status: res?.status,
          response: null
        };
      }

    } catch (error) {
      console.error(`API Error [${endpoint}]:`, error);
      throw error;
    }
  };
}

export default BaseApi;