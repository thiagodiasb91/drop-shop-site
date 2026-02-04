import layoutAuthHtml from '../layout/layout-authenticated.html?raw';
import layoutHtml from '../layout/layout-public.html?raw';

console.log("layout.module.loaded");

export async function loadLayout(app, route) {
  console.log("layout.loadLayout.request", route);

  Alpine.data("setPageTitle", () => ({
    pageTitle: route.title ?? "Nada"
  }));

  if (route.public) {
    app.innerHTML = layoutHtml;
    console.log("layout.loadLayout.public");
  } else {
    app.innerHTML = layoutAuthHtml;
    console.log("layout.loadLayout.authenticated");
  }

  const content = document.getElementById("content");
  console.log("layout.loadLayout.contentLoaded", content);

  if (!route.js)
    return;

  console.log("layout.loadLayout.importing", route.js);
  try {
    const module = await route.js();
    console.log("layout.loadLayout.getData", module.getData);

    if (module.render) {
      console.log("layout.loadLayout.initializingAlpineForContent");
      content.innerHTML = module.render();
    }

    if (module.getData) {
      console.log("layout.loadLayout.initializingGetData");
      Alpine.data("getData", () => module.getData());
    }

    console.log("layout.loadLayout.initializingLayout");

    Alpine.initTree(content);
  }
  catch (error) {
    console.error("layout.loadLayout.errorImporting", error);
    // loadLayout(app, { js: "./pages/not-found/not-found.js" });
  }

  console.log("layout.loadLayout.completed");
}


