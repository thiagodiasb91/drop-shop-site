import html from "./dashboard.html?raw"
import AuthService from "../../services/auth.service.js"

export function getData() {
  return {
  }
}

export function render() {
  console.log("page.dashboard.render.loaded");
  return html;
}
