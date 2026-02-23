import { ENV } from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"
import CacheHelper from "../utils/cache.helper.js"

const ProductsService = {
  basePath: `${ENV.API_BASE_URL}/products`,
  async getAll() {
    console.log("ProductsService.getAll.request")
    const res = await fetch(
      `${this.basePath}`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    )
    return responseHandler(res)
  },
  async getAllWithSkus() {
    const productsResponse = await ProductsService.getAll()

    if (!productsResponse.ok) {
      console.error('Erro ao consultar os produtos.');
      return productsResponse
    }

    const products = await Promise.all(productsResponse.response.map(async (p) => {
      const skusResponse = await ProductsService.getSkusByProductId(p.id)

      if (!skusResponse.ok) {
        console.error('Erro ao consultar os skus.');
        return skusResponse
      }
      const skus = skusResponse.response;

      const sizes = [...new Set(skus.map(s => s.size).filter(Boolean))];
      const colors = [...new Set(skus.map(s => s.color).filter(Boolean))];

      return {
        ...p,
        skus: skus,
        displayVariations: { sizes, colors }
      }
    }))

    return {
      ok: true,
      response: products
    }
  },
  async getSkusByProductId(productId) {
    console.log("SkusService.getSkusByProductId.request", productId)
    const res = await fetch(
      `${this.basePath}/${productId}/skus`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    )
    return responseHandler(res)
  },
  async create(data) {
    const res = await fetch(`${this.basePath}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data)
    });
    return responseHandler(res);
  },

  async update(id, data) {
    const res = await fetch(`${this.basePath}/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data)
    });
    return responseHandler(res);
  },

  async createSku(productId, skuData) {
    const res = await fetch(`${this.basePath}/${productId}/skus`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(skuData)
    });
    return responseHandler(res);
  },

  async deleteSku(productId, skuId) {
    const res = await fetch(
      `${this.basePath}/${productId}/skus/${skuId}`,
      {
        method: "DELETE"
      }
    );
    return responseHandler(res);
  },

  async delete(id) {
    const res = await fetch(
      `${this.basePath}/${id}`,
      {
        method: "DELETE"
      }
    );
    return responseHandler(res);
  },
  async getProductSuppliers(productId) {
    console.log("ProductsService.getSuppliersForProduct.request", productId)
    const res = await fetch(
      `${this.basePath}/${productId}/suppliers`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${CacheHelper.get("session_token")}`
        },
      }
    )
    return responseHandler(res)
  }
}

export default ProductsService

//