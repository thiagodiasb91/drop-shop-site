import BaseApi from "./base.api.js";
import logger from "../utils/logger.js";

const api = new BaseApi("/suppliers");

const SupplierOrdersService = {
  async getOrdersToSend() {
    logger.local("SupplierOrdersService.getOrdersToSend.request");
    return api.call(
      "/shipments",
      {
        method: "GET"
      }
    );
  },
};

export default SupplierOrdersService;
