import logger from "./utils/logger.js";
import BaseApi from "./base.api.js";

const api = new BaseApi("/sellers");

const SellersService = {
  async getProductSkus(productId) {
    return api.call(`/products/${productId}/skus`, {
      method: "GET",
    });
  },
  async getProductsWithSuppliers() {
    logger.local("SellersService.getProductsWithSuppliers.request");

    return api.call(
      "/products/available",
      {
        method: "GET",
      }
    );
  },
  async getLinkedProducts() {
    logger.local("SellersService.getLinkedProducts.request");

    return api.call(
      "/products",
      {
        method: "GET",
      }
    );
  },
  async linkProduct(productId, supplierId, salePrice) {
    return api.call(`/products/${productId}/suppliers/${supplierId}`, {
      method: "POST",
      body: JSON.stringify({ "price": salePrice })
    });
  },
  async updateProductLink(productId, supplierId, salePrice) {
    return api.call(
      `${this.basePath}/products/${productId}/suppliers/${supplierId}`, {
      method: "PUT",
      body: JSON.stringify({ "price": salePrice })
    });
  },

  async unlinkProduct(productId, supplierId) {
    return api.call(
      `${this.basePath}/products/${productId}/suppliers/${supplierId}`, {
      method: "DELETE",
    });
  }
};

export default SellersService;
