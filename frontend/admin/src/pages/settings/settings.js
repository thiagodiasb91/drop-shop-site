import html from "./settings.html?raw"

export function getData() {
  return {
  }
}

export function render() {
  console.log("page.settings.render.loaded");
  return html;
}
