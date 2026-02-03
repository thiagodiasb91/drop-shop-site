import { ENV } from "../config/env.js"
import { AuthService } from "./auth.service.js"

export const SupplierService = {
  async getPayments() {
    try {
      const res = await fetch(`${ENV.API_BASE_URL}/bff/payments`, {
        credentials: "include",
        headers: {
          ...await AuthService.getAuthHeader()
        },
      })

      if (!res.ok) {
        throw new Error(`Erro ao buscar pagamentos aos fornecedores: ${res.statusText}`)
      }

      return res.json()
    } catch (error) {
      console.error("SupplierService.getPayments error:", error)
      throw error
    }
  } 

}

const get = (supplierId) => {
  const supplierList = [
    {
      id: 1,
      email: 'joao.silva@example.com',
    },
    {
      id: 2,
      email: 'maria.souza@example.com',
    },
    {
      id: 3,
      email: 'carlos.pereira@example.com',
    },
    {
      id: 4,
      email: 'ana.lima@example.com',
    },]

  setTimeout(() => {}, 1000)


  return supplierList.find(s => String(s.id) === supplierId)
}

const getLinkedProducts = (supplierId) => {
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

export default {
  get,
  getLinkedProducts
}