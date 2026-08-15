// Development service worker — passes all requests straight through to the network.
// In production the published version (service-worker.published.js) takes over.
self.addEventListener("fetch", () => {});
