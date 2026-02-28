import html from "./link-products.html?raw";
import SellersService from "../../../services/sellers.service";
import ProductsService from "../../../services/products.service";
import stateHelper from "../../../utils/state.helper.js";
import logger from "../../../utils/logger.js";

export function getData() {
  return {
    loading: true,

    search: "",
    filter: "all",
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
        );

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
          };
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
        return original && p.selectedSupplierId && (p.selectedSupplierId !== original.supplierId || p.salePrice !== original.salePrice);
      }).length;
    },
    get hasChanges() {
      return this.products.some(p => {
        const original = this.originalLinks.get(p.id);

        // Se não tinha vínculo e agora tem um fornecedor selecionado
        if (!original && p.selectedSupplierId) return true;

        // Se tinha vínculo e agora foi removido (selectedSupplierId nulo)
        if (original && !p.selectedSupplierId) return true;

        // Se o fornecedor selecionado mudou em relação ao original
        if (original && p.selectedSupplierId !== original.supplierId) return true;

        // Se o preço mudou (mantendo a lógica que você já deve ter)
        if (original && p.salePrice !== original.salePrice) return true;

        return false;
      });
    },
    isChanged(product) {
      const original = this.originalLinks.get(product.id);
      if (!original) return !!product.selectedSupplierId;
      return product.selectedSupplierId !== original.supplierId || Number(product.salePrice) !== Number(original.salePrice);
    },
    async cancel() {
      await this.refresh();
    },

    get filteredProducts() {
      const q = this.search.toLowerCase();
      return this.products.filter(p => p.name.toLowerCase().includes(q) || p.id.includes(q));
    },
    hasInvalidProducts() {
      this.products.forEach(p => {
        if (p.selectedSupplierId && (!p.salePrice)) {
          p.hasError = true;
          p.expanded = true;
          p.errorText = "Preço obrigatório faltando";
          return true;
        } else if (p.selectedSupplierId && (p.salePrice < 1 || p.salePrice > 499999)) {
          p.hasError = true;
          p.expanded = true;
          p.errorText = "Preço deve ser entre R$ 1,00 e R$ 499.999,00";
          return true;
        } else {
          p.hasError = false;
          return false;
        }
      });

    },
    async saveLinks() {
      this.loading = true;

      const hasInvalid = this.hasInvalidProducts();

      if (hasInvalid) {
        this.showErrors = true;
        stateHelper.toast("Por favor, corrija os erros antes de salvar.", "error");
        this.loading = false;
        return;
      }

      this.saving = true;

      const toLink = this.products.filter(p => {
        const original = this.originalLinks.get(p.id);

        if (p.selectedSupplierId && !original)
          return true;
        if (original && p.selectedSupplierId !== original.supplierId)
          return true;

        return false;
      });
      const toUpdate = this.products.filter(p => {
        const orig = this.originalLinks.get(p.id);
        return orig && p.selectedSupplierId && (Number(p.salePrice) !== Number(orig.salePrice));
      });
      const toUnlink = this.products.filter(p => !p.selectedSupplierId && this.originalLinks.has(p.id));
      const promises = [
        ...toLink?.map(p => {
          logger.local(`Linkando ${p.id} para ${p.selectedSupplierId} com preço ${p.salePrice}`);
          return SellersService.linkProduct(p.id, p.selectedSupplierId, p.salePrice);
        }) || [],
        ...toUpdate?.map(p => {
          logger.local(`Atualizando link ${p.id} para ${p.selectedSupplierId} com preço ${p.salePrice}`);
          return SellersService.updateProductLink(p.id, p.selectedSupplierId, p.salePrice);
        }) || [],
        ...toUnlink?.map(p => {
          logger.local(`Removendo link ${p.id}`);
          const originalData = this.originalLinks.get(p.id);
          logger.local(`Removendo link do produto ${p.id} com o fornecedor ${originalData.supplierId}`);
          return SellersService.unlinkProduct(p.id, originalData.supplierId);
        }) || []
      ];

      try {
        logger.local("Executando as seguintes operações:");
        const responses = await Promise.all(promises);
        const hasAnyErrors = responses.filter(p => p.ok === false);
        if (hasAnyErrors.length > 0) {
          stateHelper.toast(`Algumas alterações não puderam ser salvas.(${hasAnyErrors.length}/${responses.length})`, "error");
        } else {
          stateHelper.toast("Vínculos atualizados com sucesso", "success");
        }
        await this.refresh();
      } catch (e) {
        logger.error("Erro ao processar salvamento:", e);
        stateHelper.toast("Erro ao salvar algumas alterações", "error");
      } finally {
        this.saving = false;
        this.loading = false;
      }
    },

    goBack() {
      history.back();
    }
  };
}

export function render() {
  logger.local("page.vierw-stock.render.loaded");
  return html;
}
