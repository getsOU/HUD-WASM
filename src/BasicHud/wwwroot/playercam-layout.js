// Position the single persistent receiver layer over the current portrait targets.
// Layout observers only: no transport, media capture, timers or viewer protocol.
(() => {
  const start = () => {
    const app = document.getElementById('app');
    if (!app) return;
    const watched = new Set();
    const sizes = new ResizeObserver(() => layout());
    function layout() {
      const targets = new Map();
      for (const target of app.querySelectorAll('[data-playercam-target]')) {
        const id = target.dataset.playercamTarget;
        if (id) targets.set(id, target);
      }
      const elements = new Set(targets.values());
      for (const layer of app.querySelectorAll('.playercam-layer')) {
        elements.add(layer);
        const root = layer.getBoundingClientRect();
        if (!root.width || !root.height) continue;
        const sx = layer.clientWidth / root.width, sy = layer.clientHeight / root.height;
        for (const receiver of layer.querySelectorAll('[data-playercam-player]')) {
          const target = receiver.dataset.playercamActive === 'true' && targets.get(receiver.dataset.playercamPlayer);
          if (!target) { receiver.removeAttribute('data-positioned'); continue; }
          const rect = target.getBoundingClientRect();
          if (!rect.width || !rect.height) { receiver.removeAttribute('data-positioned'); continue; }
          receiver.style.left = `${(rect.left - root.left) * sx}px`;
          receiver.style.top = `${(rect.top - root.top) * sy}px`;
          receiver.style.width = `${rect.width * sx}px`;
          receiver.style.height = `${rect.height * sy}px`;
          receiver.dataset.positioned = 'true';
        }
      }
      for (const target of watched) if (!elements.has(target)) { sizes.unobserve(target); watched.delete(target); }
      for (const target of elements) if (!watched.has(target)) { watched.add(target); sizes.observe(target); }
    }
    new MutationObserver(layout).observe(app, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-playercam-target', 'data-playercam-active'] });
    layout();
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
  else start();
})();
