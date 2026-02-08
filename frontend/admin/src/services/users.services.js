import { ENV } from "../config/env.js"
import { responseHandler } from "../utils/response.handler.js"

console.log("ENV.API_BASE_URL", ENV.API_BASE_URL);

const UsersService = {
  basePath: `${ENV.API_BASE_URL}/users`,
  async getAllUsers() {
    console.log("UsersService.getAllUsers.request")
    const res = await fetch(
      `${this.basePath}`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    )

    return responseHandler(res)
  },
  async save(user) {
    console.log("UsersService.save.request", user)
    const res = await fetch(
      `${this.basePath}/${user.id}`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(user),
      }
    )

    return responseHandler(res)
  },
}

export default UsersService