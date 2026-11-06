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
baseline.game.roundNumber = 10;
baseline.game.players[0].stats.mvps = 0;
baseline.game.players[0].roundKills = 5;
baseline.game.players[5].health = 5;
const moved = structuredClone(baseline);
moved.game.players[0].position.x += 200;
moved.game.observedPlayer = structuredClone(moved.game.players[0]);
const shot = structuredClone(moved);
shot.game.players[0].weapons[0].ammoClip = 17;
shot.game.observedPlayer = structuredClone(shot.game.players[0]);
const hurt = structuredClone(shot);
hurt.game.players[1].health = 5;
const dead = structuredClone(hurt);
dead.game.players[6].health = 0;
const earlyAward = structuredClone(dead);
earlyAward.game.players[0].name = 'catgirl sofc';
earlyAward.game.players[0].stats.mvps = 1;
earlyAward.game.players[0].stats.kills += 1;
earlyAward.game.players[0].roundKills = 2;
earlyAward.game.players[0].roundDamage = 200;
const roundOver = structuredClone(earlyAward);
roundOver.game.roundPhase = 'over';
roundOver.game.roundNumber = 11;
roundOver.game.countdown = { phase: 'over', secondsRemaining: 17 };
roundOver.game.roundWinner = 'counterTerrorists';
roundOver.game.players[0].roundKills = 2;
roundOver.game.players[0].roundDamage = 200;
const pausedOver = structuredClone(roundOver);
pausedOver.game.countdown = { phase: 'paused', secondsRemaining: 17 };
pausedOver.game.players[0].stats.score = (pausedOver.game.players[0].stats.score ?? 0) + 1;
const nextFreeze = structuredClone(roundOver);
nextFreeze.game.roundNumber = 11;
nextFreeze.game.roundPhase = 'freezeTime';
nextFreeze.game.countdown = { phase: 'freezeTime', secondsRemaining: 17 };
nextFreeze.game.players[0].roundKills = 0;
nextFreeze.game.players[0].roundDamage = 0;
const pausedFreeze = structuredClone(nextFreeze);
pausedFreeze.game.countdown = { phase: 'paused', secondsRemaining: 17 };
pausedFreeze.game.players[0].stats.score = (pausedFreeze.game.players[0].stats.score ?? 0) + 1;
const nextLive = structuredClone(nextFreeze);
nextLive.game.roundPhase = 'live';
nextLive.game.countdown = { phase: 'live', secondsRemaining: 90 };
const finalAward = structuredClone(nextLive);
finalAward.game.phase = 'gameOver';
finalAward.game.roundPhase = 'over';
finalAward.game.roundNumber = 12;
finalAward.game.countdown = { phase: 'gameOver', secondsRemaining: 0 };
finalAward.game.players[0].stats.mvps = 2;
finalAward.game.players[0].roundKills = 1;
finalAward.game.players[0].roundDamage = 111;
const finalHeartbeat = structuredClone(finalAward);
finalHeartbeat.game.players[0].roundDamage = 112;
const finalUpdate = structuredClone(finalHeartbeat);
finalUpdate.game.players[0].stats.score = (finalUpdate.game.players[0].stats.score ?? 0) + 1;
const competitionBranding = structuredClone(finalUpdate);
competitionBranding.managedMatch = {
  id: 'preview-match', name: 'Preview match', bestOf: 1,
  teamA: { id: 'ct-team', name: 'Preview CT', shortName: 'CT', logo: null, seriesWins: 0 },
  teamB: { id: 't-team', name: 'Preview T', shortName: 'T', logo: null, seriesWins: 0 },
  ct: { side: 'counterTerrorists', status: 'manual', teamId: 'ct-team', teamAScore: 0, teamBScore: 0 },
  t: { side: 'terrorists', status: 'manual', teamId: 't-team', teamAScore: 0, teamBScore: 0 },
  competition: { id: 'preview-league', name: 'Demo League', seasonId: null, seasonName: null, sections: [], round: null,
    logo: '/cast-media/league.png', sponsors: [
      { id: 'a', name: 'Demo A', image: '/cast-media/sponsor-a.png' },
      { id: 'b', name: 'Demo B', image: '/cast-media/sponsor-b.png' },
      { id: 'c', name: 'Demo C', image: '/cast-media/sponsor-c.png' },
      { id: 'a-repeat', name: 'Demo A again', image: '/cast-media/sponsor-a.png' }
    ] }
};
competitionBranding.game.killFeed = [{ id: 1, connectionId: '00000000-0000-0000-0000-000000000001', epoch: 1, gameTimeSeconds: 120,
  killer: { entityIndex: 1, steamId: baseline.game.players[0].steamId, name: 'Preview CT 1', side: 'counterTerrorists' },
  victim: { entityIndex: 6, steamId: baseline.game.players[5].steamId, name: 'Preview T 1', side: 'terrorists' },
  assister: { entityIndex: 2, steamId: baseline.game.players[1].steamId, name: 'Preview CT 2', side: 'counterTerrorists' },
  weapon: 'weapon_ak47', weaponKind: 'ak47', flashAssist: false, headshot: false, wallbang: false,
  attackerBlind: false, throughSmoke: false, noScope: false }];
const fixture = path.join(scratch, 'selective.json');
const scheduledStart = structuredClone(competitionBranding);
scheduledStart.game = null;
scheduledStart.freshness = 'waiting';
scheduledStart.receivedAt = null;
const plannedStartTime = new Date(Date.now() - 5 * 60000).toISOString();
const delayedStartTime = new Date(Date.now() + 10 * 60000).toISOString();
const laterStartTime = new Date(Date.now() + 20 * 60000).toISOString();
scheduledStart.managedMatch.startsAt = plannedStartTime;
scheduledStart.managedMatch.delayedStartsAt = null;
const delayedStart = structuredClone(scheduledStart);
delayedStart.managedMatch.delayedStartsAt = delayedStartTime;
const laterStart = structuredClone(delayedStart);
laterStart.managedMatch.delayedStartsAt = laterStartTime;
const clearedStart = structuredClone(scheduledStart);
const delayedOnlyStart = structuredClone(laterStart);
delayedOnlyStart.managedMatch.startsAt = null;
const unknownStart = structuredClone(scheduledStart);
unknownStart.managedMatch.startsAt = null;
await writeFile(fixture, JSON.stringify({ schemaVersion: 1, intervalMilliseconds: 5000, loop: false,
  snapshots: [...Array(100).fill(baseline), moved, shot, hurt, dead, earlyAward, roundOver, pausedOver, nextFreeze, pausedFreeze, nextLive, finalAward, finalHeartbeat, finalUpdate, competitionBranding,
    scheduledStart, delayedStart, laterStart, clearedStart, delayedOnlyStart, unknownStart, competitionBranding] }));

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
  const page = await browser.newPage({ viewport: { width: 1920, height: 1080 }, timezoneId: 'Europe/Berlin' });
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
  const checkVerticalHealth = async () => {
    const fills = await hud.locator('.roster .live-player:not(.is-dead)').evaluateAll(elements => elements.map(card => {
      const fill = getComputedStyle(card, '::before');
      const box = card.getBoundingClientRect();
      const textBottom = selector => { const range = document.createRange(); range.selectNodeContents(card.querySelector(selector)); return range.getBoundingClientRect().bottom; };
      return { health: Number(card.querySelector('.card-health').textContent), width: box.width, height: box.height,
        slotBaseline: textBottom('.card-slot'), nameBaseline: textBottom('.player-name'),
        slotWidth: card.querySelector('.card-slot').getBoundingClientRect().width, column: fill.gridColumnStart, columnEnd: fill.gridColumnEnd,
        fillWidth: parseFloat(fill.width), fillHeight: parseFloat(fill.height), left: fill.left, right: fill.right,
        t: card.classList.contains('t'), background: getComputedStyle(card).backgroundColor, fillColor: fill.backgroundColor,
        loadoutBackground: getComputedStyle(card.querySelector('.card-loadout')).backgroundColor,
        legacyTrack: Boolean(card.querySelector('.health-track')) };
    }));
    for (const fill of fills) {
      assert(Math.abs(fill.slotBaseline - fill.nameBaseline) < .25, 'vertical observer digits must align with player names');
      assert(Math.abs(fill.fillWidth - (fill.width - fill.slotWidth) * fill.health / 100) < .2,
        'vertical HP percentages must exclude the observer strip, so five HP stays fully visible beside it');
      assert.equal(fill.column, fill.t ? '1' : '2');
      assert.equal(fill.columnEnd, fill.t ? '-2' : '-1', 'the observer grid column must be outside the HP fill area');
      assert(Math.abs(fill.fillHeight - fill.height) < .2, 'vertical HP fill must span the full card height');
      assert.equal(fill.t ? fill.right : fill.left, '0px', 'CT/T fill starts at its outside edge');
      assert.notEqual(fill.background, fill.fillColor, 'the unfilled card must not retain its team-colored background');
      assert.equal(fill.loadoutBackground, 'rgba(0, 0, 0, 0)');
      assert(!fill.legacyTrack, 'there must not be an additional loadout-only health layer');
    }
    assert.equal(fills.filter(fill => fill.health === 5).length, 2, 'five HP is exercised on both sides');
  };
  await hud.waitForFunction(() => [...document.querySelectorAll('.hud-ui-icon use')].length > 0
    && [...document.querySelectorAll('.hud-ui-icon use')].every(use => use.getBBox().width > 0));
  const outlineIcons = await hud.locator('.hud-ui-icon').evaluateAll(icons => icons.map(icon => {
    const box = icon.getBoundingClientRect(), style = getComputedStyle(icon);
    return { width: box.width, height: box.height, stroke: style.stroke, color: style.color, filter: style.filter,
      label: icon.getAttribute('aria-label') };
  }));
  assert(outlineIcons.every(icon => icon.label && icon.stroke === icon.color && icon.filter === 'none'
    && icon.width >= 12 && Math.abs(icon.width - icon.height) < .1));
  assert.equal(await hud.locator('.radar-widget .hud-ui-icon').count(), 0);
  assert.equal(await hud.locator('.killfeed [data-cs2-icon]').count(), 0);
  console.log('PASS shared local outline symbols have readable sizes and CSS color; radar stays unchanged');
  const roundKills = hud.locator('.roster.ct .live-player').first().locator('.round-kill-indicator');
  assert.equal(await roundKills.locator('[data-ui-icon="kills"]').count(), 5, 'each source round kill has its own symbol');
  assert.equal(await roundKills.locator('strong').count(), 0, 'round kills have no visible number');
  assert.equal(await roundKills.evaluate(element => getComputedStyle(element).backgroundColor), 'rgba(0, 0, 0, 0)', 'round-kill stack has no background');
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
  assert.equal(await hud.locator('.roster.ct .card-health').nth(1).textContent(), '5');
  await checkVerticalHealth();
  console.log('PASS health changes update exactly one player card');
  await command({ kind: 'options', options: { caption: 'Typed v3 caption' } });
  await hud.locator('.caption').filter({ hasText: 'Typed v3 caption' }).waitFor();
  await command({ kind: 'panel', values: { display: { caption: 'Panel caption', hideRosters: true } } });
  await hud.waitForFunction(() => document.querySelectorAll('.roster').length === 0);
  await hud.locator('.caption').filter({ hasText: 'Panel caption' }).waitFor();
  await command({ kind: 'panel', values: { display: { caption: '', hideRosters: true } } });
  await hud.waitForFunction(() => !document.querySelector('.caption')?.textContent.trim());
  await command({ kind: 'options', options: { caption: 'Changed options caption' } });
  await page.waitForTimeout(150);
  assert.equal((await hud.locator('.caption').textContent()).trim(), '', 'cleared panel caption must stay empty after options change');
  await command({ kind: 'panel', values: { display: { hideRosters: false } } });
  await hud.waitForFunction(() => document.querySelectorAll('.roster').length === 2);
  console.log('PASS typed options, panel caption precedence, explicitly cleared captions and hide/show defaults');
  const defaultLogo = 'assets/branding/sou-wordmark.png';
  const logo = hud.locator('.caption-brand');
  await until(async () => await logo.getAttribute('src') === defaultLogo);
  const logoBox = await logo.boundingBox();
  const brandingCards = await cards();
  const brandingRadar = await radar();
  const logoBytes = await readFile(path.join(packagePath, 'wwwroot', defaultLogo));
  await page.route('**/cast-media/scoreboard-logo.png', route => route.fulfill({
    contentType: 'image/png', body: logoBytes
  }));
  await command({ kind: 'panel', values: { branding: { logo: '/cast-media/scoreboard-logo.png' } } });
  await hud.waitForFunction(() => {
    const image = document.querySelector('.caption-brand');
    return image.src.endsWith('/cast-media/scoreboard-logo.png') && image.complete && image.naturalWidth > 0;
  });
  assert.deepEqual(await cards(), brandingCards, 'branding changes must leave player card content unchanged');
  assert.equal(await radar(), brandingRadar, 'branding changes must leave radar markers unchanged');
  assert.deepEqual(await logo.evaluate(image => {
    const style = getComputedStyle(image); return { fit: style.objectFit, filter: style.filter };
  }), { fit: 'contain', filter: 'none' }, 'custom logos must fit completely and retain their colors');
  assert.deepEqual(await logo.boundingBox(), logoBox, 'custom logos must preserve scoreboard geometry');
  await page.screenshot({ path: path.join(scratch, 'scoreboard-custom-logo-1920x1080.png') });
  await command({ kind: 'panel', values: { branding: { logo: '' } } });
  await until(async () => await logo.getAttribute('src') === defaultLogo);
  assert.equal(await logo.getAttribute('data-image-fallback'), 'true');
  await page.route('**/cast-media/scoreboard-missing.png', route => route.fulfill({ status: 404, body: '' }));
  const missingLogoResponse = page.waitForResponse(response => response.url().endsWith('/cast-media/scoreboard-missing.png'));
  await command({ kind: 'panel', values: { branding: { logo: '/cast-media/scoreboard-missing.png' } } });
  await missingLogoResponse;
  await until(async () => await logo.getAttribute('src') === defaultLogo);
  await command({ kind: 'panel', values: { branding: { logo: '/cast-media/scoreboard-logo.png' } } });
  await until(async () => (await logo.getAttribute('src')).endsWith('/cast-media/scoreboard-logo.png'));
  await command({ kind: 'panel', values: { branding: { logo: '' } } });
  await until(async () => await logo.getAttribute('src') === defaultLogo);
  console.log('PASS panel scoreboard logo replacement, layout/color preservation, clearing, load-error fallback and recovery');
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
    const textBottom = selector => { const range = document.createRange(); range.selectNodeContents(card.querySelector(selector)); return range.getBoundingClientRect().bottom; };
    return { side: card.classList.contains('t') ? 't' : 'ct', card: { left: card.getBoundingClientRect().left, right: card.getBoundingClientRect().right },
      slotBaseline: textBottom('.card-slot'), nameBaseline: textBottom('.player-name'),
      slot: { left: bounds('.card-slot').left, right: bounds('.card-slot').right },
      weaponLeft: bounds('.weapon-cell').left, healthLeft: bounds('.card-health').left,
      nameAlignment: getComputedStyle(card.querySelector('.card-identity')).textAlign,
      statOrder: getComputedStyle(card.querySelector('.card-stats > span')).flexDirection,
      utilityOrder: getComputedStyle(card.querySelector('.utility-icons')).flexDirection,
      utilityColumn: getComputedStyle(card.querySelector('.utility-icons')).gridColumnStart,
      equipmentColumn: getComputedStyle(card.querySelector('.card-status')).gridColumnStart };
  }));
  for (const card of cardContent) {
    assert(Math.abs(card.slotBaseline - card.nameBaseline) < .25, 'horizontal observer digits must align with player names');
    assert(card.weaponLeft < card.healthLeft, 'both teams show weapon left and health right');
    assert.equal(card.nameAlignment, 'left');
    assert.equal(card.statOrder, 'row');
    assert.equal(card.utilityOrder, 'row');
    assert.equal(card.utilityColumn, '1');
    assert.equal(card.equipmentColumn, '2');
    assert(Math.abs(card.slot.left - card.card.left) < 1,
      'observer slot and side strip stay on the left for both teams');
  }
  const healthBackgrounds = await hud.locator('.roster .live-player').evaluateAll(cards => cards.map(card => {
    const fill = getComputedStyle(card, '::before');
    return { health: Number(card.style.getPropertyValue('--player-health').replace('%', '')), height: card.getBoundingClientRect().height,
      slotWidth: card.querySelector('.card-slot').getBoundingClientRect().width, column: fill.gridColumnStart, columnEnd: fill.gridColumnEnd,
      width: card.getBoundingClientRect().width, fillHeight: parseFloat(fill.height), fillWidth: parseFloat(fill.width), bottom: fill.bottom,
      loadoutBackground: getComputedStyle(card.querySelector('.card-loadout')).backgroundColor,
      legacyTrack: Boolean(card.querySelector('.health-track')) };
  }));
  for (const card of healthBackgrounds) {
    assert(Math.abs(card.fillHeight - card.height * card.health / 100) < 1, 'health controls the full-card background height');
    assert(Math.abs(card.fillWidth - (card.width - card.slotWidth)) < 1 && card.bottom === '0px',
      'horizontal health spans the content width beside the observer strip and recedes from the top');
    assert.equal(card.column, '2');
    assert.equal(card.columnEnd, '-1');
    assert.equal(card.loadoutBackground, 'rgba(0, 0, 0, 0)');
    assert(!card.legacyTrack);
  }
  assert.equal(healthBackgrounds[1].health, 5);
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
  await checkVerticalHealth();
  await page.setViewportSize({ width: 1600, height: 900 });
  await checkVerticalHealth();
  await page.setViewportSize({ width: 1920, height: 1080 });
  assert.notEqual(await hud.locator('.roster.t .live-player.is-dead .card-death').evaluate(element => getComputedStyle(element).display), 'none',
    'vertical dead cards retain their original death marker');
  console.log('PASS horizontal playercards, dead-card spacing, unclipped health, observer clearance, proportional scaling and vertical restoration');
  assert.equal(await hud.locator('.round-mvp').count(), 0);
  await hud.evaluate(() => {
    window.mvpMotionSamples = [];
    window.mvpMotionObserver = new MutationObserver(records => {
      for (const record of records) {
        const element = record.target;
        if (record.type === 'attributes' && element.matches('.round-mvp'))
          window.mvpMotionSamples.push({ key: element.dataset.mvpKey, visible: element.dataset.mvpVisible,
            progress: Number(element.style.getPropertyValue('--mvp-progress')) });
      }
    });
    window.mvpMotionObserver.observe(document.body, { subtree: true, attributes: true, attributeFilter: ['style', 'data-mvp-visible'] });
  });
  const beforeEarlyAward = await cards();
  await command({ kind: 'step' });
  await until(async () => JSON.stringify(await cards()) !== JSON.stringify(beforeEarlyAward));
  assert.equal(await hud.locator('.round-mvp').count(), 0, 'an early MVP increment remains pending while the phase is still live');
  await command({ kind: 'step' });
  await hud.locator('.round-mvp').waitFor();
  const mvpShownAt = Date.now();
  await hud.waitForFunction(() => Number(document.querySelector('.round-mvp')?.style.getPropertyValue('--mvp-progress')) >= .999);
  assert(await hud.evaluate(() => window.mvpMotionSamples.some(sample => sample.visible === 'true' && sample.progress > 0 && sample.progress < 1)),
    'MVP must animate through intermediate reveal states with bundled Motion');
  const checkMvpLayout = async () => {
    const boxes = await hud.evaluate(() => {
      const rect = selector => { const box = document.querySelector(selector)?.getBoundingClientRect();
        return box ? { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: box.width } : null; };
      const borderless = selector => ['Top', 'Right', 'Bottom', 'Left'].every(side =>
        getComputedStyle(document.querySelector(selector))[`border${side}Width`] === '0px');
      const name = document.querySelector('.mvp-name');
      const nameBox = name.getBoundingClientRect();
      const text = document.createRange(); text.selectNodeContents(name);
      const textBox = text.getBoundingClientRect();
      return { bar: rect('.scoreboard'), mvp: rect('.round-mvp'), history: rect('.round-history'), hud: rect('.hud'),
        nameFitsVertically: textBox.top >= nameBox.top && textBox.bottom <= nameBox.bottom,
        borderless: borderless('.scoreboard') && borderless('.round-mvp'),
        backgroundsMatch: getComputedStyle(document.querySelector('.scoreboard')).backgroundColor === getComputedStyle(document.querySelector('.round-mvp')).backgroundColor };
    });
    assert.equal(boxes.mvp.left, boxes.bar.left, 'MVP left edge must equal the matchbar');
    assert.equal(boxes.mvp.width, boxes.bar.width, 'MVP width must exactly equal the matchbar');
    assert(Math.abs(boxes.mvp.top - boxes.bar.bottom - boxes.hud.width * .00625) < .1,
      'MVP must use the same proportional gap as the vertical player cards: 12 px at 1920');
    assert(boxes.borderless, 'matchbar and MVP must have no outside borders');
    assert(boxes.backgroundsMatch, 'MVP and matchbar must use the same neutral HUD-blue background');
    assert(boxes.nameFitsVertically, 'MVP names must retain their full height, including descenders such as g');
    assert.equal(await hud.locator('.round-message').count(), 0, 'the separate CT/T winner banner has been removed');
    for (const overlay of [boxes.history].filter(Boolean))
      assert(overlay.top >= boxes.mvp.bottom, 'round overlays must not overlap the MVP card');
  };
  await checkMvpLayout();
  assert.match(await hud.locator('.mvp-round').textContent(), /ROUND 10 MVP/);
  assert.equal(await hud.locator('.mvp-stats b').nth(0).textContent(), '2');
  assert.equal(await hud.locator('.mvp-stats b').nth(1).textContent(), '200');
  await page.screenshot({ path: path.join(scratch, 'mvp-1920x1080.png') });
  const barCrop = await hud.locator('.scoreboard').boundingBox();
  const mvpCrop = await hud.locator('.round-mvp').boundingBox();
  await page.screenshot({ path: path.join(scratch, 'matchbar-mvp.png'),
    clip: { x: barCrop.x, y: barCrop.y, width: barCrop.width, height: mvpCrop.y + mvpCrop.height - barCrop.y } });
  await page.setViewportSize({ width: 1600, height: 900 });
  await checkMvpLayout();
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.waitForTimeout(Math.max(0, 2500 - (Date.now() - mvpShownAt)));
  await command({ kind: 'step' });
  await hud.locator('.clock-label').filter({ hasText: 'PAUSED' }).waitFor();
  await page.waitForTimeout(Math.max(0, 6000 - (Date.now() - mvpShownAt)));
  await checkMvpLayout();
  assert.equal(await hud.locator('.round-mvp').count(), 1, 'MVP must remain visible beyond five seconds before its eight-second deadline');
  assert.equal(await hud.locator('.mvp-stats b').nth(1).textContent(), '200', 'paused round-over must retain completed-round damage');
  await command({ kind: 'step' });
  await hud.locator('.clock-label').filter({ hasText: 'ROUND 11' }).waitFor();
  await hud.waitForFunction(() => !document.querySelector('.round-mvp'));
  assert.equal(await hud.locator('.round-kill-indicator').count(), 0, 'next freeze clears all playercard kill stacks even before source counters reset');
  assert(Date.now() - mvpShownAt < 7800, 'freeze must end the MVP display before the eight-second deadline');
  assert(await hud.evaluate(() => window.mvpMotionSamples.some(sample => sample.visible === 'false' && sample.progress > 0 && sample.progress < 1)),
    'freeze must animate the MVP exit before removing its element');
  await command({ kind: 'step' });
  await hud.locator('.clock-label').filter({ hasText: 'PAUSED' }).waitFor();
  assert.equal(await hud.locator('.round-mvp').count(), 0, 'paused freeze updates must not revive the MVP');
  await command({ kind: 'step' });
  await hud.locator('.clock-label').filter({ hasText: 'ROUND 11' }).waitFor();
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await hud.evaluate(() => { window.mvpMotionSamples = []; });
  await command({ kind: 'step' });
  await hud.locator('.round-mvp').waitFor();
  const finalMvpShownAt = Date.now();
  await hud.waitForFunction(() => document.querySelector('.round-mvp')?.style.getPropertyValue('--mvp-progress') === '1.00000');
  assert(!(await hud.evaluate(() => window.mvpMotionSamples.some(sample => sample.progress > 0 && sample.progress < 1))),
    'reduced motion must reveal the MVP immediately without intermediate movement');
  assert.match(await hud.locator('.mvp-round').textContent(), /ROUND 11 MVP/);
  await page.waitForTimeout(2500);
  await command({ kind: 'step' });
  await until(async () => await hud.locator('.mvp-stats b').nth(1).textContent() === '112');
  await page.waitForTimeout(Math.max(0, 7300 - (Date.now() - finalMvpShownAt)));
  assert.equal(await hud.locator('.round-mvp').count(), 1, 'a different round MVP gets its own eight-second display');
  await until(async () => await hud.locator('.round-mvp').count() === 0, 2000);
  assert(Date.now() - finalMvpShownAt >= 7800 && Date.now() - finalMvpShownAt < 9400,
    'match-end MVP must expire eight seconds after first display without extending its deadline on source updates');
  await command({ kind: 'step' });
  await page.waitForTimeout(150);
  assert.equal(await hud.locator('.round-mvp').count(), 0, 'an update of an expired award must not revive it');
  await hud.evaluate(() => { window.mvpMotionObserver.disconnect(); delete window.mvpMotionObserver; delete window.mvpMotionSamples; });
  await page.emulateMedia({ reducedMotion: 'no-preference' });
  console.log('PASS MVP Motion reveal/exit, reduced motion, timing and geometry, eight-second expiry, early freeze dismissal and no revival');
  for (const [file, label, color, width, height] of [ ['league', 'LEAGUE', '#b6def5', 120, 120], ['sponsor-a', 'DEMO A', '#ffffff', 300, 120],
    ['sponsor-b', 'DEMO B', '#eee1a0', 240, 120], ['sponsor-c', 'DEMO C', '#ffffff', 180, 120] ]) {
    const data = await page.evaluate(({ label, color, width, height }) => {
      const canvas = document.createElement('canvas'); canvas.width = width; canvas.height = height;
      const context = canvas.getContext('2d'); context.fillStyle = color; context.font = `bold ${Math.min(38, Math.floor(width / (label.length * .7)))}px Arial`;
      context.textAlign = 'center'; context.textBaseline = 'middle'; context.fillText(label, width / 2, height / 2);
      return canvas.toDataURL('image/png').split(',')[1];
    }, { label, color, width, height });
    await page.route(`**/cast-media/${file}.png`, route => route.fulfill({ contentType: 'image/png', body: Buffer.from(data, 'base64') }));
  }
  await command({ kind: 'step' });
  await hud.locator('.live-competition-logo').waitFor();
  // SDK preview.2's command endpoint rejects Images arrays. The actual panel-array
  // combination is covered by Razor tests; use managed sponsors here for browser geometry.
  await until(async () => await hud.locator('.live-branding .sponsor-image').count() === 1);
  const activeSponsor = () => hud.locator('.live-branding .sponsor-image').getAttribute('src');
  await until(async () => (await activeSponsor()).endsWith('/cast-media/sponsor-a.png'));
  const rotationStartedAt = Date.now();
  const competitionLogo = await hud.locator('.live-competition-logo').getAttribute('src');
  const killNames = async () => Promise.all(['killer', 'assister', 'victim'].map(role => hud.locator(`[data-kill-role="${role}"]`).textContent()));
  assert.deepEqual(await killNames(), ['Preview CT 1', 'Preview CT 2', 'Preview T 1']);
  const killerId = baseline.game.players[0].steamId;
  const assistId = baseline.game.players[1].steamId;
  const victimId = baseline.game.players[5].steamId;
  const profileOptions = {
    players: { [killerId]: { name: 'Managed killer &amp;' }, [assistId]: { name: 'Managed assist' }, [victimId]: { name: 'Managed victim' } },
    match: { left: { id: 'ct-team', steamIds: [killerId, assistId] }, right: { id: 't-team', steamIds: [victimId] } }
  };
  await command({ kind: 'options', options: profileOptions });
  await until(async () => JSON.stringify(await killNames()) === JSON.stringify(['Managed killer &amp;', 'Managed assist', 'Managed victim']));
  assert.equal(await hud.locator('[data-kill-role="killer"]').getAttribute('title'), 'Managed killer &amp;');
  assert.equal(await hud.locator('.live-player .player-name').first().textContent(), 'Managed killer &amp;', 'killfeed and player cards must use the same profile name');
  await command({ kind: 'options', options: { ...profileOptions, players: { ...profileOptions.players, [killerId]: { name: 'Renamed killer' } } } });
  await until(async () => (await killNames())[0] === 'Renamed killer');
  await command({ kind: 'options', options: { ...profileOptions, match: { ...profileOptions.match, right: { id: 'unrelated-team', steamIds: [victimId] } } } });
  await until(async () => JSON.stringify(await killNames()) === JSON.stringify(['Preview CT 1', 'Preview CT 2', 'Preview T 1']));
  console.log('PASS killfeed uses managed names for killer, assist and victim, updates existing entries and rejects mismatched profile teams');
  await hud.waitForFunction(() => [...document.querySelectorAll('.live-branding img')].every(image => image.complete && image.naturalWidth > 0));
  assert.equal(await hud.locator('.observed .sponsors').count(), 0, 'observer must not duplicate sponsors');
  const checkBranding = async () => {
    await hud.waitForFunction(() => [...document.querySelectorAll('.live-branding img')].every(image => image.complete && image.naturalWidth > 0));
    const geometry = await hud.evaluate(() => {
      const brand = document.querySelector('.live-branding').getBoundingClientRect();
      const feed = document.querySelector('.killfeed').getBoundingClientRect();
      const header = document.querySelector('.scoreboard').getBoundingClientRect();
      const logo = document.querySelector('.live-competition-logo').getBoundingClientRect();
      const sponsor = document.querySelector('.live-branding .sponsor-image').getBoundingClientRect();
      return { above: brand.bottom <= feed.top, aligned: Math.abs(brand.right - feed.right) < .1, clearsHeader: brand.left >= header.right,
        order: sponsor.right < logo.left && Math.abs(logo.right - brand.right) < .1,
        fits: [...document.querySelectorAll('.live-branding img')].every(image => {
          const rect = image.getBoundingClientRect(); return rect.left >= brand.left && rect.right <= brand.right && rect.top >= brand.top && rect.bottom <= brand.bottom;
        }) };
    });
    assert(geometry.above && geometry.aligned && geometry.fits && geometry.clearsHeader && geometry.order, 'competition anchors the right edge above the killfeed, with sponsors directly to its left');
  };
  const checkLogoSizes = async (competitionPercent, sponsorPercent) => {
    await checkBranding();
    const size = await hud.evaluate(() => {
      const rect = selector => { const box = document.querySelector(selector).getBoundingClientRect(); return { width: box.width, height: box.height }; };
      const ratio = selector => { const image = document.querySelector(selector); return image.naturalWidth / image.naturalHeight; };
      return { area: rect('.live-branding'), hud: rect('.hud'), competition: rect('.live-competition-logo'), sponsor: rect('.live-branding .sponsor-image'), competitionRatio: ratio('.live-competition-logo'), sponsorRatio: ratio('.live-branding .sponsor-image') };
    });
    assert(Math.abs(size.competition.width - size.competition.height * size.competitionRatio) < 0.1, 'competition width must follow the actual image aspect ratio');
    assert(Math.abs(size.sponsor.width - size.sponsor.height * size.sponsorRatio) < 0.1, 'sponsor width must follow the currently rotated image aspect ratio');
    assert(Math.abs(size.competition.height - size.area.height * competitionPercent / 100) < 0.1, 'competition height must use the available branding height');
    assert(Math.abs(size.sponsor.height - size.area.height * sponsorPercent / 100) < 0.1, 'sponsor height must use the same available branding height');
    assert(Math.abs(size.area.width - size.competition.width - size.sponsor.width - size.hud.width * .008) < .2,
      'branding width must contain only the image widths and their gap, without reserved logo slots');
    await checkBranding();
  };
  await checkBranding();
  await checkLogoSizes(100, 100);
  await page.screenshot({ path: path.join(scratch, 'live-branding-1920x1080.png') });
  await page.setViewportSize({ width: 1600, height: 900 }); await checkBranding();
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.waitForTimeout(Math.max(0, 2500 - (Date.now() - rotationStartedAt)));
  await command({ kind: 'panel', values: { display: { hideSponsors: false } } });
  await page.waitForTimeout(Math.max(0, 5300 - (Date.now() - rotationStartedAt)));
  assert((await activeSponsor()).endsWith('/cast-media/sponsor-a.png'), 'default sponsor duration must exceed the old five seconds');
  await until(async () => (await activeSponsor()).endsWith('/cast-media/sponsor-b.png'), 6000);
  assert(Date.now() - rotationStartedAt >= 9500 && Date.now() - rotationStartedAt < 11500,
    'the default rotation is ten seconds and unchanged source updates must not restart it');
  const customIntervalAt = Date.now();
  await command({ kind: 'panel', values: { branding: { sponsorIntervalSeconds: '1' } } });
  assert((await activeSponsor()).endsWith('/cast-media/sponsor-b.png'), 'changing duration must retain the current sponsor');
  for (const file of ['sponsor-c', 'sponsor-a']) {
    await until(async () => (await activeSponsor()).endsWith(`/cast-media/${file}.png`), 2500);
    assert.equal(await hud.locator('.live-branding .sponsor-image').count(), 1, 'only one sponsor may be visible');
    assert.equal(await hud.locator('.live-competition-logo').getAttribute('src'), competitionLogo, 'competition logo must stay fixed');
    await checkBranding();
  }
  assert(Date.now() - customIntervalAt < 4000, 'a new panel duration must take effect immediately');
  const sizeLogos = async (competition, sponsor) => {
    await command({ kind: 'panel', values: { branding: { sponsorIntervalSeconds: '30', competitionLogoHeightPercent: competition, sponsorHeightPercent: sponsor } } });
    const expectedCompetition = competition === '' ? '1' : String(Number(competition.replace(',', '.').replace('%', '')) / 100);
    await until(async () => await hud.locator('.live-branding').evaluate((element, expected) =>
      element.style.getPropertyValue('--competition-logo-scale') === expected, expectedCompetition));
  };
  await sizeLogos('50%', '75');
  await checkLogoSizes(50, 75);
  await page.screenshot({ path: path.join(scratch, 'branding-sized-1920x1080.png') });
  await sizeLogos('80', '25,5');
  await checkLogoSizes(80, 25.5);
  await page.setViewportSize({ width: 1600, height: 900 }); await checkLogoSizes(80, 25.5);
  await page.setViewportSize({ width: 1920, height: 1080 });
  await sizeLogos('0', '100'); await checkLogoSizes(0, 100);
  await sizeLogos('', ''); await checkLogoSizes(100, 100);
  await command({ kind: 'panel', values: { display: { hideSponsors: true } } });
  await hud.waitForFunction(() => !document.querySelector('.live-branding .sponsors'));
  assert.equal(await hud.locator('.live-competition-logo').count(), 1, 'Hide sponsors must retain the competition logo');
  await command({ kind: 'panel', values: { display: { hideSponsors: false, hideObservedPlayer: true } } });
  await hud.waitForFunction(() => !document.querySelector('.observed') && document.querySelectorAll('.live-branding .sponsor-image').length > 0);
  await checkBranding();
  await command({ kind: 'panel', values: { display: { hideObservedPlayer: false } } });
  await hud.locator('.observed').waitFor();
  console.log('PASS sponsor rotation, live duration, intrinsic logo widths, right-side competition placement and visibility');
  for (const view of manifest.subviews) {
    await command({ kind: 'view', route: view.route });
    await hud.waitForURL(new URL(view.route, output).href);
    await hud.locator('.presentation').waitFor();
  }
  await command({ kind: 'view', route: '' });
  await hud.waitForURL(output);
  await hud.locator('.roster .live-player').first().waitFor();
  console.log('PASS all declared manager views and return to live');
  const schedule = () => hud.locator('.scheduled-start, .break-clock');
  const scheduleTimes = () => schedule().locator('time').evaluateAll(times => times.map(time => Date.parse(time.dateTime)));
  const checkSchedule = async (delayed, times, message) => {
    await until(async () => JSON.stringify(await scheduleTimes()) === JSON.stringify(times.map(time => Date.parse(time))));
    assert.equal(await schedule().locator(':scope > span').textContent(), delayed ? 'DELAYED START' : 'PLANNED START');
    assert.equal(await schedule().locator('.original-start').count(), delayed && times.length === 2 ? 1 : 0);
    const dates = await schedule().locator('time').evaluateAll(nodes => nodes.map(node => {
      const parts = Object.fromEntries(new Intl.DateTimeFormat('en-GB', {
        year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
      }).formatToParts(new Date(node.dateTime)).map(part => [part.type, part.value]));
      return { actual: node.textContent, expected: `${parts.year}-${parts.month}-${parts.day} ${parts.hour}:${parts.minute}` };
    }));
    assert(dates.every(date => date.actual === date.expected), `start dates must use browser-local time without a timezone suffix: ${JSON.stringify(dates)}`);
    const countdown = await schedule().locator('strong').textContent();
    if (message) assert.equal(countdown, message);
    else assert(/^\d+:\d{2}$/.test(countdown), 'effective future start has a countdown');
    return countdown;
  };
  await command({ kind: 'view', route: 'views/prematch' });
  await hud.waitForURL(new URL('views/prematch', output).href);
  await command({ kind: 'step' });
  await hud.waitForFunction(() => !document.querySelector('.scheduled-start, .break-clock'));
  await command({ kind: 'step' });
  const firstStartCountdown = await checkSchedule(true, [delayedStartTime, plannedStartTime]);
  await until(async () => await schedule().locator('strong').textContent() !== firstStartCountdown, 3000);
  await page.screenshot({ path: path.join(scratch, 'delayed-start-prematch-1920x1080.png') });
  await command({ kind: 'view', route: 'views/break' });
  await hud.waitForURL(new URL('views/break', output).href);
  await checkSchedule(true, [delayedStartTime, plannedStartTime]);
  await command({ kind: 'step' });
  await checkSchedule(true, [laterStartTime, plannedStartTime]);
  await page.screenshot({ path: path.join(scratch, 'delayed-start-break-1920x1080.png') });
  await command({ kind: 'step' });
  await hud.waitForFunction(() => !document.querySelector('.scheduled-start, .break-clock'));
  await command({ kind: 'step' });
  await checkSchedule(true, [laterStartTime]);
  await command({ kind: 'step' });
  await checkSchedule(false, [], 'Start time not set');
  await command({ kind: 'step' });
  await command({ kind: 'view', route: '' });
  await hud.waitForURL(output);
  await hud.locator('.roster .live-player').first().waitFor();
  console.log('PASS prematch/break use and label delayed starts before GSI, tick on SDK time, and handle changes, removal and unknown dates');
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
