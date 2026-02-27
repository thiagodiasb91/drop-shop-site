import BaseApi from "./base.api";
import logger from "../utils/logger";

const baseApi = new BaseApi("/products");

const ProductsService = {
  async getAll() {
    return baseApi.call("/", {
      method: "GET",
      headers: { "Content-Type": "application/json" },
    });
  },
  async getAllWithSkus() {
    const productsResponse = await ProductsService.getAll();

    if (!productsResponse.ok) {
      logger.error("Erro ao consultar os produtos.", productsResponse.response);
      return productsResponse;
    }

    const products = await Promise.all(productsResponse.response.map(async (p) => {
      const skusResponse = await ProductsService.getSkusByProductId(p.id);

      if (!skusResponse.ok) {
        logger.error("Erro ao consultar os skus.", skusResponse.response);
        return skusResponse;
      }
      const skus = skusResponse.response;

      const sizes = [...new Set(skus.map(s => s.size).filter(Boolean))];
      const colors = [...new Set(skus.map(s => s.color).filter(Boolean))];

      return {
        ...p,
        skus: skus,
        displayVariations: { sizes, colors }
      };
    }));

    return {
      ok: true,
      response: products
    };
  },
  async getSkusByProductId(productId) {
    return baseApi.call(`/${productId}/skus`, {
      method: "GET",
    });
  },
  async create(data) {
    return baseApi.call("/", {
      method: "POST",
      body: JSON.stringify(data)
    });
  },

  async update(id, data) {
    return baseApi.call(`/${id}`, {
      method: "PUT",
      body: JSON.stringify(data)
    });
  },

  async updateSku(id, data) {
    return baseApi.call(`/${id}`, {
      method: "PUT",
      body: JSON.stringify(data)
    });
  },

  async createSku(productId, skuData) {
    return baseApi.call(`/${productId}/skus`, {
      method: "POST",
      body: JSON.stringify(skuData)
    });
  },

  async deleteSku(productId, skuId) {
    return baseApi.call(`/${productId}/skus/${skuId}`, {
      method: "DELETE"
    });
  },

  async delete(id) {
    return baseApi.call(`/${id}`, {
      method: "DELETE"
    });
  },
  async getProductSuppliers(productId) {
    return baseApi.call(`/${productId}/suppliers`, {
      method: "GET",
    });
  }
};

export default ProductsService;