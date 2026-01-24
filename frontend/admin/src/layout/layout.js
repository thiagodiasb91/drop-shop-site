import layoutAuthHtml from './layout-logged.html?raw';
import layoutHtml from './layout-public.html?raw';

console.log("layout.module.loaded");

export async function loadLayout(app, route) {
  console.log("layout.loadLayout.request", route);

  if (route.public) {
    console.log("Carregando layout público.");
    app.innerHTML = layoutHtml;
  } else {
    console.log("Carregando layout protegido.");
    app.innerHTML = layoutAuthHtml;
  }

  if (route.js) {
    console.log("Carregando script da rota:", route.js);
    const { getData } = await import(route.js);
    console.log("Imported:", getData);
    Alpine.data("getData", getData);
  }

  const content = document.getElementById("content");
  console.log("Carregando conteúdo da rota:", route.html);
  content.innerHTML = await fetch(route.html).then(r => r.text());
  console.log("Conteúdo carregado.");

  Alpine.initTree(content);

  console.log("layout.loadLayout.completed");
}


