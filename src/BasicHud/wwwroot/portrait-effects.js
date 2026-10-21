/* Motion drives interruptible intensity tweens. Canvas generates evolving geometry
   every frame; the outer CSS mask preserves the selected portrait's alpha. */
(() => {
    "use strict";
    const instances = new Map();
    const reduced = matchMedia("(prefers-reduced-motion: reduce)");
    const timings = { smoke: [.65, .9], flash: [.16, .26], fire: [.32, .48] };
    const tau = Math.PI * 2;
    const fract = n => n - Math.floor(n);
    function growth(from, baseDuration) {
        const values = [from], times = [0];
        let elapsed = 0;
        const stages = [[1 / 3, 1 / 3, .25 / baseDuration], [1, 3 / baseDuration, 0]];
        let previous = 0;
        for (const [end, rise, hold] of stages) {
            if (from < end) {
                elapsed += rise * baseDuration * (end - Math.max(from, previous)) / (end - previous);
                values.push(end); times.push(elapsed);
            }
            if (from <= end && hold > 0) {
                elapsed += hold * baseDuration;
                values.push(end); times.push(elapsed);
            }
            previous = end;
        }
        return elapsed > 0 ? { values, duration: elapsed, times: times.map(time => time / elapsed) }
            : { values: [from, 1], duration: 0, times: [0, 1] };
    }
    const arrival = (level, index, count) => {
        const delay = index / count * .32;
        const progress = Math.max(0, Math.min(1, (level - delay) / (1 - delay)));
        return progress * progress * (3 - 2 * progress);
    };
    function cloud(ctx, x, y, radius, time, seed) {
        // Build one seamless cloud outline from overlapping, breathing round puffs.
        const puffs = Array.from({ length: 6 }, (_, i) => {
            const angle = i / 6 * tau + seed * .7;
            const orbit = .48 + Math.sin(time * .5 + seed + i) * .05;
            return {
                x: Math.cos(angle) * orbit,
                y: Math.sin(angle) * orbit,
                radius: .49 + Math.sin(time * .8 + seed * 2 + i * 1.7) * .08
            };
        });
        ctx.beginPath();
        for (let i = 0; i < 96; i++) {
            const angle = i / 96 * tau;
            const dx = Math.cos(angle), dy = Math.sin(angle);
            let edge = .66;
            for (const puff of puffs) {
                const projection = dx * puff.x + dy * puff.y;
                const discriminant = puff.radius ** 2 - puff.x ** 2 - puff.y ** 2 + projection ** 2;
                if (discriminant >= 0) edge = Math.max(edge, projection + Math.sqrt(discriminant));
            }
            const px = x + dx * edge * radius * 1.24;
            const py = y + dy * edge * radius * .88;
            if (i === 0) ctx.moveTo(px, py);
            else ctx.lineTo(px, py);
        }
        ctx.closePath(); ctx.fill(); ctx.stroke();
    }
    class PortraitParticles {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext("2d");
            this.channels = Object.fromEntries(Object.keys(timings).map(key => [key, { value: 0, radius: 0, target: false, control: null }]));
            this.frame = 0;
            this.binding = null;
            this.tick = this.tick.bind(this);
            this.resize = new ResizeObserver(() => this.wake());
            this.resize.observe(canvas);
            this.update();
        }
        update() {
            if (this.binding !== this.canvas.dataset.binding) {
                for (const channel of Object.values(this.channels)) {
                    channel.control?.stop(); channel.control = null;
                    channel.value = 0; channel.radius = 0; channel.target = false;
                }
                this.binding = this.canvas.dataset.binding;
            }
            for (const [key, channel] of Object.entries(this.channels)) {
                const target = this.canvas.dataset[key] === "true";
                if (target === channel.target && !reduced.matches) continue;
                channel.control?.stop();
                channel.target = target;
                const next = target ? 1 : 0;
                if (reduced.matches) { channel.value = next; channel.radius = next; }
                else if (key === "flash") {
                    // Fresh flashes expand opaquely. Clearing freezes the reached
                    // radius and fades alpha; reactivation resumes both in place.
                    if (target && channel.value <= .0001) { channel.radius = 0; channel.value = 1; }
                    const radius = channel.radius, opacity = channel.value;
                    const nextRadius = target ? 1 : radius;
                    channel.control = Motion.animate(0, 1, {
                        duration: timings.flash[target ? 0 : 1] * Math.max(Math.abs(nextRadius - radius), Math.abs(next - opacity)),
                        ease: "linear",
                        onUpdate: progress => {
                            channel.radius = radius + (nextRadius - radius) * progress;
                            channel.value = opacity + (next - opacity) * progress;
                            this.wake();
                        }
                    });
                }
                else {
                    // Fire retains its slow surge; smoke has one shared linear flight.
                    // Every replacement starts at the exact current intensity.
                    const plan = target && key === "fire" ? growth(channel.value, timings[key][0]) : null;
                    channel.control = Motion.animate(channel.value, plan?.values ?? next, {
                        duration: plan?.duration ?? (target && key === "smoke" ? 3.45 : timings[key][target ? 0 : 1]) * Math.abs(next - channel.value),
                        ...(plan ? { times: plan.times } : {}),
                        ease: "linear",
                        onUpdate: value => { channel.value = value; this.wake(); }
                    });
                }
            }
            this.wake();
        }
        wake() { if (!this.frame) this.frame = requestAnimationFrame(this.tick); }
        tick(now) {
            this.frame = 0;
            const canvas = this.canvas;
            if (!canvas.isConnected) return;
            const bounds = canvas.getBoundingClientRect();
            const ratio = Math.min(devicePixelRatio || 1, 2);
            const w = Math.max(1, Math.round(bounds.width * ratio));
            const h = Math.max(1, Math.round(bounds.height * ratio));
            if (canvas.width !== w || canvas.height !== h) { canvas.width = w; canvas.height = h; }
            const ctx = this.ctx;
            ctx.setTransform(w / 100, 0, 0, h / 100, 0, 0);
            ctx.clearRect(0, 0, 100, 100);
            const t = reduced.matches ? 1.4 : now / 1000;
            for (const key of ["smoke", "fire", "flash"]) {
                const level = this.channels[key].value;
                if (level > .0001) this[key](ctx, t, level);
            }
            // Hidden effects have no idle frame loop.
            if (!reduced.matches && Object.values(this.channels).some(c => c.value > .0001 || c.target)) this.wake();
        }
        smoke(ctx, t, level) {
            ctx.save();
            // All clouds start together at one height below the avatar and share
            // an arrival time. Higher destinations therefore have faster flights.
            for (let i = 0; i < 12; i++) {
                const row = Math.floor(i / 3);
                if (level === 0) continue;
                const radius = 29 + i % 3 * 4;
                const x = i % 3 * 48 + 3 + Math.sin(t * .6 + i * 2.3) * 10 * level;
                const restingY = 96 - row * 34 + Math.cos(t * .7 + i) * 7;
                const y = 140 + (restingY - 140) * level;
                ctx.globalAlpha = Math.min(1, level * 4) * .92;
                ctx.fillStyle = ["#bdc9d3", "#dae3e9", "#aabac7"][i % 3];
                ctx.strokeStyle = "#91a5b5"; ctx.lineWidth = .8;
                cloud(ctx, x, y, radius, t, i);
            }
            ctx.restore();
        }
        fire(ctx, t, level) {
            ctx.save();
            // Rebuild the flame contours every frame rather than move flame artwork.
            for (let i = 0; i < 9; i++) {
                const x = i * 14 - 7;
                const height = 36 + (Math.sin(t * 8 + i * 2.1) + 1) * 20;
                const sway = Math.sin(t * 6 + i * 1.9) * 9;
                const progress = arrival(level, i, 9);
                ctx.save(); ctx.translate(0, (1 - progress) * (height + 12));
                ctx.fillStyle = "#ff7124"; ctx.strokeStyle = "#ffd25b"; ctx.lineWidth = 1.3;
                const tipX = x + sway, tipY = 100 - height;
                ctx.beginPath(); ctx.moveTo(x - 17, 105);
                // Broad curved shoulders taper to a recognisable flame point.
                ctx.bezierCurveTo(x - 29, 80, tipX - 16, tipY + 21, tipX, tipY);
                ctx.bezierCurveTo(tipX + 2, tipY + 26, x + 33, 81, x + 18, 105);
                ctx.closePath(); ctx.fill(); ctx.stroke();
                ctx.fillStyle = "#ffe88b";
                const innerX = x + sway / 2, innerY = 100 - height * .55;
                ctx.beginPath(); ctx.moveTo(x - 9, 105);
                ctx.bezierCurveTo(x - 16, 90, innerX - 9, innerY + 12, innerX, innerY);
                ctx.bezierCurveTo(innerX + 1, innerY + 16, x + 20, 87, x + 10, 105); ctx.fill();
                ctx.restore();
            }
            for (let i = 0; i < 14; i++) {
                const age = fract(t * .8 + i * .618);
                ctx.globalAlpha = 1 - age; ctx.fillStyle = "#ffe192";
                const progress = arrival(level, i, 14);
                ctx.beginPath(); ctx.arc(fract(i * .7549) * 100 + Math.sin(t * 3 + i) * 6, 108 - age * 110 * progress, 1.4 * (1 - age), 0, tau); ctx.fill();
            }
            ctx.restore();
        }
        flash(ctx, t, level) {
            // A physical circle expands from the bottom left to the furthest corner.
            const aspect = this.canvas.width / this.canvas.height;
            const radius = Math.hypot(100 * aspect, 100) * this.channels.flash.radius;
            ctx.save(); ctx.scale(1 / aspect, 1);
            ctx.globalAlpha = level;
            ctx.fillStyle = "#fff7d2";
            ctx.beginPath(); ctx.arc(0, 100, radius, 0, tau); ctx.fill();
            // Draw accents inside the existing surface without increasing its alpha.
            ctx.globalCompositeOperation = "source-atop";
            ctx.globalAlpha = 1;
            ctx.strokeStyle = "#ffd76b"; ctx.lineWidth = 4;
            ctx.beginPath(); ctx.arc(0, 100, Math.max(0, radius - 2), 0, tau); ctx.stroke();
            ctx.strokeStyle = "#ffffff"; ctx.lineWidth = 2.5;
            for (let i = 0; i < 3; i++) {
                const age = fract(t * 1.4 + i / 3);
                ctx.globalAlpha = (1 - age) * .8;
                ctx.beginPath(); ctx.arc(0, 100, radius * age, 0, tau); ctx.stroke();
            }
            ctx.restore();
        }
        dispose() {
            cancelAnimationFrame(this.frame);
            for (const channel of Object.values(this.channels)) channel.control?.stop();
            this.resize.disconnect();
        }
    }
    function reconcile(records = []) {
        for (const [canvas, instance] of instances) {
            if (!canvas.isConnected) { instance.dispose(); instances.delete(canvas); }
        }
        for (const canvas of document.querySelectorAll("canvas[data-portrait-effects]")) {
            if (!instances.has(canvas)) instances.set(canvas, new PortraitParticles(canvas));
        }
        for (const record of records) {
            if (record.type === "attributes") instances.get(record.target)?.update();
        }
    }
    new MutationObserver(reconcile).observe(document.body, {
        subtree: true, childList: true, attributes: true,
        attributeFilter: ["data-binding", "data-smoke", "data-flash", "data-fire"]
    });
    reduced.addEventListener("change", () => { for (const instance of instances.values()) instance.update(); });
    reconcile();
})();
