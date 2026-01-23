export async function loadLayout(app, route) {
  console.log("router.loadLayout.request", route);
  const layoutHtml = await fetch("/static/layout/layout.html").then(r => r.text());
  console.log("Layout HTML carregado:", layoutHtml);
  app.innerHTML = layoutHtml;

  const content = document.getElementById("content");
  content.innerHTML = await fetch(route.html).then(r => r.text());

  if (route.js) {
    console.log("Carregando script da rota:", route.js);
    const imported = await import(route.js);
    console.log("Imported:", imported);
    Alpine.data("getData", imported.getData);
  }
  
  window.Alpine = Alpine;
  Alpine.start();
}
