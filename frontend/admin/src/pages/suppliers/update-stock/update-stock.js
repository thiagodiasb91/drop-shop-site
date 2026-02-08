import html from "./update-stock.html?raw"

export function getData() {
  return {
    loading: true,
    search: '',
    supplierEmail: 'fornecedor@exemplo.com',

    products: [],
    hasChanges: false,
    changedCount: 0,

    init() {
      // mock grande
      this.products = Array.from({ length: 20 }).map((_, pIndex) => ({
        id: `prod-${pIndex}`,
        name: `Produto ${pIndex + 1}`,
        open: false,
        skus: Array.from({ length: 6 }).map((_, sIndex) => ({
          id: `sku-${pIndex}-${sIndex}`,
          supplierSku: `SUP-${pIndex}-${sIndex}`,
          attributes: {
            cor: ['Azul', 'Preto', 'Branco'][sIndex % 3],
            tamanho: [38, 39, 40, 41, 42, 43][sIndex]
          },
          stock: Math.floor(Math.random() * 50),
          originalStock: null,
          changed: false
        }))
      }));

      // guarda estoque original
      this.products.forEach(p =>
        p.skus.forEach(sku => {
          sku.originalStock = sku.stock;
        })
      );

      this.loading = false;
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

    saveAll() {
      const payload = this.products.flatMap(p =>
        p.skus
          .filter(s => s.changed)
          .map(s => ({
            skuId: s.id,
            stock: s.stock
          }))
      );

      console.log('SALVANDO ESTOQUE', payload);

      // simula sucesso
      this.products.forEach(p =>
        p.skus.forEach(s => {
          s.originalStock = s.stock;
          s.changed = false;
        })
      );

      this.updateChanges();
      Alpine.store('toast').open(`${payload.length} SKU(s) salvo(s) com sucesso`, 'success');
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
