import { ENV } from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"
import CacheHelper from "../utils/cache.helper.js"

const SuppliersService = {
  basePath: `${ENV.API_BASE_URL}/suppliers`,
  async save(supplier) {
    console.log("SuppliersService.save.request", supplier)

    const res = await fetch(
      `${this.basePath}?`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${CacheHelper.get("session_token")}`
        },
        body: JSON.stringify(supplier),
      }
    )

    return responseHandler(res)
  },
  async get(supplierId) {
    console.log("SuppliersService.get.request", supplierId)

    const res = await fetch(
      `${this.basePath}/${supplierId}`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    )

    return responseHandler(res)
  },
  async getLinkedProducts() {
    console.log("SuppliersService.get.request")

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
  async getLinkedProductSkus(productId) {
    console.log("SuppliersService.getLinkedProductSkus.request", productId)
    const res = await fetch(
      `${this.basePath}/products/${productId}/skus`,
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
  async linkProduct(productId, data) {
    const res = await fetch(`${this.basePath}/products/${productId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
      body: JSON.stringify(data),
    });
    return responseHandler(res);
  },
  async updateSkuSupplierAndPrice(productId, sku, skuSupplier, price) {
    const body = {
      skuSupplier,
      price
    }
    const res = await fetch(`${this.basePath}/products/${productId}/skus/${sku}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
      body: JSON.stringify(body),
    });
    return responseHandler(res);
  },
  async updateProductStock(productId, sku, stock) {
    const body = {
      quantity: stock,
    }
    const res = await fetch(`${this.basePath}/products/${productId}/skus/${sku}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${CacheHelper.get("session_token")}`
      },
      body: JSON.stringify(body),
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

export default SuppliersService
