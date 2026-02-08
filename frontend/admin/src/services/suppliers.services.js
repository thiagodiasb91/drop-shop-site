import { ENV } from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"

const SuppliersService = {
  basePath: `${ENV.API_BASE_URL}/suppliers`,
  async save(supplier) {
    console.log("SuppliersService.save.request", supplier)

    const res = await fetch(
      `${this.basePath}?`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
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
  getLinkedProducts (supplierId) {
    return [
      {
        productId: 'PROD-05',
        skus: [
          {
            attributes: { Cor: 'Branco', Tamanho: 'Único' },
            supplierSku: 'CAM-BR-P',
            costPrice: 19.9
          },
          {
            attributes: { Cor: 'Preto', Tamanho: 'Único' },
            supplierSku: 'CAM-PR-G',
            costPrice: 24.9
          }
        ]
      }
    ]
  }
}

export default SuppliersService
