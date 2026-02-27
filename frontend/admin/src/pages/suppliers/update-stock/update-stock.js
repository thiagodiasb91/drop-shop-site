import html from "./update-stock.html?raw";
import ProductsService from "../../../services/products.service";
import SuppliersService from "../../../services/suppliers.service";
import stateHelper from "../../../utils/state.helper.js";
import logger from "../../../utils/logger.js";

export function getData() {
  return {
    loading: true,
    search: "",
    supplierEmail: "",

    products: [],
    hasChanges: false,
    changedCount: 0,

    async init() {
      this.loading = true;
      const logged = stateHelper.user;
      this.supplierEmail = logged?.user?.email;

      this.products = await this.fetchProducts();

      // guarda estoque original
      this.products.forEach(p =>
        p.skus.forEach(sku => {
          sku.originalStock = sku.stock;
        })
      );

      this.updateChanges();
      this.loading = false;
    },

    async fetchProducts() {
      try {
        const productsResponse = await SuppliersService.getLinkedProducts();
        const linkedProducts = productsResponse.response || [];
        let response = [];
        for (const p of linkedProducts) {
          const [skusRes, linkedSkusRes] = await Promise.all([
            ProductsService.getSkusByProductId(p.productId),
            SuppliersService.getLinkedProductSkus(p.productId)
          ]);

          const platformSkus = skusRes.response || [];
          const linkedSkus = linkedSkusRes.response || [];

          let productSkus = [];
          for (const s of platformSkus) {
            const linkedSku = linkedSkus.find(ls => ls.sku === s.sku);
            productSkus.push({
              sku: linkedSku.sku,
              skuSupplier: linkedSku.skuSupplier,
              color: s.color || "Padrão",
              size: s.size || "ÚNico",
              stock: linkedSku.quantity,
              originalStock: linkedSku.quantity,
              changed: false
            });
          }

          p.open = (linkedSkus.length > 0);
          if (productSkus.length > 0) {
            response.push({
              id: p.productId,
              name: p.productName,
              imageUrl: p.productImage || `https://picsum.photos/${Math.floor(Math.random() * 500)}?text=Produto+${p.productId}`,
              open: false, // Começa fechado para melhor visualização
              stock: productSkus.reduce((acc, cur) => acc + cur.stock, 0),
              skus: productSkus
            });
          }
        }
        return response;
      } catch (error) {
        logger.error("Erro ao carregar produtos:", error);
        stateHelper.toast("Erro ao carregar lista de estoque", "error");
        return [];
      }
    },

    getGroupedSkus(product) {
      if (!product.skus) return {};
      return product.skus.reduce((acc, sku) => {
        const color = sku.color || "Padrão";
        if (!acc[color]) acc[color] = [];
        acc[color].push(sku);
        return acc;
      }, {});
    },
    markAsChanged(sku) {
      // Garante que o valor seja numérico e trata campos vazios como 0
      if (sku.stock === "" || sku.stock === null) sku.stock = 0;

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

      const promises = [];
      const changedList = [];

      for (const p of this.products) {
        for (const s of p.skus) {
          if (s.stock < 0) {
            s.error = true;
            this.loading = false;
          }
          if (s.changed) {
            changedList.push(s);
            promises.push(SuppliersService.updateProductStock(p.id, s.sku, s.stock));
          }
        }
      }

      const hasErrors = this.products.some(p => p.skus.some(s => s.error));
      if (hasErrors) {
        stateHelper.toast("Corrija os campos com erros antes de salvar.", "error");
        return;
      }

      try {
        const results = await Promise.all(promises);
        const errors = results.filter(r => !r.ok);

        if (errors.length > 0) {
          stateHelper.toast(`Erro em ${errors.length} atualizações.`, "error");
        } else {
          stateHelper.toast(`${changedList.length} SKU(s) atualizado(s) com sucesso`, "success");
          // Reinicia para limpar os estados de "changed" e resetar o originalStock
          await this.init();
        }
      } catch (ex) {
        logger.error("Erro ao salvar estoque", ex);
        stateHelper.toast("Erro crítico ao salvar estoque.","error" );
      } finally {
        this.loading = false;
      }
    },
    get filteredProducts() {
      if (!this.search) return this.products;

      const term = this.search.toLowerCase();

      return this.products.filter(p =>
        p.name.toLowerCase().includes(term) ||
        p.skus.some(s => 
          s.color.toLowerCase().includes(term) || 
          s.skuSupplier.toLowerCase().includes(term)
        )
      );
    }
  };
}

export function render() {
  return html;
}
