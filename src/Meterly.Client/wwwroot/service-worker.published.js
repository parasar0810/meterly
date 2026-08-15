// Production service worker with offline-first caching for Blazor WASM assets.
// The build pipeline auto-generates "service-worker-assets.js" and injects it
// as self.assetsManifest so we always cache the correct hashed filenames.

const CACHE_NAME = "meterly-cache-v1";

self.addEventListener("install", event => {
    event.waitUntil(onInstall(event));
});

self.addEventListener("activate", event => {
    event.waitUntil(onActivate(event));
});

self.addEventListener("fetch", event => {
    event.respondWith(onFetch(event));
});

async function onInstall(event) {
    self.skipWaiting();
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: "no-cache" }));

    await caches.open(CACHE_NAME).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    // Remove old caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key !== CACHE_NAME)
        .map(key => caches.delete(key))
    );
}

async function onFetch(event) {
    if (event.request.method !== "GET") return;

    const shouldServeIndexHtml =
        event.request.mode === "navigate" &&
        !["/_", "/api/"].some(prefix => event.request.url.includes(prefix));

    const request = shouldServeIndexHtml ? "index.html" : event.request;
    const cachedResponse = await caches.match(request);
    return cachedResponse || fetch(event.request);
}

const offlineAssetsInclude = [
    /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/,
    /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/
];
const offlineAssetsExclude = [/^_content\//, /service-worker\.js$/];
