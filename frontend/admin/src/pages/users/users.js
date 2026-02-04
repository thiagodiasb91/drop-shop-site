import html from "./users.html?raw"
import {navigate} from "../../core/router.js"

export function getData() {
  return {
    search: '',
    users: [],

    init() {
      this.fetchUsers();
    },

    async fetchUsers() {
      this.users = [
        {
          id: 1,
          name: 'João Silva',
          cognitoId: '74e82498-2061-70e3-f44e-30f978854444',
          email: 'joao.silva@example.com',
          emailVerified: true,
          role: 'supplier',
          saving: false
        },
        {
          id: 2,
          name: 'Maria Souza',
          cognitoId: '75e82498-2061-70e3-f44e-30f978854444',
          email: 'maria.souza@example.com',
          emailVerified: false,
          role: 'seller',
          saving: false
        },
        {
          id: 3,
          name: 'Carlos Pereira',
          cognitoId: '99s82498-2061-70e3-f44e-30f978854444',
          email: 'carlos.pereira@example.com',
          emailVerified: true,
          role: 'admin',
          saving: false
        },
        {
          id: 4,
          name: 'Ana Lima',
          cognitoId: '87s25846-2061-70e3-f44e-30f978854444',
          email: 'ana.lima@example.com',
          emailVerified: false,
          role: 'supplier',
          saving: false
        },]
    },

    get filteredUsers() {
      if (!this.search) return this.users;
      const s = this.search.toLowerCase();
      return this.users.filter(u =>
        u.name.toLowerCase().includes(s) ||
        u.cognitoId.toLowerCase().includes(s)
      );
    },

    labelRole(role) {
      return {
        supplier: 'Fornecedor',
        seller: 'Vendedor',
        admin: 'Admin',
        null: 'Sem acesso'
      }[role];
    },

    setRole(user, role) {
      if (user.role === role) return;
      user.role = role;
      this.save(user);
    },

    save(user) {
      user.saving = true;

      console.log("users.save", user)
      setTimeout(() => {
        user.saving = false;
      }, 600);
    },
    copy(value, event) {
      navigator.clipboard.writeText(value);

      const btn = event?.currentTarget;
      if (!btn) return;

      btn.classList.add('copied');
      setTimeout(() => btn.classList.remove('copied'), 600);
    },
    openProductLink(user) {
      if (user.role !== 'supplier') return
      navigate(`/suppliers/${user.id}/products`)
    }
  }
}

export function render() {
  console.log("page.settings.render.loaded");
  return html;
}
