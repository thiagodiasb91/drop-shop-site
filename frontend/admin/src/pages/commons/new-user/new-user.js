import html from "./new-user.html?raw";
import stateHelper from "../../../utils/state.helper.js";

export function getData() {
  return {
    email: "",
    async init() {
      const logged = stateHelper.user;
      this.email = logged?.user?.email;
    }
  };
}

export function render() {
  return html;
}
