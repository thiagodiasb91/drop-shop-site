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
    showErrors: false,
    saving: false,
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
        const linkedProducts = resLinkedProducts.ok ? resLinkedProducts.response : [];
        this.originalLinks = new Map(
          linkedProducts.map(p => [p.productId, {
            supplierId: p.supplierId,
            salePrice: p.price || null
          }])
        )

        this.products = resProductsWithSuppliers.response.map(p => {
          const original = this.originalLinks.get(p.id);

          return {
            ...p,
            expanded: !!original,
            selectedSupplierId: original?.supplierId || null,
            salePrice: original?.salePrice || null,
            suppliers: [],
            loadingSuppliers: false,
            hasError: false
          }
        });
        this.showErrors = false;

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
          const min = s.skus.reduce((acc, sku) => Math.min(acc, sku.price), Infinity);
          const max = s.skus.reduce((acc, sku) => Math.max(acc, sku.price), -Infinity);
          return {
            ...s,
            priceDisplay: min === max ? `R$ ${min.toFixed(2)}` : `R$ ${min.toFixed(2)} - ${max.toFixed(2)}`
          };
        });
      }
      product.loadingSuppliers = false;
    },
    async handleCheckboxToggle(product, isChecked) {
      if (isChecked) {
        if (!product.selectedSupplierId && product.suppliers.length > 0) {
          product.selectedSupplierId = product.suppliers[0].supplierId;
        }

        product.expanded = true;

        await this.fetchSuppliers(product);

        if (!product.selectedSupplierId && product.suppliers.length > 0) {
          product.selectedSupplierId = product.suppliers[0].supplierId;
        }
      } else {
        product.selectedSupplierId = null;
        product.expanded = false;
        product.hasError = false;
      }
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
    get includedCount() {
      return this.products.filter(p => p.selectedSupplierId && !this.originalLinks.has(p.id)).length;
    },
    get removedCount() {
      return this.products.filter(p => !p.selectedSupplierId && this.originalLinks.has(p.id)).length;
    },
    get updatedCount() {
      return this.products.filter(p => {
        const original = this.originalLinks.get(p.id);
        return p.selectedSupplierId &&
          original &&
          p.selectedSupplierId === original.supplierId &&
          p.salePrice !== original.salePrice;
      }).length;
    },
    get hasChanges() {
      return this.includedCount > 0 || this.removedCount > 0 || this.updatedCount > 0;
    },
    async cancel() {
      await this.refresh();
    },

    get filteredProducts() {
      const q = this.search.toLowerCase();
      return this.products.filter(p => p.name.toLowerCase().includes(q) || p.id.includes(q));
    },
    async saveLinks() {
      this.loading = true;
      let hasInvalid = false;
      this.products.forEach(p => {
        if (p.selectedSupplierId && (!p.salePrice)) {
          p.hasError = true;
          p.expanded = true;
          hasInvalid = true;
          p.errorText = "Preço obrigatório faltando";
        } else if (p.selectedSupplierId && (p.salePrice < 1 || p.salePrice > 499999)) {
          p.hasError = true;
          p.expanded = true;
          hasInvalid = true;
          p.errorText = "Preço deve ser entre R$ 1,00 e R$ 499.999,00";
        } else {
          p.hasError = false;
        }
      });

      if (hasInvalid) {
        this.showErrors = true;
        Alpine.store('toast').open('Por favor, corrija os erros antes de salvar.', 'error');
        this.loading = false;
        return;
      }

      this.saving = true;

      const toLink = this.products.filter(p => p.selectedSupplierId && !this.originalLinks.has(p.id));
      const toUpdate = this.products.filter(p => {
        const orig = this.originalLinks.get(p.id);
        return orig && p.selectedSupplierId && (p.selectedSupplierId !== orig.supplierId || Number(p.salePrice) !== Number(orig.salePrice));
      });
      const toUnlink = this.products.filter(p => !p.selectedSupplierId && this.originalLinks.has(p.id));
      const promises = [
        ...toLink?.map(p => {
          console.log(`Linkando ${p.id} para ${p.selectedSupplierId} com preço ${p.salePrice}`)
          return SellersService.linkProduct(p.id, p.selectedSupplierId, p.salePrice);
        }),
        ...toUpdate?.map(p => {
          console.log(`Atualizando link ${p.id} para ${p.selectedSupplierId} com preço ${p.salePrice}`)
          return SellersService.updateProductLink(p.id, p.selectedSupplierId, p.salePrice);
        }),
        ...toUnlink?.map(p => {
          console.log(`Removendo link ${p.id}`)
          const originalData = this.originalLinks.get(p.id);
          console.log(`Removendo link do produto ${p.id} com o fornecedor ${originalData.supplierId}`);
          return SellersService.unlinkProduct(p.id, originalData.supplierId);
        })
      ];

      try {
        console.log("Executando as seguintes operações:");
        await Promise.all(promises);
        Alpine.store('toast').open('Vínculos atualizados com sucesso', 'success');
        await this.refresh();
      } catch (e) {
        console.error("Erro ao processar salvamento:", e);
        Alpine.store('toast').open('Erro ao salvar algumas alterações', 'error');
      } finally {
        this.saving = false;
        this.loading = false;
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
