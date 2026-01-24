import { AuthService } from "../../services/auth.service.js"

export function getData() {
  return {
    user: null,

    async init() {
      const me = await AuthService.me()

      if (!me) {
        window.location.href = "/pages/auth/login.html"
        return
      }

      this.user = me.user
    },

    async logout() {
      await AuthService.logout()
      window.location.reload()
      // window.location.href = "/pages/auth/login.html"
    }
  }
}
