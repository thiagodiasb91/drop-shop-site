import html from "./users.html?raw"
import {navigate} from "../../core/router.js"
import UsersService from "../../services/users.services.js"

export function getData() {
  return {
    search: '',
    users: [],

    init() {
      this.fetchUsers();
    },

    async fetchUsers() {
      const users = await UsersService.getAllUsers()
      console.log("pages.users.fetchUsers.getAllUsers", users)

      if (!users.ok){
        console.error("pages.users.fetchUsers.getAllUsers.error", users.data)
        Alpine.store('toast').open('Erro ao consultar usuários.', 'error');
        return
      }

      this.users = users.data
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

    async setRole(user, role) {
      if (user.role === role) return;
      user.role = role;
      await this.save(user);
    },

    async save(user) {
      user.saving = true;

      console.log("users.save", user)
      const res = await UsersService.save(user)

      if (!res.ok){
        console.error("pages.users.save.error", res.data)
        Alpine.store('toast').open('Erro ao salvar o usuário.', 'error');
        return
      }
      user.saving = false;
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
