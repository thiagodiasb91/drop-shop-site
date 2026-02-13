import { ENV } from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"
import CacheHelper from "../utils/cache.helper.js"

const SellersService = {
  basePath: `${ENV.API_BASE_URL}/sellers`,
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
  async linkProduct(productId, supplierId) {
    const res = await fetch(`${this.basePath}/products/${productId}/link`, {
      method: "POST",
      headers: { 
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
      body: JSON.stringify({ supplierId })
    });
    return responseHandler(res);
  },

  async unlinkProduct(productId) {
    const res = await fetch(`${this.basePath}/products/${productId}`, {
      method: "DELETE",
      headers: { 
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
    });
    return responseHandler(res);
  }
}

export default SellersService
