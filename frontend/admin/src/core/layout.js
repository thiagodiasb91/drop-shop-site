console.log("layout.module.loaded");

const layouts = {
  "authenticaded": () => import('../layout/layout-authenticated.html?raw'),
  "public": () => import('../layout/layout-public.html?raw'),
  // "clean": () => import('../layout/layout-clean.html?raw')
}

async function getLayout(layout) {
  const l = (await layouts[layout]()).default;
  console.log("layout.getLayout.response", layout);
  return l
}

export async function loadLayout(app, route) {
  console.log("layout.loadLayout.request", route);

  Alpine.data("setPageTitle", () => ({
    pageTitle: route.title ?? "Nada"
  }));


  if (route.layout) {
    app.innerHTML = await getLayout(route.layout);
  }
  else if (route.public) {
    app.innerHTML = await getLayout('public');
    console.log("layout.loadLayout.public");
  } else {
    app.innerHTML = await getLayout('authenticaded');
    console.log("layout.loadLayout.authenticated");
  }

  const content = document.getElementById("content");
  console.log("layout.loadLayout.contentLoaded", content);

  if (!route.js)
    return;

  console.log("layout.loadLayout.importing", route.js);
  try {
    const module = await route.js();
    console.log("layout.loadLayout.getData", module?.getData);

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


