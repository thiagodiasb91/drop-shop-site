import { menu } from "./bootstrap.menu";
import { commons } from "./bootstrap.commons";
import AuthService from "../services/auth.service.js";

export default class Bootstrap {
  constructor(Alpine) {
    this.Alpine = Alpine;
  }

  init() {
    try {
      registerFunctions(this.Alpine, 'menu', menu);
      registerFunctions(this.Alpine, 'bootstrap', commons);
    }
    catch (err) {
      console.warn('bootstrap: failed to register Function', err);
    }
  }
}

const registerFunctions = (AlpineInstance, modName, modRef) => {
  const register = (A) => { if (A && A.data) A.data(modName, modRef); };

  if (AlpineInstance && AlpineInstance.data) {
    register(AlpineInstance);
    return;
  }

  if (typeof Alpine !== 'undefined' && Alpine && Alpine.data) {
    register(Alpine);
    return;
  }
}


document.addEventListener('alpine:init', () => {
  Alpine.store('auth', {
    user: null,    
    async refresh() {
      this.user = await AuthService.me(true); 
      return this.user;
    }
  });
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