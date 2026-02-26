import ENV from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"
import CacheHelper from "../utils/cache.helper.js"

const SellersService = {
  basePath: `${ENV.API_BASE_URL}/sellers`,
  async getProductSkus(productId) {
    const res = await fetch(`${this.basePath}/products/${productId}/skus`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
    });
    return responseHandler(res);
  },
  async getProductsWithSuppliers() {
    console.log("SellersService.getProductsWithSuppliers.request")

    const res = await fetch(
      `${this.basePath}/products/available`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    )

    return responseHandler(res)
  },
  async getLinkedProducts() {
    console.log("SellersService.getLinkedProducts.request")

    const res = await fetch(
      `${this.basePath}/products`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${CacheHelper.get("session_token")}`
        },
      }
    )

    return responseHandler(res)
  },
  async linkProduct(productId, supplierId, salePrice) {
    const res = await fetch(`${this.basePath}/products/${productId}/suppliers/${supplierId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
      body: JSON.stringify({ "price": salePrice })
    });
    return responseHandler(res);
  },
  async updateProductLink(productId, supplierId, salePrice) {
    const res = await fetch(`${this.basePath}/products/${productId}/suppliers/${supplierId}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
      body: JSON.stringify({ "price": salePrice })
    });
    return responseHandler(res);
  },

  async unlinkProduct(productId, supplierId) {
    const res = await fetch(`${this.basePath}/products/${productId}/suppliers/${supplierId}`, {
      method: "DELETE",
      headers: {
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
    });
    return responseHandler(res);
  }
}

export default SellersService
