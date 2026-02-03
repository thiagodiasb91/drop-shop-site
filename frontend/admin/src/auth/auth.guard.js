import { AuthService } from "../services/auth.service.js"
import { navigate } from "../router.js";

export async function requireAuth() {
  const user = await AuthService.me()

  console.log("auth.requireAuth.user", user)
  if (!user) {
    console.log("auth.requireAuth.not-authenticated");
    navigate("/login");
    // throw new Error("Not authenticated");
  }

  console.log("auth.requireAuth.authenticated", user);

  return user
}
