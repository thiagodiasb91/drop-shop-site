import { ENV } from "../../../config/env.js"

export function getData() {
  return {
    loading: false,

    login() {
      console.log("Redirecionando para o login Cognito...");
      this.loading = true;
      window.location.href = ENV.API_BASE_URL + "/bff/auth/login";
    }
  }
}