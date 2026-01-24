import { requireAuth } from "../../../auth/auth.guard.js"

const user = await requireAuth()

console.log("Usuário autenticado:", user)

// aqui você chama /orders/list depois
