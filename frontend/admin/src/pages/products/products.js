import html from "./products.html?raw"
import { currency } from "../../utils/format.utils";

export function getData() {
  return {
    loading: false,
    search: '',
    products: [{
      id: 'PROD-01',
      name: 'Camiseta Básica',
      variations: [
        {
          name: 'Cor',
          options: ['Azul', 'Vermelho']
        },
        {
          name: 'Tamanho',
          options: ['P', 'M', 'G', 'GG']
        }
      ],
      active: true
    }, {
      id: 'PROD-02',
      name: 'Calça Jeans',
      variations: [{
        name: 'Cor',
        options: ['Azul', 'Preto'],
      }, {
        name: 'Tamanho',
        options: ['P', 'M', 'G', 'GG'],
      }],
      active: true
    }, {
      id: 'PROD-03',
      name: 'Tênis Esportivo',
      variations: [{
        name: 'Cor',
        options: ['Branco', 'Preto'],
      }, {
        name: 'Tamanho',
        options: ['40', '42', '44', '46'],
      }],
      active: true
    }, {
      id: 'PROD-04',
      name: 'Jaqueta de Couro',
      variations: [{
        name: 'Cor',
        options: ['Marrom', 'Cinza'],
      }, {
        name: 'Tamanho',
        options: ['P', 'M', 'G', 'GG'],
      }],
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
        name: '',
        active: true,
        variations: []
      }
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
      if (!this.validateProduct()) return;

      this.modal.loading = true;

      try {
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
          Alpine.store('toast').open('Produto salvo com sucesso.', 'success');
          this.modal.open = false;

          this.modal.loading = false;
          this.modal.open = false;
        }, 800);
      } catch (e) {
        Alpine.store('toast').open('Erro ao salvar produto.', 'error');
        console.log(e);
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
    addVariation() {
      this.modal.form.variations.push({
        name: '',
        options: [],
        _newOption: ''
      });
    },

    removeVariation(index) {
      this.modal.form.variations.splice(index, 1);
    },

    addOption(vIndex) {
      const v = this.modal.form.variations[vIndex];
      const value = v._newOption?.trim();
      if (!value) return;

      v.options.push(value);
      v._newOption = '';
    },

    removeOption(vIndex, oIndex) {
      this.modal.form.variations[vIndex].options.splice(oIndex, 1);
    },

    validateProduct() {
      const variations = this.modal.form.variations;

      if (!variations.length) {
        Alpine.store('toast').open('O produto precisa ter ao menos uma variação.', 'error');
        return false;
      }

      for (const v of variations) {
        if (!v.name?.trim()) {
          Alpine.store('toast').open('A variação precisa ter um nome.', 'error');
          return false;
        }

        if (!v.options || v.options.length === 0) {
          Alpine.store('toast').open('A variação precisa ter ao menos uma opção.', 'error');
          return false;
        }
      }

      return true;
    },

  }
}

export function render() {
  console.log("page.products.render.loaded");
  return html;
}
