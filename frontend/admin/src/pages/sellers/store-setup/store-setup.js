import html from "./store-setup.html?raw"
import { navigate } from "../../../core/router.js";
import { AuthService } from "../../../services/auth.service.js"
import { ShopeeService } from "../../../services/shopee.services.js"

export function getData() {
  return {
    loading: false,
    userEmail: "",
    form: {
      name: "",
      errors: {
        name: null,
      }
    },
    step: {
      submit: true,
      link: false
    },
    linkMarketplace: "NADA",
    async init() {
      const logged = await AuthService.me()
      this.userEmail = logged.user.email
    },
    async submit() {
      if (this.loading) return;

      this.errors = { name: null };

      if (!this.validate())
        return;

      this.loading = true;

      await new Promise(resolve => setTimeout(resolve, 1000));
      this.loading = false;

      this.linkMarketplace = await ShopeeService.getSellerAuthUrl(this.userEmail)

      this.step = {
        submit: false,
        link: true
      }

      // Alpine.store("toast").open("Loja configurada com sucesso!", "success");
      // navigate("/dashboard");
    },
    validate() {
      let valid = true;

      if (!this.form.name.trim()) {
        this.form.errors.name = "O nome da loja é obrigatório.";
        valid = false;
      }
      return valid;
    }
  }
}

export function render() {
  console.log("page.store-setup.render.loaded");
  return html;
}
