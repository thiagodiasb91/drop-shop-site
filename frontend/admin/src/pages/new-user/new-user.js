import html from "./new-user.html?raw"
import AuthService from "../../services/auth.service";

export function getData() {
  return {
    email: "",
    async init() {
      const logged = await AuthService.me()
      this.email = logged?.user?.email
    }
  }
}

export function render() {
  console.log("page.new-user.render.loaded");
  return html;
}
