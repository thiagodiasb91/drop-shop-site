import html from "./initial-setup.html?raw";
import AuthService from "../../../services/auth.service.js";
import SuppliersService from "../../../services/suppliers.service.js";
import { navigate } from "../../../core/router.js";
import stateHelper from "../../../utils/state.helper.js";
import { maskCNPJ, maskPhone } from "../../../utils/format.helper.js";
import logger from "../../../utils/logger.js";

function setStep(_this, step) {
  _this.step = {
    submit: false,
    redirect: false,
    loading: false,
  };
  _this.step[step] = true;
}

export function getData() {
  return {
    userEmail: "",
    form: {
      name: null,
      legalName: null,
      cnpj: null,
      enotasId: null,
      infinitePayId: null,
      phone: null,
      addressState: "SP",
    },
    errors: {
      name: null,
      legalName: null,
      cnpj: null,
      enotasId: null,
      infinitePayId: null,
      phone: null,
      addressState: null,
    },
    step: {
      submit: true,
      redirect: false,
      loading: false,
    },
    async init() {
      logger.local("page.suppliers.initial-setup.init.called");
      const logged = stateHelper.user;
      this.userEmail = logged.user.email;

      if (logged?.resourceId) {
        logger.local("page.suppliers.initial-setup.init.alreadySetup", logged.user.resourceId);
        this.goToRedirect();
      }

      this.$watch("form.cnpj", value => {
        if (!value) return;
        this.form.cnpj = maskCNPJ(value);
      });

      this.$watch("form.phone", value => {
        if (!value) return;
        this.form.phone = maskPhone(value);
      });
    },
    async submit() {
      if (this.step.loading) return;

      this.errors = { name: null };

      if (!this.validate())
        return;

      setStep(this, "loading");

      const payload = {
        ...this.form,
        cnpj: this.form.cnpj.replace(/[^a-zA-Z0-9]/g, "").toUpperCase(),
        phone: this.form.phone.replace(/\D/g, ""),
      };

      const res = await SuppliersService.save(payload);

      if (!res.ok) {
        logger.error("pages.suppliers.initial-setup.save.error", res.response);
        stateHelper.toast(
          "Houve um erro ao tentar configurar o fornecedor. Tente novamente.",
          "error");
        setStep(this, "submit");
        return;
      }
      logger.local("page.suppliers.initial-setup.save.response", res);

      const renewResponse = await AuthService.renewToken();
      logger.local("pages.suppliers.initial-setup.renewToken.response", renewResponse);

      if (!renewResponse.ok) {
        logger.error("pages.suppliers.initial-setup.renewToken.error", renewResponse.response);
        this.message = "Houve um erro ao tentar renovar o seu token. Tente novamente.";
        this.loading = false;
        return;
      }

      stateHelper.setSession(renewResponse.response.sessionToken);

      logger.local("pages.suppliers.initial-setup.sessionToken.set");

      await AuthService.me(true);

    },
    goToRedirect() {
      setStep(this, "redirect");
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
      const cleanCNPJ = this.form.cnpj?.replace(/[^a-zA-Z0-9]/g, "") || "";
      if (cleanCNPJ.length !== 14) {
        this.errors.cnpj = "O CNPJ deve ter exatamente 14 caracteres.";
        valid = false;
      } else {
        // Regra da Receita: Os últimos 6 dígitos (posição 9 a 14) DEVEM ser números
        // O radical (8 primeiros) pode ser alfanumérico
        const filialEVerificadores = cleanCNPJ.slice(8);
        if (!/^\d+$/.test(filialEVerificadores)) {
          this.errors.cnpj = "Os últimos 6 dígitos do CNPJ devem ser apenas números.";
          valid = false;
        }
      }
      if (!this.form.enotasId?.trim()) {
        this.errors.enotasId = "O ID do e-notas é obrigatório.";
        valid = false;
      }
      if (!this.form.infinitePayId?.trim()) {
        this.errors.infinitePayId = "O ID do InfinitePay é obrigatório.";
        valid = false;
      }
      const cleanPhone = this.form.phone?.replace(/\D/g, "");
      if (!cleanPhone || cleanPhone.length < 10 || cleanPhone.length > 11) {
        this.errors.phone = "Telefone inválido. Use (11) 99999-9999.";
        valid = false;
      }
      if (!this.form.addressState?.trim()) {
        this.errors.addressState = "O estado é obrigatório.";
        valid = false;
      }
      return valid;
    }
  };
}

export function render() {
  logger.local("page.store-setup.render.loaded");
  return html;
}
