import html from "./update-stock.html?raw"
import ProductsService from "../../../services/products.services";
import SuppliersService from "../../../services/suppliers.services";
import stateHelper from "../../../utils/state.helper.js";

export function getData() {
  return {
    loading: true,
    search: '',
    supplierEmail: 'fornecedor@exemplo.com',

    products: [],
    hasChanges: false,
    changedCount: 0,

    async init() {
      // mock grande
      this.products = await this.fetchProducts()

      // guarda estoque original
      this.products.forEach(p =>
        p.skus.forEach(sku => {
          sku.originalStock = sku.stock;
        })
      );

      this.loading = false;
    },

    async fetchProducts() {
      const productsResponse = await SuppliersService.getLinkedProducts();
      const products = productsResponse.response || [];
      let response = []
      for (const p of products) {
        const skusResponse = await ProductsService.getSkusByProductId(p.productId);
        const skus = skusResponse.response || [];
        const linkedSkuResponse = await SuppliersService.getLinkedProductSkus(p.productId);
        const linkedSkus = linkedSkuResponse.response || [];
        p.skus = []

        for (const s of skus) {
          const linkedSku = linkedSkus.find(ls => ls.sku === s.sku);
          p.skus.push({
            sku: linkedSku.sku,
            skuSupplier: linkedSku.skuSupplier,
            color: s.color,
            size: s.size,
            stock: linkedSku.quantity 
          })
        }

        p.open = (linkedSkus.length > 0)

        response.push({
          id: p.productId,
          name: p.product_name,
          open: p.open,
          skus: p.skus
        })
      }
      return response;        
    },

    markAsChanged(sku) {
      sku.changed = sku.stock !== sku.originalStock;
      this.updateChanges();
    },

    updateChanges() {
      const changedSkus = this.products.flatMap(p =>
        p.skus.filter(s => s.changed)
      );

      this.changedCount = changedSkus.length;
      this.hasChanges = this.changedCount > 0;
    },

    async saveAll() {
      this.loading = true;
      const stocksToSave = this.products.flatMap(p =>
        p.skus
          .filter(s => s.changed)
          .map(s => ({
            productId: p.id,
            sku: s.sku,
            stock: s.stock
          }))
      );

      const promises = []

      for (const p of this.products) {
        for (const s of p.skus) {
          if (s.changed)
            promises.push(SuppliersService.updateProductStock(p.id, s.sku, s.stock))
        }
      }

      const res = await Promise.all(promises);
      console.log("saveAll.updateProductStock.response", res)

      const countErrors = res.filter(r => !r.ok).length;

      if (countErrors > 0) {
        console.error(`Erro ao salvar ${countErrors} SKU(s)`);
        stateHelper.toast(`Erro ao salvar ${countErrors} alterações.`, 'error');
        this.loading = false;
        return
      }

      this.products = await this.fetchProducts()
      
      this.updateChanges();
      this.loading = false;
      stateHelper.toast(`${stocksToSave.length} SKU(s) salvo(s) com sucesso`, 'success');
    },

    get filteredProducts() {
      if (!this.search) return this.products;

      const term = this.search.toLowerCase();

      return this.products.filter(p =>
        p.name.toLowerCase().includes(term) ||
        p.skus.some(s =>
          Object.values(s.attributes).some(v =>
            String(v).toLowerCase().includes(term)
          )
        )
      );
    }
  };
}

export function render() {
  return html
}
