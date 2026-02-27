import BaseApi from "./base.api.js";
import logger from "../utils/logger.js";

const api = new BaseApi("/suppliers");

const SuppliersService = {
  async save(supplier) {
    logger.local("SuppliersService.save.request", supplier);
    return api.call(
      "/",
      {
        method: "POST",
        body: JSON.stringify(supplier),
      }
    );
  },
  async get(supplierId) {
    logger.local("SuppliersService.get.request", supplierId);

    return api.call(
      `/${supplierId}`,
      {
        method: "GET"
      }
    );
  },
  async getLinkedProducts() {
    logger.local("SuppliersService.get.request");

    return api.call(
      "/products",
      {
        method: "GET",
      }
    );
  },
  async getLinkedProductSkus(productId) {
    logger.local("SuppliersService.getLinkedProductSkus.request", productId);
    return api.call(
      `/products/${productId}/skus`,
      {
        method: "GET",
      }
    );
  },
  async linkProduct(productId, data) {
    return api.call(`/products/${productId}`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
  async updateSkuSupplierAndPrice(productId, sku, skuSupplier, price) {
    const body = {
      skuSupplier,
      price
    };
    return api.call(`/products/${productId}/skus/${sku}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  },
  async updateProductStock(productId, sku, stock) {
    const body = {
      quantity: stock,
    };
    return api.call(`/products/${productId}/skus/${sku}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  },
  async unlinkProduct(productId) {
    return api.call(`/products/${productId}`, {
      method: "DELETE",
    });
  }
};

export default SuppliersService;
