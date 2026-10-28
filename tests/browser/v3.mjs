import assert from 'node:assert/strict';
import { spawn, execFileSync } from 'node:child_process';
import { mkdtemp, readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const packagePath = path.resolve(process.argv[2] ?? 'artifacts/package');
await mkdir('artifacts', { recursive: true });
const scratch = await mkdtemp(path.resolve('artifacts/browser-v3-'));
const original = path.join(scratch, 'example.json');
execFileSync('dotnet', ['tool', 'run', 'sou-hud', '--', 'fixtures', 'create', original]);
const example = JSON.parse(await readFile(original, 'utf8'));
const baseline = structuredClone(example.snapshots[0]);
baseline.game.roundPhase = 'live';
baseline.game.countdown = { phase: 'paused', secondsRemaining: 90 };
const moved = structuredClone(baseline);
moved.game.players[0].position.x += 200;
moved.game.observedPlayer = structuredClone(moved.game.players[0]);
const shot = structuredClone(moved);
shot.game.players[0].weapons[0].ammoClip = 17;
shot.game.observedPlayer = structuredClone(shot.game.players[0]);
const hurt = structuredClone(shot);
hurt.game.players[1].health = 72;
const fixture = path.join(scratch, 'selective.json');
await writeFile(fixture, JSON.stringify({ schemaVersion: 1, intervalMilliseconds: 5000, loop: false,
  snapshots: [...Array(100).fill(baseline), moved, shot, hurt] }));

const host = spawn('dotnet', ['tool', 'run', 'sou-hud', '--', 'dev', packagePath, '--port', '0', '--fixture', fixture], { stdio: ['ignore', 'pipe', 'pipe'] });
let browser;
let origin, controlPage;
let hostOutput = '';
host.stdout.on('data', value => { hostOutput += value; });
host.stderr.on('data', value => { hostOutput += value; });
const until = async (ready, timeout = 15000) => {
  const end = Date.now() + timeout;
  while (!await ready()) { if (Date.now() > end) throw new Error(`Timed out. Host: ${hostOutput}`); await new Promise(resolve => setTimeout(resolve, 30)); }
};
try {
  await until(() => /Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(hostOutput));
  origin = hostOutput.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
  browser = await chromium.launch({ headless: true, ...(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE } : {}) });
  const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
  controlPage = page;
  const errors = [], sockets = new Set(), socketUrls = [], optionsRequests = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => { if (request.url().includes('/hud-browser/options')) optionsRequests.push(request.url()); });
  page.on('websocket', socket => { sockets.add(socket); socketUrls.push(socket.url()); socket.on('close', () => sockets.delete(socket)); });
  const command = async value => {
    const response = await page.request.post(new URL('__dev/command', origin).href, { headers: { 'X-SOU-DEV': '1' }, data: value });
    assert(response.ok(), await response.text());
  };
  await page.goto(origin);
  await command({ kind: 'pause' });
  const manifest = JSON.parse(await readFile(path.join(packagePath, 'hud.json'), 'utf8'));
  const output = new URL(`hud-packages/${manifest.id}/${manifest.version}/`, origin).href;
  await page.addStyleTag({ content: '#hud{position:fixed;inset:0;width:100vw;height:100vh;z-index:10}' });
  const hud = await (await page.locator('#hud').elementHandle()).contentFrame();
  await hud.locator('.roster .live-player').first().waitFor({ timeout: 30000 });
  await hud.waitForFunction(() => document.querySelectorAll('.radar-player').length === 10);
  await until(() => sockets.size === 1);
  assert(socketUrls.every(url => url.includes('/hud-live/v3')), socketUrls.join('\n'));
  const cards = () => hud.locator('.live-player').evaluateAll(elements => elements.map(el => el.outerHTML));
  const radar = () => hud.locator('.radar-player').evaluateAll(elements => JSON.stringify(elements.map(el => el.getAttribute('style'))));
  const beforeCards = await cards(), beforeRadar = await radar();
  for (let step = 0; step < 105 && await radar() === beforeRadar; step++) {
    await command({ kind: 'step' }); await page.waitForTimeout(40);
  }
  await until(async () => await radar() !== beforeRadar);
  assert.deepEqual(await cards(), beforeCards);
  console.log('PASS v3 pose changes move radar without changing player cards');
  await command({ kind: 'step' });
  await hud.waitForFunction(() => document.querySelector('.observed .ammo')?.textContent.includes('17'));
  assert.deepEqual(await cards(), beforeCards);
  console.log('PASS active ammunition updates the observer without refreshing unrelated card content');
  await command({ kind: 'step' });
  await until(async () => (await cards()).some((html, index) => html !== beforeCards[index]));
  const afterCards = await cards();
  assert.equal(afterCards.filter((html, index) => html !== beforeCards[index]).length, 1);
  assert.match(afterCards[1], /72/);
  console.log('PASS health changes update exactly one player card');
  await command({ kind: 'options', options: { caption: 'Typed v3 caption' } });
  await hud.locator('.caption').filter({ hasText: 'Typed v3 caption' }).waitFor();
  await command({ kind: 'panel', values: { display: { caption: 'Panel caption', hideRosters: true } } });
  await hud.waitForFunction(() => document.querySelectorAll('.roster').length === 0);
  await hud.locator('.caption').filter({ hasText: 'Panel caption' }).waitFor();
  await command({ kind: 'panel', values: { display: { hideRosters: false } } });
  await hud.waitForFunction(() => document.querySelectorAll('.roster').length === 2);
  console.log('PASS typed options, panel caption precedence and hide/show defaults');
  for (const view of manifest.subviews) {
    await command({ kind: 'view', route: view.route });
    await hud.waitForURL(new URL(view.route, output).href);
    await hud.locator('.presentation').waitFor();
  }
  await command({ kind: 'view', route: '' });
  await hud.waitForURL(output);
  await hud.locator('.roster .live-player').first().waitFor();
  console.log('PASS all declared manager views and return to live');
  await command({ kind: 'action', section: 'production', name: 'lowerThird', value: 'show' });
  await hud.locator('.has-lower-third').waitFor();
  await command({ kind: 'reconnect' });
  await hud.waitForFunction(() => !document.querySelector('.has-lower-third'));
  await hud.locator('.roster .live-player').first().waitFor();
  await until(() => sockets.size === 1);
  assert.equal(optionsRequests.length, 0, 'v3 must not start a separate options poll');
  assert.deepEqual(errors, []);
  await page.screenshot({ path: path.join(scratch, 'live-1920x1080.png') });
  console.log('PASS actions reset on reconnect, one active transport, no legacy streams or options polling');
} finally {
  if (origin && controlPage) {
    try { await controlPage.request.post(new URL('__dev/command', origin).href, { headers: { 'X-SOU-DEV': '1' }, data: { kind: 'stop' }, timeout: 2000 }); }
    catch { /* The host may close its socket before the stop response arrives. */ }
  }
  await browser?.close();
  if (host.exitCode === null) host.kill();
  await writeFile(path.join(scratch, 'host.log'), hostOutput);
}
