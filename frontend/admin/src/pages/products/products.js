import html from "./index.html?raw"

export function getData() {
  return {
  }
}

export function render() {
  console.log("page.products.render.loaded");
  return html;
}
