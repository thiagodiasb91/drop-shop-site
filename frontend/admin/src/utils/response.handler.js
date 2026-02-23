import AuthService from "../services/auth.service";

function handleUnauthorized(res) {
  if (res.status === 401) {
    console.warn("Sessão expirada ou inválida. Limpando dados...");

    AuthService.logout();

    if (window.Alpine) {
      const authStore = Alpine.store('auth');
      if (authStore) authStore.user = null;
    }

    window.location.href = "/login";
    return false
  }
  return true
}
export async function responseHandler(res) {
  let response = null

  if (!handleUnauthorized(res)) {
    return { ok: false, status: 401 };
  }

  try {
    response = await res.json()
    response = response.items ? response.items : response
  } catch (e) {
    console.error("Erro ao processar resposta JSON:", e);
    response = null;
  }

  if (!res.ok && ![404].includes(res.status)) {
    // Se houver uma mensagem de erro na API, podemos disparar um Toast aqui
    if (window.Alpine && response?.error) {
      Alpine.store('toast').open(response.error, 'error');
    }
  }


  console.log("responseHandler", {
    ok: res.ok,
    status: res.status,
    response
  })

  return {
    ok: res.ok,
    status: res.status,
    response: res.ok ? response : null,
    error: res.ok ? null : response
  }
}