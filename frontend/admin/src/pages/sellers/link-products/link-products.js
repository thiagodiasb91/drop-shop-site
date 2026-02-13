import html from "./link-products.html?raw"
import SellersService from "../../../services/sellers.services"
import ProductsService from "../../../services/products.services"

export function getData() {
  return {
    loading: true,

    search: '',
    filter: 'all',
    originalLinks: new Map(),

    products: [],

    page: 0,
    pageSize: 6,
    nextCursor: true,

    async init() {
      await this.refresh();
    },

    async refresh() {
      this.loading = true;
      const [resLinkedProducts, resProductsWithSuppliers] = await Promise.all([
        SellersService.getLinkedProducts(),
        SellersService.getProductsWithSuppliers()
      ]);

      if (resProductsWithSuppliers.ok) {
        this.originalLinks = new Map(
          resLinkedProducts.response?.map(p => [p.productId, p.supplierId])
        )

        this.products = resProductsWithSuppliers.response.map(p => {
          const initialSupplierId = this.originalLinks.has(p.id) || null;
          return {
            ...p,
            expanded: !!initialSupplierId,
            selectedSupplierId: initialSupplierId,
            suppliers: [],
            loadingSuppliers: false,
          }
        });

        const loadInitialSuppliers = this.products
          .filter(p => p.selectedSupplierId)
          .map(p => this.fetchSuppliers(p));

        await Promise.all(loadInitialSuppliers);

      }
      this.loading = false;
    },
    async fetchSuppliers(product) {
      if (product.suppliers.length > 0) return;

      product.loadingSuppliers = true;
      const res = await ProductsService.getProductSuppliers(product.id);

      if (res.ok) {
        product.suppliers = res.response.map(s => {
          const min = s.minPrice
          const max = s.maxPrice
          return {
            ...s,
            priceDisplay: min === max ? `R$ ${min.toFixed(2)}` : `R$ ${min.toFixed(2)} - ${max.toFixed(2)}`
          };
        });
      }
      product.loadingSuppliers = false;
    },
    async toggleExpand(product) {
      product.expanded = !product.expanded;
      if (product.expanded) {
        await this.fetchSuppliers(product);
      }
    },
    /* ======================
       ACTIONS
    ====================== */
    // Getters para a barra Sticky
    get includedCount() {
      return this.products.filter(p => p.selectedSupplierId && !this.originalLinks.has(p.id)).length;
    },
    get removedCount() {
      return this.products.filter(p => !p.selectedSupplierId && this.originalLinks.has(p.id)).length;
    },
    get hasChanges() {
      return this.includedCount > 0 || this.removedCount > 0;
    },
    async cancel() {
      await this.refresh();
    },

    get filteredProducts() {
      const q = this.search.toLowerCase();
      return this.products.filter(p => p.name.toLowerCase().includes(q) || p.id.includes(q));
    },
    async saveLinks() {
      const toLink = this.products.filter(p => p.selectedSupplierId && !this.originalLinks.has(p.id));
      const toUnlink = this.products.filter(p => !p.selectedSupplierId && this.originalLinks.has(p.id));
      const promises = [
        toLink?.map(p =>
          // SellersService.linkProduct(p.id, p.selectedSupplierId)
          console.log(`Linkando ${p.id} para ${p.selectedSupplierId}`)
        ),
        toUnlink?.map(p =>
          // p => SellersService.unlinkProduct(p.id)
          console.log(`Removendo link ${p.id} para ${p.selectedSupplierId}`)
        )
      ];

      try {
        await Promise.all(promises);
        Alpine.store('toast').open('Vínculos atualizados com sucesso', 'success');
        await this.refresh();
      } catch (e) {
        Alpine.store('toast').open('Erro ao salvar algumas alterações', 'error');
      }
    },

    goBack() {
      history.back()
    }
  }
}

export function render() {
  console.log("page.vierw-stock.render.loaded");
  return html;
}
