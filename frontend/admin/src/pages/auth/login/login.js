import html from "./login.html?raw";
import AuthService from "../../../services/auth.service.js";
import logger from "../../../utils/logger.js";

logger.local("page.login.module.loaded");

export function getData() {
  logger.local("page.login.getData.loaded");
  return {
    loading: false,

    login() {
      logger.local("page.login.login.request");
      this.loading = true;
      AuthService.login();
    }
  };
}

export function render() {
  logger.local("page.login.render.loaded");

  return html;
}