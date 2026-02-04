import { menu } from "../core/bootstrap.menu.js"

export function layoutAuthenticated() {
  return {
    
    logged: null,

    init() {
      this.logged = Alpine.store('session')
    },

    ...menu()
  }
}
