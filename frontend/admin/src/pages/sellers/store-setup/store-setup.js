import html from "./store-setup.html?raw";
import { ShopeeService } from "../../../services/shopee.service.js";
import stateHelper from "../../../utils/state.helper.js";
import logger from "../../../utils/logger.js";

export function getData() {
  return {
    userEmail: "",
    form: {
      name: "",
      errors: {
        name: null,
      }
    },
    linkMarketplace: null,
    async init() {
      logger.local("page.store-setup.init.called");
      const logged = stateHelper.user;
      this.userEmail = logged.user.email;
      this.linkMarketplace = await ShopeeService.getSellerAuthUrl(logged.user.email);
    },
  };
}

export function render() {
  logger.local("page.store-setup.render.loaded");
  return html;
}
