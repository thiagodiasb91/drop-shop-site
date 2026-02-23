// core/state.js
const stateHelper = {
  // Acesso ao usuário
  get user() {
    return window.Alpine?.store('auth')?.user || null;
  },

  // Acesso ao estado de carregamento
  get authLoading() {
    return window.Alpine?.store('auth')?.loading ?? true;
  },

  // Atalho para saber se está logado
  get isAuthenticated() {
    return !!this.user;
  },

  refresh() {
    return window.Alpine?.store('auth')?.refresh();
  },

  toast(message, type = 'info') {
    window.Alpine?.store('toast')?.open(message, type);
  }
};

export default stateHelper;