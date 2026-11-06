/* Motion reveals the MVP below the matchbar; Blazor owns its award and expiry. */
(() => {
    "use strict";
    const instances = new Map();
    const reduced = matchMedia("(prefers-reduced-motion: reduce)");
    class MvpMotion {
        constructor(element) {
            this.element = element;
            this.hud = element.closest(".hud.scene-live");
            this.value = 0;
            this.target = false;
            this.key = null;
            this.control = null;
            this.paint(0);
            this.update();
        }
        paint(value) {
            this.value = Math.max(0, Math.min(1, value));
            this.element.style.setProperty("--mvp-progress", this.value.toFixed(5));
            this.element.style.setProperty("--mvp-content-progress", Math.max(0, (this.value - .18) / .82).toFixed(5));
            this.hud?.style.setProperty("--mvp-reveal", this.value.toFixed(5));
        }
        update(force = false) {
            const target = this.element.dataset.mvpVisible === "true";
            const key = this.element.dataset.mvpKey;
            const changed = key !== this.key;
            if (!force && target === this.target && !changed) return;
            this.control?.stop(); this.control = null;
            // A new award starts fresh; an interrupted exit reverses from its current position.
            if (changed && target && this.target) this.paint(0);
            this.key = key; this.target = target;
            const next = target ? 1 : 0;
            if (reduced.matches || document.hidden || !window.Motion) { this.paint(next); return; }
            const milliseconds = Number(target ? this.element.dataset.mvpEnterMs : this.element.dataset.mvpExitMs);
            this.control = Motion.animate(this.value, next, {
                duration: milliseconds / 1000 * Math.abs(next - this.value),
                ease: target ? [.22, 1, .36, 1] : [.4, 0, 1, 1],
                onUpdate: value => { if (this.element.isConnected) this.paint(value); }
            });
        }
        dispose() {
            this.control?.stop();
            this.hud?.style.removeProperty("--mvp-reveal");
        }
    }
    function reconcile(records = []) {
        for (const [element, instance] of instances) {
            if (!element.isConnected) { instance.dispose(); instances.delete(element); }
        }
        for (const element of document.querySelectorAll("[data-mvp-motion]")) {
            if (!instances.has(element)) instances.set(element, new MvpMotion(element));
        }
        for (const record of records) {
            if (record.type === "attributes") instances.get(record.target)?.update();
        }
    }
    new MutationObserver(reconcile).observe(document.body, {
        subtree: true, childList: true, attributes: true,
        attributeFilter: ["data-mvp-visible", "data-mvp-key"]
    });
    const refresh = () => { for (const instance of instances.values()) instance.update(true); };
    reduced.addEventListener("change", refresh);
    document.addEventListener("visibilitychange", refresh);
    reconcile();
})();
