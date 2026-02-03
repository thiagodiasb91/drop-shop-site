import html from "./login.html?raw";
import { AuthService } from "../../../services/auth.service.js";

console.log("page.login.module.loaded");

export function getData() {
  console.log("page.login.getData.loaded");
  return {
    loading: false,

    login() {
      console.log("page.login.login.request");
      this.loading = true;
      AuthService.login();
    }
  }
}

export function render() {
  console.log("page.login.render.loaded");

  return html;
}