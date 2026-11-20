import {sdkToolPath} from './sdk-tool.mjs';
import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { mkdtemp, readFile, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { chromium } from "playwright";
const tool = path.resolve(
    sdkToolPath,
);
const packagePath = path.resolve(process.argv[2]),
    fixture = JSON.parse(
        await readFile("tests/fixtures/recording-showcase.json", "utf8"),
    );
const cases = [
    { name: "carried", state: "carried" },
    { name: "plant-start", state: "planting", seconds: 3.2, offset: 100 },
    { name: "plant-half", state: "planting", seconds: 1.6, offset: 50 },
    { name: "plant-abort", state: "carried" },
    { name: "plant-missing", state: "planting" },
    { name: "afterplant", state: "planted", seconds: 20, urgency: "normal" },
    { name: "warning", state: "planted", seconds: 8, urgency: "warning" },
    {
        name: "exact-five",
        state: "planted",
        seconds: 5,
        urgency: "critical",
        paused: true,
    },
    {
        name: "exact-three",
        state: "planted",
        seconds: 3,
        urgency: "critical",
        paused: true,
    },
    { name: "critical", state: "planted", seconds: 4, urgency: "critical" },
    {
        name: "final-strength",
        state: "planted",
        seconds: 2.5,
        urgency: "critical",
    },
    {
        name: "deadline-300ms",
        state: "planted",
        seconds: 0.3,
        urgency: "critical",
        deadline: true,
    },
    { name: "final-timing-gap", state: "planted", urgency: "critical" },
    {
        name: "negative-countdown",
        state: "planted",
        seconds: -0.1,
        urgency: "critical",
    },
    { name: "final-zero", state: "planted", seconds: 0, urgency: "critical" },
    { name: "waiting-for-explosion", state: "planted", urgency: "critical" },
    {
        name: "round-over-gap",
        state: "planted",
        urgency: "critical",
        roundPhase: "over",
        round: 6,
    },
    { name: "supplied-explosion", state: "exploded" },
    {
        name: "paused",
        state: "planted",
        seconds: 4,
        urgency: "critical",
        paused: true,
    },
    {
        name: "kit-start",
        state: "defusing",
        seconds: 5,
        kit: true,
        offset: 100,
    },
    {
        name: "kit-half",
        state: "defusing",
        seconds: 2.5,
        kit: true,
        offset: 50,
    },
    {
        name: "hands-start",
        state: "defusing",
        seconds: 10,
        kit: false,
        offset: 100,
    },
    {
        name: "hands-half",
        state: "defusing",
        seconds: 5,
        kit: false,
        offset: 50,
    },
    { name: "unknown-kit", state: "defusing", seconds: 2.5 },
    { name: "unknown-defuse-time", state: "defusing", kit: true },
    { name: "defuse-abort", state: "planted", seconds: 20, urgency: "normal" },
    { name: "defused", state: "defused" },
    { name: "next-round", state: "carried" },
    { name: "exploded", state: "exploded" },
    { name: "dropped", state: "dropped" },
    { name: "unknown", state: "unknown" },
];
const base = structuredClone(fixture.snapshots[0]);
fixture.snapshots = cases.map((c) => {
    const s = structuredClone(base),
        defuser = s.game.players.find((p) => p.side === "counterTerrorists");
    defuser.defuseKit = c.kit ?? null;
    s.game.observedPlayer = structuredClone(defuser);
    s.game.roundPhase = c.roundPhase ?? "live";
    s.game.roundNumber = c.round ?? 5;
    s.game.bomb = {
        state: c.state,
        playerSteamId: c.state === "defusing" ? defuser.steamId : null,
        position: null,
        secondsRemaining: c.seconds ?? null,
    };
    s.game.roundBomb = null;
    s.game.countdown = {
        phase: c.paused ? "paused" : "live",
        secondsRemaining: 90,
    };
    return s;
});
fixture.loop = false;
await mkdir("artifacts", { recursive: true });
const scratch = await mkdtemp(path.resolve("artifacts/bomb-status-browser-")),
    fixturePath = path.join(scratch, "fixture.json");
await writeFile(fixturePath, JSON.stringify(fixture));
const manifest = JSON.parse(
    await readFile(path.join(packagePath, "hud.json"), "utf8"),
);
const host = spawn(
    "dotnet",
    [tool, "dev", packagePath, "--port", "0", "--fixture", fixturePath],
    { stdio: ["ignore", "pipe", "pipe"] },
);
let output = "",
    origin,
    browser,
    page;
host.stdout.on("data", (v) => (output += v));
host.stderr.on("data", (v) => (output += v));
const until = async (fn) => {
    const deadline = Date.now() + 30000;
    while (!(await fn())) {
        if (Date.now() > deadline) throw new Error("Timeout: " + output);
        await new Promise((r) => setTimeout(r, 30));
    }
};
try {
    await until(() =>
        /Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output),
    );
    origin = output.match(
        /Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/,
    )[1];
    browser = await chromium.launch({
        headless: true,
        executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE,
    });
    page = await browser.newPage();
    await page.goto(origin);
    const command = async (data) => {
        const r = await page.request.post(
            new URL("__dev/command", origin).href,
            { headers: { "X-SOU-DEV": "1" }, data },
        );
        assert(r.ok(), await r.text());
    };
    await command({ kind: "pause" });
    const hud = await browser.newPage({
            viewport: { width: 1920, height: 1080 },
        }),
        errors = [];
    hud.on("pageerror", (e) => errors.push(e.message));
    const stem = new URL(
        `hud-packages/${manifest.id}/${manifest.version}/`,
        origin,
    ).href;
    await hud.goto(stem);
    await hud.locator(".scoreboard").waitFor();
    const active = new Set([
        "planting",
        "planted",
        "defusing",
        "defused",
        "exploded",
    ]);
    let normalColor,
        lastShake = "none",
        lastPeriod = null,
        lastGlyph = null;
    for (let i = 0; i < cases.length; i++) {
        if (i) await command({ kind: "step" });
        const c = cases[i],
            state = c.state;
        if (active.has(state)) {
            const indicator = hud.locator(`[data-bomb-state="${state}"]`);
            await indicator.waitFor();
            const glyph =
                state === "defused"
                    ? "check"
                    : state === "exploded"
                      ? "explosion"
                      : state === "defusing"
                        ? c.kit === true
                            ? "kit"
                            : c.kit === false
                              ? "hand"
                              : "defuse"
                        : "bomb";
            const shown = indicator.locator(".bomb-glyph.shown");
            await until(
                async () => (await shown.getAttribute("data-glyph")) === glyph,
            );
            assert.equal(
                await shown.count(),
                1,
                "one glyph is selected for the current state",
            );
            if (glyph === "kit")
                assert.equal(await shown.locator("use").getAttribute("href"),
                    "assets/icons/hud-ui.svg#kit", "defuse tools use the intact shared pliers geometry");
            if (glyph === "defuse")
                assert.equal(await shown.locator("path").getAttribute("d"),
                    await indicator.locator('[data-glyph="hand"] path').getAttribute("d"),
                    "generic defuse uses the intact hand instead of disconnected tool fragments");
            const glyphDurations = await shown.evaluate((e) =>
                getComputedStyle(e)
                    .transitionDuration.split(",")
                    .map((v) => parseFloat(v)),
            );
            assert(glyphDurations.every((v) => v <= 0.18));
            if (state === "defused" || state === "exploded")
                assert(
                    glyphDurations.every((v) => v === 0),
                    "terminal symbols appear immediately without a lingering bomb",
                );
            if (lastGlyph && lastGlyph !== glyph) {
                await until(
                    async () =>
                        await shown.evaluate(
                            (g) => Number(getComputedStyle(g).opacity) > 0,
                        ),
                );
                // The active class changes once; CSS blends the stable SVG groups, without extra timers.
                assert(
                    (await indicator
                        .locator(`[data-glyph="${lastGlyph}"]`)
                        .count()) === 1,
                );
            }
            lastGlyph = glyph;
            if (state === "exploded" || state === "defused") {
                assert.equal(
                    await indicator
                        .locator(".bomb-state-symbol")
                        .evaluate(
                            (e) =>
                                e.getAnimations().filter((a) => a.animationName)
                                    .length,
                        ),
                    0,
                    "reported terminal state stops motion immediately",
                );
            }

            if (c.deadline) {
                await until(
                    async () =>
                        (await indicator.getAttribute("data-urgency")) ===
                        "critical",
                );
                assert(
                    await indicator.evaluate(
                        (e) =>
                            parseFloat(getComputedStyle(e).transitionDuration) *
                                1000 <=
                            150,
                    ),
                    "subsecond transitions remain short",
                );
                await until(
                    async () =>
                        Number(
                            await indicator.getAttribute("data-time-left-ms"),
                        ) === 0,
                );
                assert.equal(
                    await indicator.getAttribute("data-motion-expired"),
                    "false",
                    "predicted zero cannot stop the shake",
                );
                const time = await indicator
                    .locator(".bomb-state-symbol")
                    .evaluate((e) => {
                        window.__finalBombShake = e
                            .getAnimations()
                            .find((a) => a.animationName === "bomb-rattle");
                        return window.__finalBombShake.currentTime;
                    });
                await until(
                    async () =>
                        await indicator
                            .locator(".bomb-state-symbol")
                            .evaluate(
                                (e, time) =>
                                    e
                                        .getAnimations()
                                        .includes(window.__finalBombShake) &&
                                    window.__finalBombShake.currentTime >
                                        time + 300,
                                time,
                            ),
                );
                assert.equal(
                    await indicator.getAttribute("data-bomb-state"),
                    "planted",
                    "no explosion is inferred from zero",
                );
                assert.equal(
                    await indicator.getAttribute("data-urgency"),
                    "critical",
                );
                lastShake = "strong";
                lastPeriod = 100;
                continue;
            }

            assert.equal(
                await hud.locator(".clock strong, .bomb-status-label").count(),
                0,
                "bomb timer and visible captions removed",
            );
            const kit =
                c.kit === true ? "yes" : c.kit === false ? "no" : "unknown";
            await until(
                async () =>
                    (await indicator.getAttribute("data-kit")) ===
                    (state === "defusing" ? kit : "unknown"),
            );
            if (c.offset !== undefined) {
                const arc = hud.locator(".bomb-ring-progress");
                await arc.waitFor();
                await until(
                    async () =>
                        Math.abs(
                            Number(
                                await arc.getAttribute("stroke-dashoffset"),
                            ) - c.offset,
                        ) < 15,
                );
                assert.equal(
                    await arc.evaluate(
                        (e) => getComputedStyle(e).animationName,
                    ),
                    "none",
                    "progress uses remaining time, never a spinner",
                );
            } else if (state === "planting" || state === "defusing") {
                await until(
                    async () =>
                        (await hud.locator(".bomb-ring-progress").count()) ===
                        0,
                );
                assert.equal(
                    await hud.locator(".bomb-status-ring circle").count(),
                    0,
                    "unknown progress has no placeholder circle",
                );
            }
            if (c.urgency) {
                await until(
                    async () =>
                        (await indicator.getAttribute("data-urgency")) ===
                        c.urgency,
                );
                if (c.name === "afterplant")
                    normalColor = await indicator.evaluate(
                        (e) => getComputedStyle(e).color,
                    );
                if (c.urgency !== "normal")
                    await until(
                        async () =>
                            (await indicator.evaluate(
                                (e) => getComputedStyle(e).color,
                            )) !== normalColor,
                    );
                assert(
                    await indicator.evaluate(
                        (e) =>
                            parseFloat(
                                getComputedStyle(e).transitionDuration,
                            ) <= 0.22,
                    ),
                );
                const expectedShake =
                    c.seconds !== undefined
                        ? c.seconds <= 5
                            ? "strong"
                            : c.seconds <= 10
                              ? "light"
                              : "none"
                        : lastShake;
                await until(
                    async () =>
                        (await indicator.getAttribute("data-shake")) ===
                        expectedShake,
                );
                lastShake = expectedShake;
                const expectedPeriod =
                    c.seconds !== undefined
                        ? 100 + 900 * Math.max(0, Math.min(c.seconds / 40, 1))
                        : lastPeriod;
                await until(async () => {
                    const value = Number(
                        await indicator.getAttribute("data-pulse-ms"),
                    );
                    return (
                        value <= expectedPeriod + 1 &&
                        value >= Math.max(100, expectedPeriod - 40)
                    );
                });
                lastPeriod = Number(
                    await indicator.getAttribute("data-pulse-ms"),
                );
                const symbol = hud.locator(".bomb-state-symbol");
                if (
                    (await indicator.getAttribute("data-motion-expired")) ===
                    "true"
                ) {
                    assert.equal(
                        await symbol.evaluate(
                            (e) =>
                                e.getAnimations().filter((a) => a.animationName)
                                    .length,
                        ),
                        0,
                        "expired motion stays stopped across zero and missing timing",
                    );
                } else if (expectedShake === "strong") {
                    const name = "bomb-rattle";
                    await until(
                        async () =>
                            await symbol.evaluate((e, name) => {
                                const animations = e
                                    .getAnimations()
                                    .filter((a) => a.animationName);
                                return (
                                    animations.length === 1 &&
                                    animations[0].animationName === name
                                );
                            }, name),
                    );
                    assert.equal(
                        await symbol.evaluate((e) => getComputedStyle(e).scale),
                        "1",
                        "at or below five seconds there is no residual pulse scale",
                    );
                    assert.equal(
                        await symbol.evaluate(
                            (e) => getComputedStyle(e).transitionDuration,
                        ),
                        "0s",
                        "switching to shake does not leave a lingering scale transition",
                    );
                    const shake = await symbol.evaluate((e) => {
                        const animation = e
                            .getAnimations()
                            .find((a) => a.animationName === "bomb-rattle");
                        const rest = animation.effect
                            .getKeyframes()
                            .filter((k) => k.offset >= 200 / 300 - 1e-7)
                            .map((k) => k.transform);
                        return {
                            duration: animation.effect.getTiming().duration,
                            speed: animation.playbackRate,
                            angle: parseFloat(
                                getComputedStyle(e).getPropertyValue(
                                    "--bomb-shake-angle",
                                ),
                            ),
                            restIsStatic:
                                rest.length >= 2 &&
                                rest.every((v) => v === rest[0]),
                            restMilliseconds:
                                (1 - 200 / 300) *
                                animation.effect.getTiming().duration,
                        };
                    });
                    assert.equal(
                        shake.duration,
                        300,
                        "all final-five-second shake cycles use the same faster timing",
                    );
                    assert.equal(
                        shake.speed,
                        1,
                        "strength changes never accelerate the shake",
                    );
                    assert(
                        shake.restIsStatic &&
                            Math.abs(shake.restMilliseconds - 100) < 0.01,
                        "double shake ends with 100 ms of real rest",
                    );
                    if (c.seconds !== undefined)
                        assert(
                            Math.abs(
                                shake.angle -
                                    (4 +
                                        6 *
                                            Math.max(
                                                0,
                                                Math.min(
                                                    (5 - c.seconds) / 5,
                                                    1,
                                                ),
                                            )),
                            ) < 0.5,
                            "only shake amplitude increases toward zero",
                        );
                    if (c.paused)
                        assert(
                            await symbol.evaluate((e) =>
                                e
                                    .getAnimations()
                                    .filter((a) => a.animationName)
                                    .every((a) => a.playState === "paused"),
                            ),
                        );
                } else {
                    await until(
                        async () =>
                            await symbol.evaluate((e) => {
                                const pulse = e
                                    .getAnimations()
                                    .find(
                                        (a) =>
                                            a.animationName === "bomb-breathe",
                                    );
                                return (
                                    !!pulse &&
                                    Math.abs(
                                        1000 / pulse.playbackRate -
                                            Number(
                                                e.closest(".bomb-status")
                                                    .dataset.pulseMs,
                                            ),
                                    ) < 5
                                );
                            }),
                    );
                    const animations = await symbol.evaluate((e) =>
                        e
                            .getAnimations()
                            .filter((a) => a.animationName)
                            .map((a) => ({
                                name: a.animationName,
                                duration: a.effect.getTiming().duration,
                                state: a.playState,
                            })),
                    );
                    assert(
                        animations.some(
                            (a) =>
                                a.name === "bomb-breathe" &&
                                a.duration === 1000,
                        ),
                        "pulse has a stable timeline with a formula-driven playback rate",
                    );
                    assert.equal(
                        animations.length,
                        1,
                        "early pulse has no secondary animation",
                    );
                    if (c.paused)
                        assert(
                            animations.every((a) => a.state === "paused"),
                            "pause freezes pulse and shake together",
                        );
                    const pulseFrames = await symbol.evaluate((e) =>
                        e
                            .getAnimations()
                            .find((a) => a.animationName === "bomb-breathe")
                            .effect.getKeyframes()
                            .map((k) => k.scale),
                    );
                    assert.equal(
                        pulseFrames.filter((v) => v === "1.08").length,
                        2,
                        "existing double pulse retains its subtle amplitude",
                    );
                    if (c.name === "afterplant" || c.name === "warning") {
                        const start = await symbol.evaluate((e) => {
                            window.__bombAnimation = e
                                .getAnimations()
                                .find(
                                    (a) => a.animationName === "bomb-breathe",
                                );
                            return window.__bombAnimation.currentTime;
                        });
                        await until(
                            async () =>
                                await symbol.evaluate(
                                    (e, start) =>
                                        e
                                            .getAnimations()
                                            .includes(window.__bombAnimation) &&
                                        window.__bombAnimation.currentTime >
                                            start + 600,
                                    start,
                                ),
                        );
                        assert(
                            await symbol.evaluate((e) =>
                                e
                                    .getAnimations()
                                    .includes(window.__bombAnimation),
                            ),
                            "cadence changes preserve animation phase instead of restarting on render updates",
                        );
                    }
                }
                assert.equal(
                    await hud
                        .locator(
                            ".bomb-status-ring, .bomb-ring-track, .bomb-ring-state",
                        )
                        .count(),
                    0,
                    "afterplant has no ring or circle",
                );
                await hud.emulateMedia({ reducedMotion: "reduce" });
                assert.equal(
                    await symbol.evaluate(
                        (e) => getComputedStyle(e).animationName,
                    ),
                    "none",
                    "reduced motion disables pulse and shake",
                );
                await hud.emulateMedia({ reducedMotion: "no-preference" });
            }
            if (state !== "planting" && state !== "defusing") {
                assert.equal(
                    await hud.locator(".bomb-status circle").count(),
                    0,
                    "non-action states have no circle",
                );
                assert.equal(
                    await hud
                        .locator(".bomb-dial")
                        .evaluate((e) => getComputedStyle(e).backgroundColor),
                    "rgba(0, 0, 0, 0)",
                    "no filled circular background replaces the ring",
                );
            }
            if (state === "defusing")
                assert.equal(
                    await hud
                        .locator(".bomb-state-symbol")
                        .evaluate((e) => e.tagName.toLowerCase()),
                    "svg",
                );
            for (const [width, height] of [
                [1920, 1080],
                [1280, 720],
                [960, 540],
                [3840, 2160],
            ]) {
                await hud.setViewportSize({ width, height });
                const bounds = await indicator.evaluate((e) => {
                    const a = e.getBoundingClientRect(),
                        b = e.closest(".score-strip").getBoundingClientRect();
                    return {
                        inside: a.top >= b.top - 1 && a.bottom <= b.bottom + 1,
                        width: a.width,
                        height: a.height,
                    };
                });
                assert(bounds.inside, state + " fits scaled matchbar");
                assert(bounds.width > 0 && bounds.height > 0);
            }
            await hud.setViewportSize({ width: 1920, height: 1080 });
            await hud
                .locator(".scoreboard")
                .screenshot({ path: path.join(scratch, c.name + ".png") });
        } else {
            lastGlyph = null;
            lastPeriod = null;
            await until(
                async () => (await hud.locator(".bomb-status").count()) === 0,
            );
            assert.equal(
                await hud.locator(".clock strong").count(),
                1,
                "regular clock restored",
            );
        }
    }
    // A fresh page receives the current SDK state without retaining a prior terminal indicator.
    await hud.reload();
    await hud.locator(".scoreboard").waitFor();
    assert.deepEqual(errors, []);
    await writeFile(
        path.join(scratch, "result.json"),
        JSON.stringify(
            {
                passed: true,
                cases,
                screenSizes: 4,
                noNumericBombTimer: true,
                errors,
            },
            null,
            2,
        ),
    );
    console.log(
        "PASS bomb progress, kit/no-kit symbols, warning/shake thresholds, reduced motion, lifecycle and scaling",
    );
    console.log("Evidence: " + scratch);
} finally {
    if (origin && page)
        try {
            await page.request.post(new URL("__dev/command", origin).href, {
                headers: { "X-SOU-DEV": "1" },
                data: { kind: "stop" },
                timeout: 2000,
            });
        } catch {}
    await browser?.close();
    if (host.exitCode === null) host.kill();
    await writeFile(path.join(scratch, "host.log"), output);
}
