const getAll = () => {
  return {
    items: [
      {
        id: 'PROD-01',
        name: 'Camiseta Básica',
        variations: [
          { name: 'Cor', options: ['Branco', 'Preto'] },
          { name: 'Tamanho', options: ['P', 'M', 'G'] }
        ]
      },
      {
        id: 'PROD-02',
        name: 'Calça Jeans',
        variations: [
          { name: 'Cor', options: ['Azul', 'Preto'] },
          { name: 'Tamanho', options: ['P', 'M', 'G'] }
        ]
      },
      {
        id: 'PROD-03',
        name: 'Tênis Esportivo',
        variations: [
          { name: 'Cor', options: ['Branco', 'Preto'] },
          { name: 'Tamanho', options: ['40', '42', '44'] }
        ]
      },
      {
        id: 'PROD-04',
        name: 'Jaqueta de Couro',
        variations: [
          { name: 'Cor', options: ['Marrom', 'Cinza'] },
          { name: 'Tamanho', options: ['P', 'M', 'G'] }
        ]
      },
      {
        id: 'PROD-05',
        name: 'Boné de Couro',
        variations: [
          { name: 'Cor', options: ['Branco', 'Preto'] },
          { name: 'Tamanho', options: ['Único'] }
        ]
      }
    ],
    nextCursor: null
  }
}

export default { getAll }

//