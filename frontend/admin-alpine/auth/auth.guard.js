import { AuthService } from "../services/auth.service.js"

export async function requireAuth() {
  const user = await AuthService.me()

  if (!user) {
    window.location.href = "/pages/auth/login.html"
    return
  }

  return user
}
