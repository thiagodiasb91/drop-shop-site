import { AuthService } from "./services/auth.service";

export default class Bootstrap {
  constructor(Alpine) {
    this.Alpine = Alpine;
  }

  init() {
    this.Alpine.data("layout", () => ({
      async logout() {
        await AuthService.logout()
        window.location.reload()
      }
    }));

  }
}