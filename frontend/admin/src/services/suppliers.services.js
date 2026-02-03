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