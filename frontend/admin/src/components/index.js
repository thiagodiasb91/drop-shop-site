import globalLoaderHtml from "./loader.html?raw";

/**
 * Retorna o HTML do loader com uma mensagem personalizada
 * @param {string} message 
 */
const renderGlobalLoader = (message = "Carregando dados...") => {
  return globalLoaderHtml.replace("{{MESSAGE}}", message);
};

export { 
  renderGlobalLoader 
};