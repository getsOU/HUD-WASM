import assert from "node:assert/strict";
import { spawn, execFileSync } from "node:child_process";
import { mkdtemp, readFile, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { chromium } from "playwright";

const sdkTool = path.resolve("artifacts/published-sdk-tools/.store/sou.hud.tool/0.3.0-preview.9/sou.hud.tool/0.3.0-preview.9/tools/net10.0/any/SOU.Hud.Tool.dll");
const packagePath = path.resolve(
    process.argv[2] ?? "artifacts/unified-preview-2026-11-15/package",
);
await mkdir("artifacts", { recursive: true });
const scratch = await mkdtemp(path.resolve("artifacts/views-layout-"));
execFileSync("dotnet", [
    sdkTool,
    "fixtures",
    "create",
    path.join(scratch, "source.json"),
]);
const template = JSON.parse(
    await readFile(path.join(scratch, "source.json"), "utf8"),
);
const fixture = JSON.parse(
    await readFile("tests/fixtures/recording-showcase.json", "utf8"),
);
const snapshot = template.snapshots[0];
snapshot.managedMatch = fixture.snapshots[0].managedMatch;
snapshot.game.managedMatch = snapshot.managedMatch;
snapshot.game.map = "de_mirage";
snapshot.game.roundNumber = 6;
snapshot.game.roundPhase = "over";
snapshot.managedMatch.teamA.name = "Example Alpha";
snapshot.managedMatch.teamB.name = "Example Bravo";
snapshot.managedMatch.vetos = [
    "Dust2",
    "Mirage",
    "Inferno",
    "Nuke",
    "Ancient",
    "Anubis",
    "Train",
].map((name, i) => ({
    mapId: "example-veto-" + i,
    name,
    gameName: "de_" + name.toLowerCase(),
    type: i === 6 ? "decider" : i % 2 === 0 ? "ban" : "pick",
    teamId: i === 6 ? null : i % 2 === 0 ? "example-alpha" : "example-bravo",
    number: i + 1,
    playNumber: i % 2 === 1 ? (i + 1) / 2 : null,
}));
snapshot.game.players.forEach((p, i) => {
    p.side = i < 5 ? "terrorists" : "counterTerrorists";
    if (i === 0) p.name = "A Very Long Player Name With Unicode \u00dc";
});
snapshot.game.observedPlayer = snapshot.game.players[0];
fixture.snapshots = [snapshot];
fixture.loop = false;
const fixturePath = path.join(scratch, "fixture.json");
await writeFile(fixturePath, JSON.stringify(fixture));
const manifest = JSON.parse(
    await readFile(path.join(packagePath, "hud.json"), "utf8"),
);
const host = spawn(
    "dotnet",
    [
        sdkTool,
        "dev",
        packagePath,
        "--port",
        "0",
        "--fixture",
        fixturePath,
    ],
    { stdio: ["ignore", "pipe", "pipe"] },
);
let output = "",
    browser,
    page,
    origin;
host.stdout.on("data", (v) => (output += v));
host.stderr.on("data", (v) => (output += v));
const until = async (fn) => {
    const end = Date.now() + 30000;
    while (!(await fn())) {
        if (Date.now() > end) throw new Error("Timeout " + output);
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
    const views = [
        "live",
        ...manifest.subviews.map((v) => v.route.replace("views/", "")),
    ];
    const sizes = [
        [1920, 1080],
        [1280, 720],
        [2560, 1440],
        [3840, 2160],
        [1024, 768],
        [2560, 1080],
        [390, 844],
        [960, 300],
    ];
    const report = [];
    for (const view of views) {
        await hud.setViewportSize({ width: 1920, height: 1080 });
        await hud.goto(
            new URL(
                `hud-packages/${manifest.id}/${manifest.version}/${view === "live" ? "" : `views/${view}`}`,
                origin,
            ).href,
        );
        await hud
            .locator(view === "live" ? ".roster" : ".broadcast-scene")
            .first()
            .waitFor();
        if (view === "live")
            await hud.locator(".live-player").first().waitFor();
        else if (
            [
                "team-economy",
                "round-compare",
                "side-statistics",
                "multikills",
                "round-buys",
                "economy-evidence",
            ].includes(view)
        )
            await hud
                .locator('.recording-scene[data-state="available"]')
                .waitFor();
        if (view === "lineups")
            await until(
                async () =>
                    (await hud.locator(".lineup-team .player-card").count()) ===
                    10,
            );
        if (["statistics", "postmatch"].includes(view))
            await until(
                async () =>
                    (await hud
                        .locator(".statistics-table tbody tr")
                        .count()) === 10,
            );
        if (["prematch", "break", "veto"].includes(view))
            await hud.locator(".duel-team h2").first().waitFor();
        if (view === "spotlight")
            await hud.locator(".player-card").first().waitFor();

        let baseline;
        for (const [width, height] of sizes) {
            await hud.setViewportSize({ width, height });
            await hud.evaluate(
                () =>
                    new Promise((r) =>
                        requestAnimationFrame(() => requestAnimationFrame(r)),
                    ),
            );
            const data = await hud.evaluate(() => {
                const canvas = document.querySelector(".hud"),
                    c = canvas.getBoundingClientRect(),
                    s = document.querySelector(".broadcast-scene"),
                    h = s?.querySelector(".broadcast-heading");
                const rect = (e) => {
                    const b = e.getBoundingClientRect();
                    return {
                        x: (b.x - c.x) / c.width,
                        y: (b.y - c.y) / c.width,
                        w: b.width / c.width,
                        h: b.height / c.width,
                    };
                };
                const bad = [
                    ...document.querySelectorAll(
                        ".scene-body, .lineup-columns, .lineup-team, .player-card, .player-identity, .player-vitals, .inventory, .player-round, .statistics-table tr, .veto-card, .spotlight-layout, .feature-reference, .map-series, .recording-stage",
                    ),
                ]
                    .filter((e) => {
                        const b = e.getBoundingClientRect(),
                            p = (s ?? canvas).getBoundingClientRect();
                        return (
                            b.left < p.left - 1 ||
                            b.right > p.right + 1 ||
                            b.top < p.top - 1 ||
                            b.bottom > p.bottom + 1
                        );
                    })
                    .map((e) => e.className);
                return {
                    canvas: rect(canvas),
                    font:
                        parseFloat(getComputedStyle(canvas).fontSize) / c.width,
                    icons: [...document.querySelectorAll(".hud-ui-icon")]
                        .slice(0, 20)
                        .map((e) => e.getBoundingClientRect().height / c.width),
                    header: h ? rect(h) : null,
                    frame: s ? rect(s) : null,
                    bad,
                    scroll: s ? s.scrollHeight > s.clientHeight + 1 : false,
                    scrollDetails: [
                        ...document.querySelectorAll(
                            ".scene-body, .statistics-table-wrap",
                        ),
                    ].map((e) => ({
                        cls: e.className,
                        sh: e.scrollHeight,
                        ch: e.clientHeight,
                        sw: e.scrollWidth,
                        cw: e.clientWidth,
                    })),
                    innerScroll: [
                        ...document.querySelectorAll(".statistics-table-wrap"),
                    ].some(
                        (e) =>
                            e.scrollHeight > e.clientHeight + 1 ||
                            e.scrollWidth > e.clientWidth + 1,
                    ),
                    fit:
                        c.width <= innerWidth + 1 &&
                        c.height <= innerHeight + 1,
                };
            });
            assert(data.fit, `${view} canvas fits ${width}x${height}`);
            assert(Math.abs(data.canvas.h - 9 / 16) < 0.001);
            assert(Math.abs(data.font - 0.01) < 0.0001);
            assert.deepEqual(
                data.bad,
                [],
                `${view} content out of bounds at ${width}x${height}`,
            );
            assert(
                !data.scroll && !data.innerScroll,
                `${view} has no scroll/clipping at ${width}x${height}: ${JSON.stringify(data)}`,
            );
            if (baseline) {
                data.icons.forEach((h, i) =>
                    assert(
                        Math.abs(h - baseline.icons[i]) < 0.001,
                        `${view} icons scale with the canvas ${width}x${height} [${i}]: ${h} vs ${baseline.icons[i]}`,
                    ),
                );
                for (const key of ["header", "frame"])
                    if (data[key])
                        for (const dim of ["x", "y", "w", "h"])
                            assert(
                                Math.abs(data[key][dim] - baseline[key][dim]) <
                                    0.002,
                                `${view} ${key} scales proportionally`,
                            );
            } else baseline = data;
            if (width === 1920 || width === 390 || width === 1024 || (width === 2560 && height === 1080))
                await hud.screenshot({
                    path: path.join(scratch, `${view}-${width}x${height}.png`),
                    omitBackground: true,
                });
        }
        if (view !== "live") {
            assert(Math.abs(baseline.frame.x) < 0.002);
            assert(Math.abs(baseline.frame.y) < 0.002);
            assert(Math.abs(baseline.frame.w - 1) < 0.002);
            assert(Math.abs(baseline.frame.h - 9 / 16) < 0.002);
        }
        // The same scene also scales inside a constrained container within a large window.
        await hud.setViewportSize({ width: 1920, height: 1080 });
        await hud.locator(".viewport").evaluate((e) => {
            e.style.width = "960px";
            e.style.height = "540px";
        });
        const embedded = await hud
            .locator(".hud")
            .evaluate((e) => ({
                width: e.getBoundingClientRect().width,
                font: parseFloat(getComputedStyle(e).fontSize),
            }));
        assert(
            Math.abs(embedded.width - 960) < 1 &&
                Math.abs(embedded.font - 9.6) < 0.05,
            `${view} embedded output uses container size`,
        );
        await hud.locator(".viewport").evaluate((e) => {
            e.style.width = "";
            e.style.height = "";
        });
        if (view !== "live") {
            await command({
                kind: "action",
                section: "production",
                name: "lowerThird",
                value: "show",
            });
            await hud.locator(".has-lower-third").waitFor();
            const lower = await hud
                .locator(".broadcast-scene")
                .evaluate((s) => {
                    const b = s.getBoundingClientRect();
                    return [
                        ...s.querySelectorAll(
                            ".scene-body, .player-card, .player-identity, .player-vitals, .inventory, .player-round, .statistics-table tr, .veto-card, .spotlight-layout, .map-series, .recording-stage, .compare-player, .side-board-row, .multikill-player, .buy-player, .evidence-player",
                        ),
                    ]
                        .filter(
                            (e) =>
                                e.getBoundingClientRect().bottom > b.bottom + 1,
                        )
                        .map((e) => e.className);
                });
            assert.deepEqual(
                lower,
                [],
                `${view} reserves room for the lower third`,
            );
            await command({
                kind: "action",
                section: "production",
                name: "lowerThird",
                value: "hide",
            });
            await hud.waitForFunction(
                () => !document.querySelector(".has-lower-third"),
            );
        }
        report.push({
            view,
            sizes: sizes.length,
            embedded: true,
            lowerThird: view !== "live",
        });
        console.log("PASS", view, "shared frame and proportional sizing");
    }
    assert.deepEqual(errors, []);
    await writeFile(
        path.join(scratch, "result.json"),
        JSON.stringify({ passed: true, views: report, sizes, errors }, null, 2),
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
