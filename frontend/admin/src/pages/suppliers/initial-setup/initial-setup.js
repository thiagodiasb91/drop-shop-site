import html from "./initial-setup.html?raw"
import AuthService from "../../../services/auth.service.js"
import SuppliersService from "../../../services/suppliers.services.js"
import CacheHelper from "../../../utils/cache.helper.js"
import { navigate } from "../../../core/router.js"
import stateHelper from "../../../utils/state.helper.js";

function setStep (_this, step) {
  _this.step = {
      submit: false,
      redirect: false,
      loading: false,
    }
  _this.step[step] = true
}

export function getData() {
  return {
    userEmail: "",
    form: {
      name: "123",
      legalName: "123",
      cnpj: "33.113.309/0001-47",
      enotasId: "5d366413-e89b-4e8a-9f20-a47193f81ffd",
      phone: "11-988441122",
      addressState: "SP",
    },
    errors: {
      name: null,
      legalName: null,
      cnpj: null,
      enotasId: null,
      phone: null,
      addressState: null,
    },
    step: {
      submit: true,
      redirect: false,
      loading: false,
    },
    async init() {
      console.log("page.suppliers.initial-setup.init.called")
      const logged = stateHelper.user;
      this.userEmail = logged.user.email
      if(this.validate()){
        this.goToRedirect()
      }
    },
    async submit() {
      if (this.step.loading) return;

      this.errors = { name: null };

      if (!this.validate())
        return;
      
      setStep(this, 'loading')

      const res = await SuppliersService.save(this.form)

      if (!res.ok) {
        console.error("pages.suppliers.initial-setup.save.error", res.response)
        stateHelper.toast(
          "Houve um erro ao tentar configurar o fornecedor. Tente novamente.",
          "error")
        setStep(this, 'submit')
        return
      }
      console.log("page.suppliers.initial-setup.save.response", res)
      
      const renewResponse = await AuthService.renewToken()
      console.log("pages.suppliers.initial-setup.renewToken.response", renewResponse)

      if (!renewResponse.ok) {
        console.error("pages.suppliers.initial-setup.renewToken.error", renewResponse.response)
        this.message = "Houve um erro ao tentar renovar o seu token. Tente novamente."
        this.loading = false
        return
      }

      CacheHelper.set("session_token", renewResponse.response.sessionToken)

      console.log("pages.suppliers.initial-setup.sessionToken.set")

      await AuthService.me(true)
      
    },
    goToRedirect(){
      setStep(this, 'redirect')
      setTimeout(() => navigate("/"), 5000);
    },
    validate() {
      let valid = true;

      if (!this.form.name?.trim()) {
        this.errors.name = "O nome da loja é obrigatório.";
        valid = false;
      }
      if (!this.form.legalName?.trim()) {
        this.errors.legalName = "O nome legal é obrigatório.";
        valid = false;
      }
      if (!this.form.cnpj?.trim()) {
        this.errors.cnpj = "O CNPJ é obrigatório.";
        valid = false;
      }
      if (!this.form.enotasId?.trim()) {
        this.errors.enotasId = "O ID do e-notas é obrigatório.";
        valid = false;
      }
      if (!this.form.phone?.trim()) {
        this.errors.phone = "O telefone é obrigatório.";
        valid = false;
      }
      if (!this.form.addressState?.trim()) {
        this.errors.addressState = "O estado é obrigatório.";
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
