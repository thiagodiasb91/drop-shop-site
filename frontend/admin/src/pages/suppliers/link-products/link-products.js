import html from "./link-products.html?raw"
import { router, back, navigate } from "../../../core/router.js"
import SupplierService from "../../../services/suppliers.services.js"
import productService from "../../../services/products.services.js"
import AuthService from "../../../services/auth.service.js"
import ProductsService from "../../../services/products.services.js"

export function getData() {
  return {
    supplierId: null,
    supplierEmail: null,
    products: [],
    linkedProducts: [],
    search: '',
    filter: 'all',
    nextCursor: null,
    loading: true,
    originalSkuData: {},

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

      const params = new URLSearchParams(location.search);
      console.log(`${log_prefix}.params`, params)

      const loggedInfo = await AuthService.me()
      console.log(`${log_prefix}.loggedInfo`, loggedInfo)


      if (loggedInfo.role === 'supplier') {
        this.supplierId = loggedInfo.resourceId
        this.supplierEmail = loggedInfo.user.email
      }
      else if (loggedInfo.role == 'admin') {
        const supplierId = params.get("supplierId")
        if (!supplierId) {
          console.log(`${log_prefix}.loggedInfo.admin.supplierIdNotFound`)
          Alpine.store('toast').open(
            'Supplier ID não informado',
            'error'
          )
          return
        }
        this.supplierId = supplierId
        await this.fetchSupplier()
      }
      else {
        console.log(`${log_prefix}.loggedInfo.roleNotAllowed`, loggedInfo.role)
        Alpine.store('toast').open(
          'Você não tem permissão para acessar essa página',
          'error'
        )
        navigate('/')
      }

      console.log(`${log_prefix}.supplierId`, this.supplierId)
      return
    },
    async fetchSupplier() {
      const supplier = await SupplierService.get(this.supplierId)
      this.supplierEmail = supplier?.email || ''
    },
    async loadLinkedProducts() {
      console.log('page.supplier-products.loadLinkedProducts.called', this.supplierId)
      const linkedProducts = await SupplierService.getLinkedProducts(this.supplierId)
      console.log('page.supplier-products.loadLinkedProducts.response', linkedProducts.response)
      this.linkedProducts = linkedProducts.ok ? linkedProducts.response : []
    },
    async loadProducts(reset = false) {
      if (reset) {
        this.products = []
        this.originalLinks = new Set();
        this.originalSkuData = {};
      }

      try {
        const products = await productService.getAll()
        console.log('page.supplier-products.loadProducts.products', products)

        for (const p of products.response) {
          console.log('page.supplier-products.loadProducts.linkedProducts', this.linkedProducts)

          const isLinked = this.linkedProducts?.some(lp => lp.productId === p.id)
          
          const productObj = {
            ...p,
            selected: isLinked,
            skus: [],
            loadingSkus: false,
            skusLoaded: false
          }
          
          this.products.push(productObj);

          // Se já estava vinculado, carregamos os SKUs de imediato para exibição
          if (isLinked) {
            this.originalLinks.add(p.id);
            await this.fetchProductSkus(productObj);
          }
        }
      }
      catch (ex) {
        console.error('page.supplier-products.loadProducts.error', ex)
        Alpine.store('toast').open(
          'Erro ao carregar produtos',
          'error'
        )
      }
    },
    async fetchProductSkus(productRef) {
      const product = this.products.find(p => p.id === productRef.id);
      if (product.skusLoaded || product.loadingSkus) return;
      
      product.loadingSkus = true;
      console.log(`Carregando SKUs para o produto: ${product.id}`);
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
            skuSupplier: linked ? linked.skuSupplier : '', 
            costPrice: linked ? linked.price : ''
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
        console.error('Erro crítico ao carregar variações:', err);
        Alpine.store('toast').open('Não foi possível carregar as variações deste produto.', 'error');
        product.selected = false; 
      } finally {
        product.loadingSkus = false;
        console.log(`Fim do carregamento para: ${product.id}`);
      }
    },
    async toggleProduct(product) {
      if (product.selected) {
        await this.fetchProductSkus(product);
      }
    },
    async saveLinks() {
      this.loading = true;

      const selected = this.products.filter(p => p.selected)
      const supplierSkus = []

      for (const p of selected) {
        for (const sku of p.skus) {
          if (!sku.skuSupplier || !sku.costPrice) {
            Alpine.store('toast').open(
              'Preencha todos os SKUs e preços',
              'error'
            )
            this.loading = false;
            return
          }
          if (supplierSkus.includes(sku.skuSupplier)){
            Alpine.store('toast').open(
              `Existem registros com sku repetido (sku fornecedor= '${sku.skuSupplier}')`,
              'error'
            )
            this.loading = false;
            return
          }
          supplierSkus.push(sku.skuSupplier)
      
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
        Alpine.store('toast').open('Alterações salvas com sucesso!', 'success');

        // Recarrega para sincronizar o originalSkuData com o banco
        await this.loadLinkedProducts();
        await this.loadProducts(true);

      } catch (ex) {
        console.error(ex);
        Alpine.store('toast').open('Erro ao salvar algumas alterações.', 'error');
      } finally {
        this.loading = false;
      }

      Alpine.store('toast').open('Vínculos salvos com sucesso', 'success')
    },
    cancel() {
      // Simplesmente recarrega os produtos do estado original
      this.loadProducts(true);
      Alpine.store('toast').open('Alterações descartadas.', 'info');
    },
    get filteredProducts() {
      let list = this.products
      if (this.filter === 'isLinked') {
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
    applyPriceToAll(product, price) {
      if (!price || price < 0) {
        Alpine.store('toast').open(
          'Informe um valor válido para continuar',
          'error'
        )
        return
      }
      product.skus.forEach(sku => {
        sku.costPrice = price;
      });

      Alpine.store('toast').open(
        `Preço R$ ${price} aplicado a todos os SKUs de ${product.name}`, 
        'info');
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

  }
}

export function render() {
  return html
}
