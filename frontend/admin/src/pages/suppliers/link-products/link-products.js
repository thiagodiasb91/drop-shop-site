import html from "./link-products.html?raw";
import { navigate } from "../../../core/router.js";
import SupplierService from "../../../services/suppliers.service.js";
import productService from "../../../services/products.service.js";
import stateHelper from "../../../utils/state.helper.js";
import logger from "../../../utils/logger.js";

export function getData() {
  return {
    supplierId: null,
    supplierEmail: null,
    products: [],
    linkedProducts: [],
    search: "",
    filter: "all",
    nextCursor: null,
    loading: true,
    originalSkuData: {},

    async init() {
      logger.local("page.supplier-products.init.called");
      await this.resolveSupplier();
      await this.loadLinkedProducts();
      await this.loadProducts(true);
      this.loading = false;
    },

    async resolveSupplier() {
      const log_prefix = "page.supplier-products.resolveSupplier";
      logger.local(`${log_prefix}.call`);

      const params = new URLSearchParams(location.search);
      logger.local(`${log_prefix}.params`, params);

      const loggedInfo = stateHelper.user;
      logger.local(`${log_prefix}.loggedInfo`, loggedInfo);


      if (loggedInfo.role === "supplier") {
        this.supplierId = loggedInfo.resourceId;
        this.supplierEmail = loggedInfo.user.email;
      }
      else if (loggedInfo.role === "admin") {
        const supplierId = params.get("supplierId");
        if (!supplierId) {
          logger.local(`${log_prefix}.loggedInfo.admin.supplierIdNotFound`);
          stateHelper.toast(
            "Supplier ID não informado",
            "error"
          );
          return;
        }
        this.supplierId = supplierId;
        await this.fetchSupplier();
      }
      else {
        logger.local(`${log_prefix}.loggedInfo.roleNotAllowed`, loggedInfo.role);
        stateHelper.toast(
          "Você não tem permissão para acessar essa página",
          "error"
        );
        navigate("/");
      }

      logger.local(`${log_prefix}.supplierId`, this.supplierId);
      return;
    },
    async fetchSupplier() {
      const supplier = await SupplierService.get(this.supplierId);
      this.supplierEmail = supplier?.email || "";
    },
    async loadLinkedProducts() {
      logger.local("page.supplier-products.loadLinkedProducts.called", this.supplierId);
      const linkedProducts = await SupplierService.getLinkedProducts(this.supplierId);
      logger.local("page.supplier-products.loadLinkedProducts.response", linkedProducts.response);
      this.linkedProducts = linkedProducts.ok ? linkedProducts.response : [];
    },
    async loadProducts(reset = false) {
      if (reset) {
        this.products = [];
        this.originalLinks = new Set();
        this.originalSkuData = {};
      }

      try {
        const products = await productService.getAll();
        logger.local("page.supplier-products.loadProducts.products", products);

        for (const p of products.response) {
          logger.local("page.supplier-products.loadProducts.linkedProducts", this.linkedProducts);

          const isLinked = this.linkedProducts?.some(lp => lp.productId === p.id);

          const productObj = {
            ...p,
            selected: isLinked,
            skus: [],
            loadingSkus: false,
            skusLoaded: false
          };

          this.products.push(productObj);

          // Se já estava vinculado, carregamos os SKUs de imediato para exibição
          if (isLinked) {
            this.originalLinks.add(p.id);
            await this.fetchProductSkus(productObj);
          }
        }
      }
      catch (ex) {
        logger.error("page.supplier-products.loadProducts.error", ex);
        stateHelper.toast(
          "Erro ao carregar produtos",
          "error"
        );
      }
    },
    async fetchProductSkus(productRef) {
      const product = this.products.find(p => p.id === productRef.id);
      if (product.skusLoaded || product.loadingSkus) return;

      product.loadingSkus = true;
      logger.local(`Carregando SKUs para o produto: ${product.id}`);
      try {
        // 1. Busca SKUs da plataforma
        const platformSkusRes = await productService.getSkusByProductId(product.id);
        const platformSkus = platformSkusRes.response || [];

        // 2. Busca SKUs do fornecedor (se estiver vinculado)
        let supplierSkus = [];
        if (this.originalLinks.has(product.id)) {
          try {
            const supplierRes = await SupplierService.getLinkedProductSkus(product.id);
            supplierSkus = supplierRes.response || [];
          } catch (err) {
            console.warn("Produto marcado como vinculado, mas falhou ao buscar detalhes no fornecedor", err);
          }
        }

        // 3. Mapeia e cruza os dados
        product.skus = platformSkus.map(s => {
          const linked = supplierSkus.find(ls => ls.sku === s.sku);
          const data = {
            key: s.sku,
            color: s.color,
            size: s.size,
            platformSku: s.sku,
            skuSupplier: linked ? linked.skuSupplier : "",
            costPrice: linked ? linked.price : ""
          };

          // Salva estado original para o "HasChanges" funcionar
          this.originalSkuData[s.sku] = {
            skuSupplier: data.skuSupplier,
            costPrice: data.costPrice
          };

          return data;
        });

        product.skusLoaded = true;
      } catch (err) {
        logger.error("Erro crítico ao carregar variações:", err);
        stateHelper.toast("Não foi possível carregar as variações deste produto.", "error");
        product.selected = false;
      } finally {
        product.loadingSkus = false;
        logger.local(`Fim do carregamento para: ${product.id}`);
      }
    },
    getGroupedSkus(product) {
      if (!product.skus) return {};
      return product.skus.reduce((acc, sku) => {
        if (!acc[sku.color]) acc[sku.color] = [];
        acc[sku.color].push(sku);
        return acc;
      }, {});
    },
    async toggleProduct(product) {
      if (product.selected) {
        await this.fetchProductSkus(product);
      }
    },
    async saveLinks() {
      this.loading = true;

      const selected = this.products.filter(p => p.selected);
      const supplierSkus = [];

      for (const p of selected) {
        for (const sku of p.skus) {
          if (!sku.costPrice) {
            stateHelper.toast(
              "Preencha todos os preços",
              "error"
            );
            this.loading = false;
            return;
          }
          if (supplierSkus.includes(sku.skuSupplier)) {
            stateHelper.toast(
              `Existem registros com sku repetido (sku fornecedor= '${sku.skuSupplier}')`,
              "error"
            );
            this.loading = false;
            return;
          }
          supplierSkus.push(sku.skuSupplier);

        }
      }

      try {
        const promises = [];

        for (const p of this.products) {
          const isCurrentlySelected = p.selected;
          const wasOriginallyLinked = this.originalLinks.has(p.id);

          // 1. DELETAR: Estava vinculado e foi desmarcado
          if (!isCurrentlySelected && wasOriginallyLinked) {
            promises.push(SupplierService.unlinkProduct(p.id));
          }

          // 2. CRIAR: Não estava vinculado e foi marcado agora
          else if (isCurrentlySelected && !wasOriginallyLinked) {
            const payload = {
              skus: p.skus.map(s => ({
                sku: s.platformSku,
                skuSupplier: s.skuSupplier,
                price: parseFloat(s.costPrice) || 0
              }))
            };
            promises.push(SupplierService.linkProduct(p.id, payload));
          }

          // 3. ATUALIZAR: Já estava vinculado e teve alteração interna nos SKUs
          else if (isCurrentlySelected && wasOriginallyLinked) {
            p.skus.forEach(sku => {
              const original = this.originalSkuData[sku.platformSku];
              if (original && (sku.skuSupplier !== original.skuSupplier || sku.costPrice !== original.costPrice)) {
                promises.push(SupplierService.updateSkuSupplierAndPrice(p.id, sku.platformSku, sku.skuSupplier, parseFloat(sku.costPrice || 0)));
              }
            });
          }
        }

        await Promise.all(promises);
        stateHelper.toast("Alterações salvas com sucesso!", "success");

        // Recarrega para sincronizar o originalSkuData com o banco
        await this.loadLinkedProducts();
        await this.loadProducts(true);

      } catch (ex) {
        logger.error(ex);
        stateHelper.toast("Erro ao salvar algumas alterações.", "error");
      } finally {
        this.loading = false;
      }

      stateHelper.toast("Vínculos salvos com sucesso", "success");
    },
    async cancel() {
      this.loading = true;
      await this.loadProducts(true);
      this.loading = false;
      stateHelper.toast("Alterações descartadas.", "info");
    },
    get filteredProducts() {
      let list = this.products;
      if (this.filter === "isLinked") {
        list = list.filter(p => p.selected);
      }

      if (this.filter === "unlinked") {
        list = list.filter(p => !p.selected);
      }

      if (this.search) {
        const q = this.search.toLowerCase();
        list = list.filter(p =>
          p.name.toLowerCase().includes(q)
        );
      }

      return list;
    },
    applyPriceToAll(product, price) {
      if (!price || price < 0) {
        stateHelper.toast(
          "Informe um valor válido para continuar",
          "error"
        );
        return;
      }
      product.skus.forEach(sku => {
        sku.costPrice = price;
      });

      stateHelper.toast(
        `Preço R$ ${price} aplicado a todos os SKUs de ${product.name}`,
        "info");
    },
    get includedCount() {
      return this.products.filter(p => p.selected && !this.originalLinks.has(p.id)).length;
    },

    get removedCount() {
      return this.products.filter(p => !p.selected && this.originalLinks.has(p.id)).length;
    },

    get includedSkuCount() {
      return this.products
        .filter(p => p.selected && !this.originalLinks.has(p.id))
        .reduce((sum, p) => sum + p.skus.length, 0);
    },

    get removedSkuCount() {
      return this.products
        .filter(p => !p.selected && this.originalLinks.has(p.id))
        .reduce((sum, p) => sum + p.skus.length, 0);
    },
    get updatedSkusCount() {
      let count = 0;
      this.products.forEach(p => {
        // Apenas produtos que já estavam vinculados e permanecem selecionados podem ser "atualizados"
        if (p.selected && this.originalLinks.has(p.id)) {
          p.skus.forEach(sku => {
            const original = this.originalSkuData[sku.platformSku];
            if (original && (sku.skuSupplier !== original.skuSupplier || sku.costPrice !== original.costPrice)) {
              count++;
            }
          });
        }
      });
      return count;
    },

    get hasChanges() {
      // Agora inclui a contagem de SKUs editados
      return this.includedCount > 0 || this.removedCount > 0 || this.updatedSkusCount > 0;
    },

  };
}

export function render() {
  return html;
}
