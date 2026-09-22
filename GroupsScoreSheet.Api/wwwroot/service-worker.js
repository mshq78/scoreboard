const CACHE_VERSION = "groups-score-sheet-v4";
const APP_SHELL_CACHE = `${CACHE_VERSION}-app-shell`;

const REQUIRED_APP_SHELL_FILES = [
  "/index.html",
];

const OPTIONAL_APP_SHELL_FILES = [
  "/",
  "/evaluator-logo.png",
  "/manifest.webmanifest",
  "/pwa-icon.svg",
];

const APP_SHELL_FILES = [
  ...REQUIRED_APP_SHELL_FILES,
  ...OPTIONAL_APP_SHELL_FILES,
];

self.addEventListener("install", (event) => {
  event.waitUntil(
    cacheAppShell()
      .then(() => self.skipWaiting()),
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((cacheNames) => {
        return Promise.all(
          cacheNames
            .filter((cacheName) => cacheName.startsWith("groups-score-sheet-"))
            .filter((cacheName) => !cacheName.startsWith(CACHE_VERSION))
            .map((cacheName) => caches.delete(cacheName)),
        );
      })
      .then(() => self.clients.claim()),
  );
});

self.addEventListener("fetch", (event) => {
  const request = event.request;

  if (request.method !== "GET") {
    return;
  }

  const url = new URL(request.url);

  if (url.origin !== self.location.origin) {
    return;
  }

  if (url.pathname.startsWith("/api/")) {
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(handleNavigationRequest(request));
    return;
  }

  if (url.pathname.startsWith("/assets/")) {
    event.respondWith(cacheFirst(request));
    return;
  }

  event.respondWith(networkFirst(request));
});

async function cacheAppShell() {
  const cache = await caches.open(APP_SHELL_CACHE);

  await Promise.all(
    APP_SHELL_FILES.map(async (path) => {
      try {
        const response = await fetch(path, { cache: "reload" });

        if (response.ok) {
          await cache.put(path, response.clone());
        }
      } catch (error) {
        if (REQUIRED_APP_SHELL_FILES.includes(path)) {
          throw error;
        }

        console.warn("Optional app shell file could not be cached:", path, error);
      }
    }),
  );
}

async function handleNavigationRequest(request) {
  try {
    const networkResponse = await fetch(request);

    if (networkResponse.ok) {
      const cache = await caches.open(APP_SHELL_CACHE);
      await cache.put("/index.html", networkResponse.clone());
    }

    return networkResponse;
  } catch {
    const cachedIndex = await caches.match("/index.html");

    if (cachedIndex) {
      return cachedIndex;
    }

    return new Response(
      "Application is offline and index.html is not cached.",
      {
        status: 503,
        headers: {
          "Content-Type": "text/plain; charset=utf-8",
        },
      },
    );
  }
}

async function cacheFirst(request) {
  const cachedResponse = await caches.match(request);

  if (cachedResponse) {
    return cachedResponse;
  }

  const networkResponse = await fetch(request);

  if (networkResponse.ok) {
    const cache = await caches.open(APP_SHELL_CACHE);
    await cache.put(request, networkResponse.clone());
  }

  return networkResponse;
}

async function networkFirst(request) {
  try {
    const networkResponse = await fetch(request);

    if (networkResponse.ok) {
      const cache = await caches.open(APP_SHELL_CACHE);
      await cache.put(request, networkResponse.clone());
    }

    return networkResponse;
  } catch {
    const cachedResponse = await caches.match(request);

    if (cachedResponse) {
      return cachedResponse;
    }

    throw new Error("Network request failed and no cache was found.");
  }
}
