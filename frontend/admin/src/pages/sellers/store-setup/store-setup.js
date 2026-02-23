import html from "./store-setup.html?raw"
import { navigate } from "../../../core/router.js";
import AuthService from "../../../services/auth.service.js"
import { ShopeeService } from "../../../services/shopee.services.js"
import stateHelper from "../../../utils/state.helper.js";

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
      console.log("page.store-setup.init.called");
      const logged = stateHelper.user;
      this.userEmail = logged.user.email;
      this.linkMarketplace = await ShopeeService.getSellerAuthUrl(logged.user.email);
    },
  }
}

export function render() {
  console.log("page.store-setup.render.loaded");
  return html;
}
