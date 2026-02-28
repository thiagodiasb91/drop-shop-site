import html from "./settings.html?raw";

export function getData() {
  return {
    open: "profile", // seção inicial aberta

    toggle(section) {
      this.open = this.open === section ? null : section;
    }
  };
}

export function render() {
  return html;
}
