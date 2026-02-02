import { AuthService } from "../services/auth.service.js"
import { navigate } from "../router.js";

export async function requireAuth() {
  const user = await AuthService.me()

  if (!user) {
    console.log("router.requireAuth.notAuthenticated");
    navigate("/login");
    throw new Error("Not authenticated");
  }

  console.log("router.requireAuth.authenticated", user);

  return user
}
