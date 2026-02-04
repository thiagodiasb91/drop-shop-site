import html from "./supplier-products.html?raw"
import { router, back, navigate } from "../../core/router.js"
import SupplierService from "../../services/suppliers.services.js"
import productService from "../../services/products.services.js"
import { AuthService } from "../../services/auth.service.js"



export function getData() {
  return {
    supplierId: null,
    supplier: {
      id: null,
      email: null
    },
    products: [],
    linkedProducts: [],
    search: '',
    filter: 'all',
    nextCursor: null,
    loading: true,

    async init() {
      console.log('page.supplier-products.init.called')
      await this.resolveSupplier()
      await this.loadLinkedProducts()
      await this.loadProducts(true)
      this.loading = false
    },

    async resolveSupplier() {
      const log_prefix = 'page.supplier-products.resolveSupplier'
      console.log(`${log_prefix}.call`);

      const params = router.current?.params ?? {}
      console.log(`${log_prefix}.params`, params)

      if (!params.supplierId) {
        Alpine.store('toast').open(
          'ID do fornecedor não informado',
          'error'
        )
        navigate('/')
      }

      const loggedUser = await AuthService.me()
      console.log(`${log_prefix}.loggedUser`, loggedUser)

      if (params.supplierId === 'me') {
        if (loggedUser.roles != 'supplier') {
          console.log(`${log_prefix}.loggedUser.supplier.not.allowed`, loggedUser.roles)
          Alpine.store('toast').open(
            'Você não tem permissão para acessar essa página',
            'error'
          )
          navigate('/')
        }
        this.supplierId = loggedUser.id
      }
      else {
        if (loggedUser.roles != 'admin') {
          console.log(`${log_prefix}.loggedUser.admin.not.allowed`, loggedUser.roles)
          Alpine.store('toast').open(
            'Você não tem permissão para acessar essa página',
            'error'
          )
          navigate('/')
        }

        this.supplierId = params.supplierId
      }
      console.log(`${log_prefix}.supplierId`, this.supplierId)
      await this.fetchSupplier()
      return
    },

    buildSkus(variations) {
      const result = []

      const walk = (index, current) => {
        if (index === variations.length) {
          result.push({
            key: JSON.stringify(current),
            attributes: current,
            supplierSku: '',
            costPrice: ''
          })
          return
        }

        const variation = variations[index]
        for (const option of variation.options) {
          walk(index + 1, {
            ...current,
            [variation.name]: option
          })
        }
      }

      walk(0, {})
      return result
    },

    formatSkuAttributes(attrs) {
      return Object.entries(attrs)
        .map(([k, v]) => `${k}: ${v}`)
        .join(' / ')
    },

    saveLinks() {
      const selected = this.products.filter(p => p.selected)

      if (!selected.length) {
        Alpine.store('toast').open(
          'Selecione ao menos um produto',
          'error'
        )
        return
      }

      for (const p of selected) {
        for (const sku of p.skus) {
          if (!sku.supplierSku || !sku.costPrice) {
            Alpine.store('toast').open(
              'Preencha todos os SKUs e preços',
              'error'
            )
            return
          }
        }
      }

      const payload = {
        supplierId: this.supplierId,
        products: selected.map(p => ({
          productId: p.id,
          skus: p.skus
        }))
      }

      console.log('Payload:', payload)

      Alpine.store('toast').open('Vínculos salvos com sucesso')
    },
    get filteredProducts() {
      let list = this.products
      if (this.filter === 'linked') {
        list = list.filter(p => p.selected)
      }

      if (this.filter === 'unlinked') {
        list = list.filter(p => !p.selected)
      }

      if (this.search) {
        const q = this.search.toLowerCase()
        list = list.filter(p =>
          p.name.toLowerCase().includes(q)
        )
      }

      return list
    },

    goBack() {
      back()
    },
    async fetchSupplier() {
      this.supplier = SupplierService.get(this.supplierId) || {
        id: null,
        email: null
      }
    },

    async loadProducts(reset = false) {
      if (reset) {
        this.products = []
        this.nextCursor = null
      }

      const data = productService.getAll()
      console.log('page.supplier-products.loadProducts.data', data)

      data.items.forEach(p => {
        const baseSkus = this.buildSkus(p.variations)
        console.log('page.supplier-products.loadProducts.baseSkus', baseSkus)


        const linked = this.linkedProducts.find(
          lp => lp.productId === p.id
        )
        console.log('page.supplier-products.loadProducts.linked', linked)

        if (linked) {
          // produto já vinculado
          baseSkus.forEach(sku => {
            const found = linked.skus.find(
              l =>
                JSON.stringify(l.attributes) ===
                JSON.stringify(sku.attributes)
            )

            if (found) {
              sku.supplierSku = found.supplierSku
              sku.costPrice = found.costPrice
            }
          })
        }

        this.products.push({
          ...p,
          selected: !!linked,
          skus: baseSkus
        })
      })


      this.nextCursor = data.nextCursor
    },
    async loadLinkedProducts() {
      console.log('page.supplier-products.loadLinkedProducts.called')
      // mock backend GET /suppliers/:supplierId/products
      this.linkedProducts = SupplierService.getLinkedProducts(this.supplierId)
    },
  }
}

export function render() {
  return html
}
