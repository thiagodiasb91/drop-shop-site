import html from "./index.html?raw"

export function getData() {
  return {
    currentPageAccessed: window.location.pathname,
  }
}

export function render() {
  console.log("page.not-found.render.loaded");
  return html;
}
