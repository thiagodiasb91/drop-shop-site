import logger from "./logger.js";

const CacheHelper = {
  get(name) {
    const cached = sessionStorage.getItem(name);
    try{
      const response = JSON.parse(cached);
      logger.local("cache.get.response.json", name, response);
      return response;
    }
    catch{
      logger.local("cache.get.response", name, cached);
      return cached;
    }
  },
  set (name, value) {
    value = JSON.stringify(value);
    sessionStorage.setItem(name, value);
  },
  remove(name){
    try{
      sessionStorage.removeItem(name);
    } catch(err) {
      logger.error("cache.remove.error", err);
    }
  }
};

export default CacheHelper;