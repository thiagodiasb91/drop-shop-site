import html from "./users.html?raw"
import { navigate } from "../../../core/router.js"
import UsersService from "../../../services/users.services.js"

export function getData() {
  return {
    loading: true,
    search: '',
    users: [],
    availableRoles: [
      'admin',
      'seller', 
      'supplier', 
      'new-user',
    ],

    init() {
      this.fetchUsers();
    },

    async fetchUsers() {
      this.loading = true
      const res = await UsersService.getAllUsers()
      console.log("pages.users.fetchUsers.getAllUsers", res)

      if (!res.ok) {
        console.error("pages.users.fetchUsers.getAllUsers.error", res.response)
        Alpine.store('toast').open('Erro ao consultar usuários.', 'error');
        return
      }

      this.users = res.response.map(u => ({ ...u, saving: false }));
      this.loading = false
    },

    get filteredUsers() {
      if (!this.search) return this.users;
      const s = this.search.toLowerCase();
      return this.users.filter(u =>
        u.email.toLowerCase().includes(s) ||
        u.cognitoId.toLowerCase().includes(s)
      );
    },

    labelRole(role) {
      return {
        supplier: 'Fornecedor',
        seller: 'Vendedor',
        admin: 'Admin',
        'new-user': 'Sem acesso'
      }[role];
    },
    async setRole(user, role) {
      if (user.role === role || user.saving) return;

      const oldRole = user.role;
      user.role = role;
      user.saving = true;

      const res = await UsersService.save(user);

      if (!res.ok) {
        user.role = oldRole; // Reverte em caso de erro
        Alpine.store('toast').open('Erro ao atualizar permissão.', 'error');
      } else {
        Alpine.store('toast').open(`Usuário atualizado para ${this.labelRole(role)}`, 'success');
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
