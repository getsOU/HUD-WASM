/* Adjust only the formula pulse without restarting its phase.
   Final shaking is CSS-only and ends with the actual SDK state, not a predicted deadline. */
(() => {
    "use strict";
    const previous = new WeakMap();
    function refresh() {
        for (const element of document.querySelectorAll(
            ".bomb-status[data-pulse-ms]",
        )) {
            const milliseconds = Number(element.dataset.pulseMs);
            if (
                !Number.isFinite(milliseconds) ||
                milliseconds < 100 ||
                milliseconds > 1000
            )
                continue;
            const pulse = element
                .querySelector(".bomb-state-symbol")
                ?.getAnimations()
                .find(
                    (animation) => animation.animationName === "bomb-breathe",
                );
            if (!pulse) continue;
            const rate = 1000 / milliseconds;
            const last = previous.get(element);
            if (last?.pulse !== pulse || last.rate !== rate) {
                pulse.updatePlaybackRate(rate);
                previous.set(element, { pulse, rate });
            }
        }
    }
    new MutationObserver(refresh).observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ["data-pulse-ms", "data-bomb-state"],
    });
    matchMedia("(prefers-reduced-motion: reduce)").addEventListener(
        "change",
        refresh,
    );
    document.addEventListener("visibilitychange", refresh);
    refresh();
})();
