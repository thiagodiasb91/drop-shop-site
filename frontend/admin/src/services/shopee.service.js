import logger from "../utils/logger.js";
import BaseApi from "./base.api.js";

const api = new BaseApi("/shopee");

export const ShopeeService = {
  async getSellerAuthUrl(email) {
    logger.local("ShopeeService.getSellerAuthUrl.request", email);
    const query = new URLSearchParams({ email });

    const res = await api.call(
      `${this.basePath}/auth-url?${query}`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    );

    if (!res.ok) {
      logger.error("ShopeeService.getSellerAuthUrl.error", res.response);
      throw new Error("ShopeeService.getSellerAuthUrl.error");
    }
    const response = res.response;
    logger.local("ShopeeService.getSellerAuthUrl.response", response);
    return response.authUrl;
  }
};
