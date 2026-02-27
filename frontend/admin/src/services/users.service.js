import logger from "../utils/logger.js";
import BaseApi from "./base.api.js";

const api = new BaseApi("/users");

const UsersService = {
  async getAllUsers() {
    logger.local("UsersService.getAllUsers.request");
    return api.call(
      "/",
      {
        method: "GET",
      }
    );
  },
  async save(user) {
    logger.local("UsersService.save.request", user);
    return api.call(
      `/${user.id}`,
      {
        method: "PUT",
        body: JSON.stringify(user),
      }
    );
  },
};

export default UsersService;