import html from "./index.html?raw"
import { currency } from "../../utils/format.utils";

export function getData() {
  return {
    loading: false,
    search: '',
    products: [{
      id: 'PROD-01',
      sku: 'SKU-001',
      name: 'Produto 1',
      active: true
    },{
      id: 'PROD-02',
      sku: 'SKU-002',
      name: 'Produto 2',
      active: true
    },{
      id: 'PROD-03',
      sku: 'SKU-003',
      name: 'Produto 3',
      active: false
    }],

    modal: {
      open: false,
      editing: false,
      loading: false,
      form: {}
    },

    openCreate() {
      this.modal.open = true;
      this.modal.editing = false;
      this.modal.form = {
        sku: '',
        name: '',
        active: true
      };
    },

    openEdit(product) {
      this.modal.open = true;
      this.modal.editing = true;
      this.modal.form = { ...product };
    },

    closeModal() {
      if (this.modal.loading) return;
      this.modal.open = false;
    },

    saveProduct() {
      this.modal.loading = true;

      setTimeout(() => {
        if (this.modal.editing) {
          const index = this.products.findIndex(
            p => p.id === this.modal.form.id
          );
          this.products[index] = { ...this.modal.form };
        } else {
          this.products.push({
            ...this.modal.form,
            id: Date.now()
          });
        }

        this.modal.loading = false;
        this.modal.open = false;
      }, 800);
    },

    remove(product) {
      if (confirm(`Excluir "${product.name}"?`)) {
        this.products = this.products.filter(p => p.id !== product.id);
      }
    },

    currency(value) {
      return currency(value)
    }
  }
}

export function render() {
  console.log("page.products.render.loaded");
  return html;
}
