import html from "./link-products.html?raw"

export function getData(){
  return {
    loading: true,

    search: '',
    filter: 'all',

    products: [],
    originalLinks: new Set(),

    page: 0,
    pageSize: 6,
    nextCursor: true,

    init() {
      setTimeout(() => {
        this.loadProducts()
        this.loading = false
      }, 600)
    },

    loadProducts() {
      if (!this.nextCursor) return

      const newProducts = generateMockProducts(this.page, this.pageSize)

      newProducts.forEach(p => {
        if (p.linked) {
          this.originalLinks.add(p.id)
        }
      })

      this.products.push(...newProducts)

      this.page++
      if (this.page >= 3) this.nextCursor = false
    },

    /* ======================
       COMPUTED – HEADER
    ====================== */

    get totalProducts() {
      return this.products.length
    },

    get linkedProducts() {
      return this.products.filter(p => p.linked).length
    },

    get syncStats() {
      const synced = this.products.filter(p => p.linked && p.synced).length
      const pending = this.products.filter(p => p.linked && !p.synced).length
      return { synced, pending }
    },

    get includedCount() {
      return this.products.filter(
        p => p.linked && !this.originalLinks.has(p.id)
      ).length
    },

    get removedCount() {
      return this.products.filter(
        p => !p.linked && this.originalLinks.has(p.id)
      ).length
    },

    get hasChanges() {
      return this.includedCount > 0 || this.removedCount > 0
    },

    /* ======================
       FILTER / SEARCH
    ====================== */

    get filteredProducts() {
      let list = this.products

      if (this.filter === 'linked') {
        list = list.filter(p => p.linked)
      }

      if (this.filter === 'unlinked') {
        list = list.filter(p => !p.linked)
      }

      if (this.search.trim()) {
        const q = this.search.toLowerCase()

        list = list.filter(p =>
          p.name.toLowerCase().includes(q) ||
          p.description.toLowerCase().includes(q) ||
          p.id.toLowerCase().includes(q) ||
          p.marketplaceId.toLowerCase().includes(q) ||
          p.skus.some(sku =>
            sku.id.toLowerCase().includes(q) ||
            sku.label.toLowerCase().includes(q)
          )
        )
      }

      return list
    },

    getTotalSkuProducts(product) {
      return product.skus.reduce((sum, sku) => sum + sku.stock, 0)
    },
    /* ======================
       ACTIONS
    ====================== */

    saveLinks() {
      if (!this.hasChanges) return

      const included = this.products
        .filter(p => p.linked && !this.originalLinks.has(p.id))
        .map(p => p.id)

      const removed = this.products
        .filter(p => !p.linked && this.originalLinks.has(p.id))
        .map(p => p.id)

      console.log('INCLUIR:', included)
      console.log('REMOVER:', removed)

      // simula sucesso
      this.originalLinks = new Set(
        this.products.filter(p => p.linked).map(p => p.id)
      )

      alert('Vínculos salvos com sucesso (mock)')
    },

    goBack() {
      history.back()
    }
  }
}
function generateMockProducts(page, size) {
  const baseIndex = page * size

  return Array.from({ length: size }).map((_, i) => {
    const index = baseIndex + i + 1
    const linked = Math.random() > 0.5
    const synced = linked && Math.random() > 0.3

    const skus = generateMockSkus(index)

    return {
      id: `PROD-${1000 + index}`,
      marketplaceId: `MP-${5000 + index}`,
      name: `Camiseta Premium ${index}`,
      description: `Camiseta de algodão fio 30.1 — ref ${index}`,
      expanded: false,
      linked,
      synced,
      skus
    }
  })
}

function generateMockSkus(productIndex) {
  const variations = [
    { color: 'Preto', size: 'P' },
    { color: 'Preto', size: 'M' },
    { color: 'Preto', size: 'G' },
    { color: 'Branco', size: 'M' },
    { color: 'Branco', size: 'G' }
  ]

  return variations.map((v, i) => ({
    id: `SKU-${productIndex}-${i + 1}`,
    label: `${v.color} / ${v.size}`,
    stock: Math.floor(Math.random() * 40)
  }))
}

export function render() {
  console.log("page.vierw-stock.render.loaded");
  return html;
}
