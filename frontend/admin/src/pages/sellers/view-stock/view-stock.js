import html from "./view-stock.html?raw";

export function getData() {
  return {
    filters: {
      search: "",
      sync: "",
      stock: "",
      maxQty: ""
    },

    products: generateMockProducts(),

    get filteredProducts() {
      const search = this.filters.search.toLowerCase();

      return this.products
        .map(product => {
          const skus = product.skus.filter(sku => {
            const matchesSearch =
              !search ||
              product.name.toLowerCase().includes(search) ||
              product.description.toLowerCase().includes(search) ||
              product.productId.toLowerCase().includes(search) ||
              sku.skuId.toLowerCase().includes(search) ||
              sku.label.toLowerCase().includes(search);

            const matchesSync =
              !this.filters.sync ||
              (this.filters.sync === "synced" && sku.synced) ||
              (this.filters.sync === "pending" && !sku.synced);

            const matchesStock =
              !this.filters.stock ||
              (this.filters.stock === "low" && sku.stock <= sku.lowStockLimit) ||
              (this.filters.stock === "ok" && sku.stock > sku.lowStockLimit);

            const matchesQty =
              !this.filters.maxQty ||
              sku.stock <= Number(this.filters.maxQty);

            return matchesSearch && matchesSync && matchesStock && matchesQty;
          });

          return skus.length ? { ...product, skus } : null;
        })
        .filter(Boolean);
    },
    get totalProducts() {
      return this.products.length;
    },

    get totalSkus() {
      return this.products.reduce((sum, p) => sum + p.skus.length, 0);
    },
    get lowStockCount() {
      return this.filteredProducts.reduce((sum, p) => {
        return sum + p.skus.filter(s => s.stock <= s.lowStockLimit).length;
      }, 0);
    },
    getProductTotalStock(product) {
      return product.skus.reduce((sum, sku) => sum + sku.stock, 0);
    },
    isProductLowStock(product) {
      return product.skus.some(sku => sku.stock <= sku.lowStockLimit);
    }
  };
}

function generateMockProducts() {
  const colors = ["Azul", "Preto", "Branco", "Verde"];
  const sizes = ["P", "M", "G", "GG"];

  return Array.from({ length: 15 }).map((_, i) => {
    const productId = `PROD-${1000 + i}`;

    return {
      productId,
      name: `Tênis Esportivo ${i + 1}`,
      description: "Produto confortável, ideal para uso diário e esportivo.",
      skus: colors.flatMap(color =>
        sizes.map(size => ({
          skuId: `SKU-${color[0]}${size}-${i + 1}`,
          label: `Cor: ${color} · Tamanho: ${size}`,
          stock: Math.floor(Math.random() * 40),
          lowStockLimit: 8,
          synced: Math.random() > 0.3
        }))
      )
    };
  });
}

export function render() {
  return html;
}
