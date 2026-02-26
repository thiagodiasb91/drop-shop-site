import ENV from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"
import CacheHelper from "../utils/cache.helper.js"
import BaseApi from "./base.api.js"

const api = new BaseApi("/suppliers")

const SuppliersService = {
  async save(supplier) {
    console.log("SuppliersService.save.request", supplier)
    return api.call(
      `/`,
      {
        method: "POST",
        body: JSON.stringify(supplier),
      }
    )
  },
  async get(supplierId) {
    console.log("SuppliersService.get.request", supplierId)

    return api.call(
      `/${supplierId}`,
      {
        method: "GET"
      }
    )
  },
  async getLinkedProducts() {
    console.log("SuppliersService.get.request")

    return api.call(
      `/products`,
      {
        method: "GET",
      }
    )
  },
  async getLinkedProductSkus(productId) {
    console.log("SuppliersService.getLinkedProductSkus.request", productId)
    return api.call(
      `/products/${productId}/skus`,
      {
        method: "GET",
      }
    )
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
    }
    return api.call(`/products/${productId}/skus/${sku}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  },
  async updateProductStock(productId, sku, stock) {
    const body = {
      quantity: stock,
    }
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
}

export default SuppliersService
