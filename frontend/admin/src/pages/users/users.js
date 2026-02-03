import html from "./users.html?raw"
import {navigate} from "../../router.js"
import { UserService } from "../../services/users.services.js"
import { AuthService } from "../../services/auth.service.js"

export function getData() {
  return {
    search: '',
    users: [],
    loading: false,
    error: null,

    async init() {
      const me = await AuthService.me()
      if (!me) {
        window.location.href = "/pages/auth/login.html"
        return
      }
      
      await this.fetchUsers()
    },

    async fetchUsers() {
      this.loading = true
      this.error = null
      
      try {
        const data = await UserService.getUsers()
        this.users = data || []
      } catch (error) {
        console.error("Erro ao buscar usuários:", error)
        this.error = error.message
        this.users = []
      } finally {
        this.loading = false
      }
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

    async save(user) {
      user.saving = true
      
      try {
        await UserService.updateUser(user.id, { role: user.role })
        console.log("Usuário atualizado:", user)
      } catch (error) {
        console.error("Erro ao salvar usuário:", error)
        this.error = error.message
      } finally {
        user.saving = false
      }
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
