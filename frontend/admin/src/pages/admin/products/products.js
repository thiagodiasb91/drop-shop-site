import html from "./products.html?raw"
import { currency } from "../../../utils/format.utils";
import ProductsService from "../../../services/products.services"
import stateHelper from "../../../utils/state.helper";
import { renderGlobalLoader } from "../../../components";

export function getData() {
  return {
    loading: false,
    search: '',
    products: null,

    modal: {
      open: false,
      editing: false,
      loading: false,
      form: {}
    },

    async init() {
      this.loading = true;
      this.products = await this.fetchProducts()
      this.loading = false;
    },
    async fetchProducts() {
      const productsResponse = await ProductsService.getAllWithSkus()
      console.log("products", productsResponse)

      if (!productsResponse.ok) {
        stateHelper.toast('Erro ao consultar os produtos e seus skus.', 'error');
        return
      }

      return productsResponse.response
    },
    get filteredProducts() {
      if (!this.search) return this.products;
      const q = this.search.toLowerCase();
      return this.products.filter(p =>
        p.name.toLowerCase().includes(q) ||
        p.skus?.some(s => s.sku.toLowerCase().includes(q))
      );
    },
    openCreate() {
      this.modal.open = true;
      this.modal.editing = false;
      this.modal.form = {
        name: '',
        active: true,
        colors: [],
        sizes: [],
        _newColor: '',
        _newSize: ''
      }
    },

    openEdit(product) {
      console.log("page.products.openEdit", product)
      this.modal.open = true;
      this.modal.editing = true;
      this.modal.form = {
        ...JSON.parse(JSON.stringify(product)),
        colors: product.displayVariations?.colors || [],
        sizes: product.displayVariations?.sizes || [],
        _newColor: '',
        _newSize: ''
      };
    },

    closeModal() {
      if (this.modal.loading) return;
      this.modal.open = false;
    },
    addColor() {
      const val = this.modal.form._newColor?.trim();
      if (val && !this.modal.form.colors.includes(val)) {
        this.modal.form.colors.push(val);
        this.modal.form._newColor = '';
      }
    },
    removeColor(index) {
      this.modal.form.colors.splice(index, 1);
    },
    addSize() {
      const val = this.modal.form._newSize?.trim();
      if (val && !this.modal.form.sizes.includes(val)) {
        this.modal.form.sizes.push(val.toUpperCase());
        this.modal.form._newSize = '';
      }
    },
    removeSize(index) {
      this.modal.form.sizes.splice(index, 1);
    },
    validateProduct() {
      if (!this.modal.form.name?.trim()) {
        stateHelper.toast('O nome do produto é obrigatório.', 'error');
        return false;
      }
      if (!this.modal.form.colors.length || !this.modal.form.sizes.length) {
        stateHelper.toast('Adicione ao menos uma cor e um tamanho.', 'error');
        return false;
      }
      return true;
    },
    async saveProduct() {
      if (!this.validateProduct()) return;

      this.modal.loading = true;

      try {
        const isEditing = this.modal.editing;
        const payload = {
          name: this.modal.form.name,
          active: this.modal.form.active
        }

        const productRes = isEditing
          ? await ProductsService.update(this.modal.form.id, payload)
          : await ProductsService.create(payload);

        if (!productRes.ok) throw new Error("Erro ao salvar produto principal");

        const productId = isEditing ? this.modal.form.id : productRes.response.id;
        const existingSkus = isEditing ? (this.modal.form.skus || []) : [];
        const currentColors = this.modal.form.colors;
        const currentSizes = this.modal.form.sizes;

        if (isEditing) {
          const skusToDelete = existingSkus.filter(s =>
            !currentColors.includes(s.color) || !currentSizes.includes(s.size)
          );

          for (const s of skusToDelete) {
            await ProductsService.deleteSku(productId, s.id);
          }
        }

        const skuPromises = [];
        currentColors.forEach(color => {
          currentSizes.forEach(size => {
            // Comparamos com os SKUs que já vieram na busca principal
            const alreadyExists = existingSkus.find(s =>
              s.color?.toLowerCase() === color.toLowerCase() &&
              s.size?.toLowerCase() === size.toLowerCase()
            );

            if (!alreadyExists) {
              skuPromises.push(
                ProductsService.createSku(productId, {
                  color: color.toLowerCase(),
                  size: size.toLowerCase(),
                  sku: `${productId}.${color.substring(0, 3)}.${size}`.toUpperCase()
                })
              );
            }
          });
        });

        if (skuPromises.length > 0) await Promise.all(skuPromises);

        stateHelper.toast('Produto salvo com sucesso.', 'success');
        await this.init(); // Recarrega a tabela usando o getAllWithSkus do service
        this.closeModal();
      } catch (e) {
        console.error(e);
        stateHelper.toast('Erro ao salvar produto.', 'error');
      } finally {
        this.modal.loading = false;
      }
    },

    remove(product) {
      if (confirm(`Excluir "${product.name}"?`)) {
        this.products = this.products.filter(p => p.id !== product.id);
      }
    },

    currency(value) {
      return currency(value)
    },
    renderLoader() {
      return renderGlobalLoader("Carregando produtos...");
    }
  }
}

export function render() {
  console.log("page.products.render.loaded");
  return html;
}
