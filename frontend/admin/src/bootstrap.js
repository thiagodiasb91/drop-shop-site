import { AuthService } from "./services/auth.service";

export default class Bootstrap {
  constructor(Alpine) {
    this.Alpine = Alpine;
  }

  init() {
    this.Alpine.data("bootstrap", () => ({
      logged: null,
      async init() {
        console.log('bootstrap.init.called');
        this.logged = await AuthService.me()
        console.log('bootstrap.init.logged', this.logged);
      },
      async logout() {
        console.log('bootstrap.logout.called');
        const confirm = window.confirm('Tem certeza que deseja sair?')
        if (!confirm) return
        await AuthService.logout()
        window.location.reload()
      }
    }));
  }
}

document.addEventListener('alpine:init', () => {
  if (!Alpine.store('toast')) {
    Alpine.store('toast', {
      show: false,
      message: '',
      type: 'info',
      _timer: null,

      open(message, type = 'info') {
        this.message = message;
        this.type = type;
        this.show = true;

        clearTimeout(this._timer);
        this._timer = setTimeout(() => {
          this.show = false;
        }, 2500);
      }
    });
  }
});