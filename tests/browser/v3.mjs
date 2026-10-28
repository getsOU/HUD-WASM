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
const dead = structuredClone(hurt);
dead.game.players[6].health = 0;
const fixture = path.join(scratch, 'selective.json');
await writeFile(fixture, JSON.stringify({ schemaVersion: 1, intervalMilliseconds: 5000, loop: false,
  snapshots: [...Array(100).fill(baseline), moved, shot, hurt, dead] }));

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
  const cards = () => hud.locator('.live-player').evaluateAll(elements => elements.map(el => {
    const copy = el.cloneNode(true);
    // Responsive canvases resize independently of Blazor/data updates.
    copy.querySelectorAll('canvas').forEach(canvas => { canvas.removeAttribute('width'); canvas.removeAttribute('height'); });
    return copy.outerHTML;
  }));
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
  const beforeHorizontal = await hud.locator('.live-player').elementHandles();
  const beforeCanvases = await hud.locator('.live-player canvas').elementHandles();
  await command({ kind: 'panel', values: { display: { horizontalPlayercards: true } } });
  await hud.locator('.hud.rosters-horizontal').waitFor();
  assert(await hud.evaluate(before => before.every((node, index) => node === document.querySelectorAll('.live-player')[index]), beforeHorizontal),
    'layout switching must preserve the actual player-card elements');
  assert(await hud.evaluate(before => before.every((node, index) => node === document.querySelectorAll('.live-player canvas')[index]), beforeCanvases),
    'layout switching must preserve the existing effect canvases');
  const horizontalGeometry = async () => hud.evaluate(() => {
    const rect = element => { const box = element.getBoundingClientRect(); return { x: box.x, y: box.y, right: box.right, bottom: box.bottom, width: box.width, height: box.height }; };
    return { observer: rect(document.querySelector('.observed')), viewport: rect(document.querySelector('.hud')),
      teams: Array.from(document.querySelectorAll('.roster')).map(team => Array.from(team.querySelectorAll('.live-player')).map(rect)),
      healthFits: Array.from(document.querySelectorAll('.card-health')).every(element => { const box = rect(element), parent = rect(element.closest('.live-player')); return box.x >= parent.x - 1 && box.right <= parent.right + 1 && element.scrollWidth <= element.clientWidth + 1; }) };
  });
  const checkHorizontal = geometry => {
    assert.equal(geometry.teams.length, 2);
    for (const team of geometry.teams) {
      assert.equal(team.length, 5);
      for (let index = 0; index < team.length; index++) {
        const card = team[index];
        assert(Math.abs(card.bottom - team[0].bottom) < 1, 'all five cards share the same bottom edge');
        assert(card.x >= geometry.viewport.x && card.right <= geometry.viewport.right + 1 && card.bottom <= geometry.viewport.bottom + 1);
        if (index > 0) assert(card.x > team[index - 1].right, 'horizontal cards must not overlap');
      }
    }
    assert(geometry.teams[0].at(-1).right < geometry.observer.x, 'CT cards stay left of the observer');
    assert(geometry.teams[1][0].x > geometry.observer.right, 'T cards stay right of the observer');
    assert(geometry.healthFits, 'health values remain inside their cards');
  };
  checkHorizontal(await horizontalGeometry());
  const cardContent = await hud.locator('.live-player').evaluateAll(cards => cards.map(card => {
    const bounds = selector => card.querySelector(selector).getBoundingClientRect();
    return { side: card.classList.contains('t') ? 't' : 'ct', card: { left: card.getBoundingClientRect().left, right: card.getBoundingClientRect().right },
      slot: { left: bounds('.card-slot').left, right: bounds('.card-slot').right },
      weaponLeft: bounds('.weapon-cell').left, healthLeft: bounds('.card-health').left,
      nameAlignment: getComputedStyle(card.querySelector('.card-identity')).textAlign,
      statOrder: getComputedStyle(card.querySelector('.card-stats > span')).flexDirection,
      utilityOrder: getComputedStyle(card.querySelector('.utility-icons')).flexDirection,
      utilityColumn: getComputedStyle(card.querySelector('.utility-icons')).gridColumnStart,
      equipmentColumn: getComputedStyle(card.querySelector('.card-status')).gridColumnStart };
  }));
  for (const card of cardContent) {
    assert(card.weaponLeft < card.healthLeft, 'both teams show weapon left and health right');
    assert.equal(card.nameAlignment, 'left');
    assert.equal(card.statOrder, 'row');
    assert.equal(card.utilityOrder, 'row');
    assert.equal(card.utilityColumn, '1');
    assert.equal(card.equipmentColumn, '2');
    assert(Math.abs(card.slot.left - card.card.left) < 1,
      'observer slot and side strip stay on the left for both teams');
  }
  const healthBackgrounds = await hud.locator('.roster.ct .live-player').evaluateAll(cards => cards.map(card => {
    const fill = getComputedStyle(card, '::before');
    return { health: Number(card.style.getPropertyValue('--player-health').replace('%', '')), height: card.getBoundingClientRect().height,
      width: card.getBoundingClientRect().width, fillHeight: parseFloat(fill.height), fillWidth: parseFloat(fill.width), bottom: fill.bottom,
      loadoutBackground: getComputedStyle(card.querySelector('.card-loadout')).backgroundColor,
      oldTrack: getComputedStyle(card.querySelector('.health-track')).display };
  }));
  for (const card of healthBackgrounds) {
    assert(Math.abs(card.fillHeight - card.height * card.health / 100) < 1, 'health controls the full-card background height');
    assert(Math.abs(card.fillWidth - card.width) < 1 && card.bottom === '0px', 'health spans the card width and recedes from the top');
    assert.equal(card.loadoutBackground, 'rgba(0, 0, 0, 0)');
    assert.equal(card.oldTrack, 'none');
  }
  assert.equal(healthBackgrounds[1].health, 72);
  await command({ kind: 'step' });
  await hud.locator('.roster.t .live-player.is-dead').waitFor();
  const deadCard = await hud.locator('.roster.t .live-player.is-dead').evaluate(card => ({
    height: card.getBoundingClientRect().height,
    aliveHeight: card.parentElement.querySelector('.live-player:not(.is-dead)').getBoundingClientRect().height,
    skullVisible: getComputedStyle(card.querySelector('.card-death')).display !== 'none',
    identityBottom: card.querySelector('.card-identity').getBoundingClientRect().bottom,
    bottom: card.getBoundingClientRect().bottom
  }));
  assert(!deadCard.skullVisible && deadCard.height < deadCard.aliveHeight * 0.75,
    'horizontal dead cards omit the large skull and the entire lower equipment row');
  assert(deadCard.identityBottom <= deadCard.bottom + 1, 'compact dead cards retain the complete identity and statistics');
  assert(await hud.locator('.roster.t .live-player.is-dead').evaluate(card =>
    Math.abs(card.querySelector('.card-slot').getBoundingClientRect().left - card.getBoundingClientRect().left) < 1),
    'dead T cards retain their left-hand strip');
  checkHorizontal(await horizontalGeometry());
  await page.screenshot({ path: path.join(scratch, 'horizontal-1920x1080.png') });
  await page.setViewportSize({ width: 1600, height: 900 });
  checkHorizontal(await horizontalGeometry());
  await page.setViewportSize({ width: 1920, height: 1080 });
  await command({ kind: 'panel', values: { display: { horizontalPlayercards: false } } });
  await hud.waitForFunction(() => !document.querySelector('.rosters-horizontal'));
  const vertical = await hud.locator('.roster.ct .live-player').evaluateAll(elements => elements.map(element => ({ x: element.getBoundingClientRect().x, y: element.getBoundingClientRect().y })));
  assert(vertical.every(card => Math.abs(card.x - vertical[0].x) < 1) && vertical[1].y > vertical[0].y);
  assert.equal(await hud.locator('.roster.ct .health-track').first().evaluate(element => getComputedStyle(element).display), 'block');
  assert.notEqual(await hud.locator('.roster.t .live-player.is-dead .card-death').evaluate(element => getComputedStyle(element).display), 'none',
    'vertical dead cards retain their original death marker');
  console.log('PASS horizontal playercards, dead-card spacing, unclipped health, observer clearance, proportional scaling and vertical restoration');
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
