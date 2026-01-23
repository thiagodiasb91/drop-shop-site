import { AuthService } from "../services/auth.service.js"
import { navigate } from "../router.js";

export async function requireAuth() {
  const user = await AuthService.me()

  if (!user) {
    navigate("/login");
    throw new Error("Not authenticated");
  }

  return user
}
