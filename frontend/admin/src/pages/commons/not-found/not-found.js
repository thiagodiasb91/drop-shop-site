import html from "./not-found.html?raw";

export function getData() {
  return {
    currentPageAccessed: window.location.pathname,
  };
}

export function render() {
  return html;
}
