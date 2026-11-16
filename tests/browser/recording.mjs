import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { spawn } from 'node:child_process';
import { mkdtemp, readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const sdkTool = path.resolve("artifacts/published-sdk-tools/.store/sou.hud.tool/0.3.0-preview.9/sou.hud.tool/0.3.0-preview.9/tools/net10.0/any/SOU.Hud.Tool.dll");
const packagePath = path.resolve(process.argv[2] ?? 'artifacts/recording-push-2026-11-10/package');
const source = JSON.parse(await readFile('tests/fixtures/recording-showcase.json', 'utf8'));
const fixture = source.recordingSnapshot;
await mkdir('artifacts', { recursive: true });
const scratch = await mkdtemp(path.resolve('artifacts/recording-push-browser-'));
const manifest = JSON.parse(await readFile(path.join(packagePath, 'hud.json'), 'utf8'));
const previewFixture = structuredClone(source);
for (const snapshot of previewFixture.snapshots) for (const match of [snapshot.managedMatch, snapshot.game?.managedMatch].filter(Boolean)) {
    match.teamA.logo = '/cast-media/recording-alpha.svg'; match.teamB.logo = '/cast-media/recording-bravo.svg';
}
const fixturePath = path.join(scratch, 'fixture.json');
await writeFile(fixturePath, JSON.stringify(previewFixture));
const host = spawn('dotnet', [sdkTool, 'dev', packagePath, '--port', '0', '--fixture', fixturePath], { stdio: ['ignore', 'pipe', 'pipe'] });
let hostOutput = '', browser, origin, page; const extraHosts=[];
host.stdout.on('data', v => hostOutput += v); host.stderr.on('data', v => hostOutput += v);
const until = async fn => { const deadline = Date.now() + 30000; while (!await fn()) { if (Date.now() > deadline) throw new Error(`Timeout: ${hostOutput}`); await new Promise(r => setTimeout(r, 40)); } };
try {
    await until(() => /Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(hostOutput));
    origin = hostOutput.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
    browser = await chromium.launch({ headless: true, executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE });
    page = await browser.newPage();
    const errors = [], forbidden = [], recordingSockets = [], activeSockets = new Set();
    const command = async data => { const response = await page.request.post(new URL('__dev/command', origin).href, { headers: { 'X-SOU-DEV': '1' }, data }); assert(response.ok(), `${response.status()}: ${await response.text()}`); };
    await page.goto(origin); await command({ kind: 'pause' });
    let output = new URL(`hud-packages/${manifest.id}/${manifest.version}/`, origin).href;
    const hud = await browser.newPage({ viewport: { width: 1920, height: 1080 }, locale: 'de-DE' });
    hud.on('pageerror', e => errors.push(e.message));
    hud.on('request', request => { if (/\/hud-browser\/(round-history|map-analysis|economy-history|recording-snapshot)/.test(request.url())) forbidden.push(request.url()); });
    hud.on('websocket', ws => { if (!ws.url().includes('/hud-live/recordings')) return; recordingSockets.push(ws); ws.sent=[]; ws.on("framesent", event=>ws.sent.push(String(event.payload))); activeSockets.add(ws); ws.on('close', () => activeSockets.delete(ws)); });
    await hud.route('**/cast-media/recording-*.svg', route => route.fulfill({ contentType: 'image/svg+xml', body: `<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80"><path fill="${route.request().url().includes('alpha') ? '#B6DEF5' : '#EEE1A0'}" d="M40 2 76 22 68 62 40 78 12 62 4 22Z"/><path fill="#11244a" d="m40 15 20 45H20Z"/></svg>` }));
    const visit = async route => { await hud.goto(output + 'views/' + route); await hud.locator('.recording-scene').waitFor(); await hud.waitForFunction(() => { const s=document.querySelector('.recording-scene')?.dataset.state; return s && !['loading','waiting'].includes(s); }); };
    const expectText = async text => assert((await hud.locator('.recording-scene').innerText()).includes(text), text);
    const panelValues = {};
    const panel = async (section, values) => { panelValues[section] = { ...panelValues[section], ...values }; await command({ kind: 'panel', values: { [section]: panelValues[section] } }); };
    let revision = 0;
    const push = async value => {
        const next = structuredClone(value);
        // Keep complete synthetic states under the simulator command body's limit.
        // These scenarios intentionally omit first-freeze/live checkpoints.
        for (const r of next.roundHistory.rounds) r.checkpoints = r.checkpoints.filter(c=>['freezeLast','roundEnd'].includes(c.kind));
        for (const key of ["roundHistory","mapAnalysis","economyHistory"]) next[key].revision = createHash("sha256").update(JSON.stringify(next[key])).digest("hex");
        next.revision = createHash('sha256').update(JSON.stringify([next, ++revision])).digest('hex');
        await writeFile(path.join(scratch, "last-push.json"), JSON.stringify({ ...source, recordingSnapshot: next }));
        await command({ kind: 'recording', recordingSnapshot: next });
    };
    const fit = async () => {
        const result = await hud.locator('.recording-scene').evaluate(scene => {
            const box = scene.getBoundingClientRect();
            const bad = [...scene.querySelectorAll('.recording-stage, .recording-view, .cast-footnote, .compare-player, .side-board-row, .buy-player, .evidence-event, .evidence-player')].filter(el => {
                const r = el.getBoundingClientRect(); return r.bottom > box.bottom + 1 || r.right > box.right + 1 || r.left < box.left - 1 || r.top < box.top - 1 || r.height < 0;
            }).map(el => el.className);
            // The existing weapon system intentionally overscans transparent SVG image bounds.
            // Measure visible layout frames, not scrollHeight of the transparent image source.
            const clipped = [...scene.querySelectorAll('.compare-player, .side-board-row, .multikill-player, .buy-player, .evidence-event, .evidence-player')].flatMap(el => {
                const r = el.getBoundingClientRect();
                return [...el.querySelectorAll('.recorded-identity, .recorded-inventory, .loadout-numbers, .sample-values, .sample-coverage, .refund-evidence')].filter(c => {
                    const b = c.getBoundingClientRect(); return b.bottom > r.bottom + 1 || b.top < r.top - 1 || b.left < r.left - 1 || b.right > r.right + 1;
                }).map(c => ({ row: el.className, content: c.className }));
            });
            const collisions = [...scene.querySelectorAll('.compare-player')].filter(row => {
                const identity = row.querySelector('.compare-identity').getBoundingClientRect();
                const deltas = row.querySelector('.compare-deltas').getBoundingClientRect();
                return [...row.querySelectorAll('.loadout-numbers, .loadout-armor, .recorded-inventory')].some(part => {
                    const b = part.getBoundingClientRect(); return b.top < identity.bottom - 1 || b.bottom > deltas.top + 1;
                });
            }).length;
            return { collisions, clipped, controls: scene.querySelectorAll('button, select, input, a, textarea, details').length, overflow: scene.scrollWidth > scene.clientWidth + 1 || scene.scrollHeight > scene.clientHeight + 1, bad };
        });
        assert.equal(await hud.locator('.recording-scene').getAttribute('lang'), 'en', 'output stays English even with a German browser');
        assert.equal(await hud.locator('.evidence-event .evidence').count(), 0, 'native event receipt acknowledgements are not viewer content');
        assert.equal(result.collisions, 0, 'loadout details must not overlap player names or deltas');
        assert.equal(result.controls, 0, 'the output must contain no interactive controls');
        assert.equal(result.overflow, false, 'the broadcast must fit without scrolling');
        assert.deepEqual(result.bad, [], 'all player rows and footnotes must fit the broadcast');
        assert.deepEqual(result.clipped, [], 'player content must fit its allocated visual row');
        const text = await hud.locator('.recording-scene').innerText();
        assert(!/TEAMZUORDNUNG|HISTORISCHE|SPIELER|BEOBACHTETE|BEISPIELDATEN|RUNDENENDE|AUSRÜSTUNG|LETZTER|KEINE|BESTÄTIGT|ABGELEITET|UNBEKANNT|full ownership|purchase prices|one class per round|sample kills \/ kills|example-raw-|example-analysis-|example-economy-|equipment-v1|force-observed-v1|76561198|2026-11-10T|Aktive Waffenidentität:/.test(text), 'no IDs, revisions, API rules or raw timestamps in output');
    };
    const views = ['team-economy', 'round-compare', 'side-statistics', 'multikills', 'round-buys', 'economy-evidence'];
    const sections = ['recordingEconomy', 'recordingCompare', 'recordingSides', 'recordingMultikills', 'recordingBuys', 'recordingEvidence'];
    for (let i = 0; i < views.length; i++) {
        await visit(views[i]); await expectText('EXAMPLE DATA');
        assert.equal(await hud.locator('.recording-scene').getAttribute('data-state'), 'available');
        assert.equal(await hud.locator('.recorded-name b').count(), 0, 'names render only as text');
        await fit(); await hud.screenshot({ path: path.join(scratch, `${views[i]}-1920.png`) });
        await hud.setViewportSize({ width: 390, height: 844 });
        await fit(); await hud.screenshot({ path: path.join(scratch, `${views[i]}-390.png`) });
        await hud.setViewportSize({ width: 1920, height: 1080 });
        await panel(sections[i], { title: 'PANEL ' + views[i] });
        await hud.locator('h1').filter({ hasText: 'PANEL ' + views[i] }).waitFor();
        await panel(sections[i], { title: '' });
    }
    console.log('PASS six passive 16:9 broadcasts, no controls or scrolling at both widths, panel title settings, text-only names');

    // Panel-driven view changes share one SDK subscription and do not reload data.
    await panel('display', { scene: 'team-economy' }); await hud.goto(output);
    await hud.locator('.economy-view').waitFor(); const connections = recordingSockets.length;
    for (const view of views) { await panel('display', { scene: view }); await hud.locator(`.recording-scene[data-view="${view}"]`).waitFor(); }
    assert.equal(recordingSockets.length, connections, 'related views share their subscription');
    await panel('display', { scene: 'live' }); await hud.locator('.roster').first().waitFor(); await until(()=>recordingSockets.at(-1).isClosed() || recordingSockets.at(-1).sent.some(frame=>frame.includes('"type":5')));
    console.log('PASS shared recording lifetime across all six scenes and cancellation on leaving');

    await visit('economy-evidence'); await panel('recordingEvidence', { round: '2' });
    await hud.locator('.evidence-movements').waitFor(); await expectText('OBS. CASH MOVEMENTS');
    assert((await hud.locator('.evidence-movements').textContent()).includes('$1,200'));
    await fit(); await hud.screenshot({path:path.join(scratch,'sampled-cash-movements.png')});
    await panel('recordingEvidence', { round: '1' });
    await hud.locator('.cast-caption').filter({hasText:'ROUND 1'}).waitFor();
    const late = structuredClone(fixture), round = late.economyHistory.rounds[0], player = round.players[0];
    const event = structuredClone(round.events.find(e => e.kind === 'purchase'));
    event.id = createHash('sha256').update('late-purchase').digest('hex'); event.receivedAt = '2026-11-10T09:00:00Z'; event.gameTimeSeconds += 20;
    round.events.push(event); player.observedPurchases++; player.purchaseEvidence = 'confirmed';
    await push(late); await until(async () => await hud.locator('.event-board header > span').textContent() === String(round.events.length));
    await expectText('ROUND 1');
    const replaced = structuredClone(fixture);
    replaced.economyHistory.rounds[0].events = [];
    for (const p of replaced.economyHistory.rounds[0].players) { p.observedPurchases=0; p.observedPickups=0; p.observedRemovals=0; p.purchaseEvidence='unknown'; p.refundEvidence='unknown'; }
    for (const t of replaced.economyHistory.rounds[0].teams) { t.classification='unknown'; t.evidence='unknown'; t.observedBuyers=0; }
    await push(replaced); await until(async () => await hud.locator('.evidence-event').count() === 0); await expectText('NO OBSERVED EVENTS');
    console.log('PASS simulator late event updates an earlier round, and full round replacement removes old events and lower counts');

    await visit('team-economy'); await panel('recordingEconomy', { metric: 'equipment', round: '2', phase: 'freezeLast' });
    const lower = structuredClone(fixture);
    for (const c of lower.roundHistory.rounds[1].checkpoints) for (const p of c.players) p.equipmentValue=100;
    await push(lower); await until(async () => (await hud.locator('.economy-headline strong').first().textContent()) === '$200');
    const pending = structuredClone(fixture);
    pending.roundHistory.rounds[5].checkpoints = pending.roundHistory.rounds[5].checkpoints.filter(c => ['freezeStart','freezeLast'].includes(c.kind));
    await panel('recordingEconomy', { round: '', phase:'freezeLast' }); await push(pending);
    await hud.locator('.cast-caption').filter({ hasText: 'ROUND 5' }).waitFor();
    assert.equal(await hud.locator('.chart-point[data-round="6"]').count(),0);
    await panel('recordingEconomy', { round:'6' });
    assert.equal(await hud.locator('.chart-point[data-round="6"]').count(),0);
    await push(fixture); await until(async () => await hud.locator('.chart-point[data-round="6"]').count() === 2);
    await panel('recordingEconomy', { round:'3' });
    assert.equal(await hud.locator('.economy-headline strong').first().textContent(), '\u2014', 'unknown checkpoint values stay gaps');
    console.log('PASS lower graph values replace earlier values, incomplete freeze stays hidden, and unknown values stay gaps');

    await visit('side-statistics'); const totals = await hud.locator('.sample-values').allTextContents();
    await push(fixture); await push(fixture);
    assert.deepEqual(await hud.locator('.sample-values').allTextContents(),totals,'map summaries replace, never accumulate');
    await visit('economy-evidence'); await panel('recordingEvidence',{round:'1'});
    await until(async()=>await hud.locator('.force-assessment.ct .recorded-team-name').textContent()==='Example Alpha');
    await panel('recordingEvidence',{round:'4'});
    await until(async () => await hud.locator('.force-assessment.ct .recorded-team-name').textContent() === 'Example Bravo');
    await until(async () => await hud.locator('.recorded-team-logo').evaluateAll(images=>images.length===2&&images.every(i=>i.complete&&i.naturalWidth>0)));
    await hud.screenshot({path:path.join(scratch,'historical-team-identities.png')});
    console.log('PASS map summaries are not added twice and team names/logos follow halftime');

    for (const count of [10,32]) {
        const crowd = structuredClone(fixture);
        for(const part of [crowd.roundHistory,crowd.mapAnalysis,crowd.economyHistory]) part.rounds=part.rounds.slice(0,1);
        const expand = rows => Array.from({length:count},(_,i)=>{const p=structuredClone(rows[i%rows.length]);p.steamId=String(76561198000001000n+BigInt(i));p.name='Example '+i+' '+(i%4===0?'Alexandra With A Very Long Player Name':'Player');return p;});
        for(const r of crowd.roundHistory.rounds)for(const c of r.checkpoints)c.players=expand(c.players);
        for(const r of crowd.mapAnalysis.rounds) {r.players=expand(r.players); for(const t of r.teams){ t.buyPlayers=r.players.filter(p=>p.side===t.side).length;t.endPlayers=t.buyPlayers; }}
        crowd.mapAnalysis.players=expand(crowd.mapAnalysis.players.filter(p=>p.side==='counterTerrorists'));
        for(const p of crowd.mapAnalysis.players)Object.assign(p,{rounds:1,killRounds:1,kills:0,headshotRounds:1,damageRounds:1,headshotSampleKills:0,headshotKills:0,damage:0,headshotPercentage:null,averageDamagePerRound:0,coversObservedRounds:true,doubleKills:0,tripleKills:0,quadKills:0,fivePlusKills:0});
        for(const r of crowd.mapAnalysis.rounds)for(const p of r.players)Object.assign(p,{kills:0,headshotSampleKills:0,headshotKills:0,damage:0});
        for(const r of crowd.economyHistory.rounds){r.players=expand(r.players);r.events=[];for(const p of r.players){p.observedPurchases=r.source?0:null;p.observedPickups=r.source?0:null;p.observedRemovals=r.source?0:null;p.purchaseEvidence='unknown';p.refundEvidence='unknown';}for(const t of r.teams){t.players=r.players.filter(p=>p.side===t.side).length;t.observedBuyers=0;t.classification='unknown';t.evidence='unknown';}}
        const baseOrigin=origin, baseOutput=output;
        const crowdedFixture=structuredClone(previewFixture);crowdedFixture.recordingSnapshot=crowd;
        const crowdedPath=path.join(scratch,`crowd-${count}.json`);await writeFile(crowdedPath,JSON.stringify(crowdedFixture));
        const extra=spawn('dotnet',[sdkTool,'dev',packagePath,'--port','0','--fixture',crowdedPath],{stdio:['ignore','pipe','pipe']});extraHosts.push(extra);
        let extraOutput='';extra.stdout.on('data',v=>extraOutput+=v);extra.stderr.on('data',v=>extraOutput+=v);
        await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(extraOutput));
        origin=extraOutput.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];output=new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href;
        await page.goto(origin); await command({kind:'pause'});
        const selectors=['.compare-player','.side-board-row','.multikill-player','.buy-player','.evidence-player'];
        for(let i=1;i<views.length;i++) {
            if(views[i]==='round-compare')await panel(sections[i],{roundA:'1',roundB:'1'});
            if(['round-buys','economy-evidence'].includes(views[i]))await panel(sections[i],{round:'1'});
            await visit(views[i]);
            assert.equal((await hud.locator(selectors[i-1]).evaluateAll(rows=>[...new Set(rows.map(r=>r.dataset.player))])).length,count);
            if(views[i]==='multikills') assert.equal(await hud.locator('.multikill-view .ct, .multikill-view .t').count(),0);
            await fit();await hud.screenshot({path:path.join(scratch,`${views[i]}-players${count}.png`)});
        }
        await command({kind:'stop'});if(extra.exitCode===null)extra.kill();origin=baseOrigin;output=baseOutput;await page.goto(origin);
    }
    await push(fixture);
    for(const view of views.filter(v=>v!=='team-economy')){ await visit(view);await until(async()=>await hud.locator('img.recorded-avatar').evaluateAll(images=>images.length>=4&&images.every(i=>i.complete&&i.naturalWidth>0))); }
    console.log('PASS all 10/32 players fit, agent fallback images load, and multikills remain combined');
    const names=structuredClone(fixture), steam=names.roundHistory.rounds[0].checkpoints[0].players[0].steamId;
    for(const r of names.roundHistory.rounds)for(const c of r.checkpoints)for(const p of c.players)if(p.steamId===steam)p.name='Wank&apos;ePEW &amp; Co &#x1F680;';
    for(const r of names.mapAnalysis.rounds)for(const p of r.players)if(p.steamId===steam)p.name='Wank&apos;ePEW &amp; Co &#x1F680;';
    for(const p of names.mapAnalysis.players)if(p.steamId===steam)p.name='Wank&apos;ePEW &amp; Co &#x1F680;';
    for(const r of names.economyHistory.rounds)for(const p of r.players)if(p.steamId===steam)p.name='Wank&apos;ePEW &amp; Co &#x1F680;';
    await push(names);
    for(const view of views.filter(v=>v!=='team-economy')){await visit(view);await expectText("Wank'ePEW & Co \u{1F680}");assert.equal(await hud.locator('.recorded-name b').count(),0);}
    await push(fixture);

    await visit('economy-evidence'); await panel('recordingEvidence',{round:'1'});
    const portraitOptions={players:{[steam]:{avatar:'/cast-media/recording-player.svg'}},match:{left:{id:'example-alpha',steamIds:[steam]},right:{id:'example-bravo',steamIds:[]}}};
    await command({kind:'options',options:portraitOptions});const portrait=hud.locator(`[data-player="${steam}"] img.recorded-avatar`).first();
    await until(async()=>(await portrait.getAttribute('src')).endsWith('/cast-media/recording-player.svg'));
    await panel('display',{agentsOnly:true});await until(async()=>(await portrait.getAttribute('src')).includes('assets/agents/'));
    await panel('display',{agentsOnly:false});await until(async()=>(await portrait.getAttribute('src')).endsWith('/cast-media/recording-player.svg'));
    await command({kind:'options',options:{}});
    // Delay the recording WebSocket handshake, while allowing live/session recovery.
    await hud.routeWebSocket('**/hud-live/recordings**', async socket=>{ await new Promise(r=>setTimeout(r,1800));socket.connectToServer(); });
    const reconnectCount=recordingSockets.length;
    await command({kind:'reconnect'});
    await hud.locator('.recording-scene[data-stale="true"]').waitFor();await expectText('STALE');assert(await hud.locator('.evidence-event').count()>0);
    await hud.screenshot({path:path.join(scratch,'reconnecting-stale.png')});
    await hud.locator('.recording-scene[data-stale="false"][data-state="available"]').waitFor({timeout:30000});
    assert(recordingSockets.length>reconnectCount, "SDK establishes a fresh recording transport after reconnect");
    assert(!recordingSockets.at(-1).isClosed());
    console.log('PASS SDK reconnect retains stale data until a complete fresh baseline, without extra active subscriptions');
    const empty=structuredClone(fixture);empty.status='notRecorded';empty.revision=null;empty.updatedAt=null;
    for(const key of ['roundHistory','mapAnalysis','economyHistory']){empty[key].status='notRecorded';empty[key].revision=null;empty[key].updatedAt=null;empty[key].rounds=[];empty[key].gaps=[];}
    empty.mapAnalysis.players=[];empty.mapAnalysis.buyRuleVersion=null;
    // Non-available snapshots must keep their revision null.
    await command({kind:'recording',recordingSnapshot:empty});await hud.locator('.recording-empty').waitFor();await expectText('NO RECORDING');
    await push(fixture); await hud.locator('.evidence-event').first().waitFor();
    await command({kind:'step'});await hud.locator('.recording-map strong').filter({hasText:'DUST2'}).waitFor();
    await hud.locator('.recording-empty').waitFor();assert.equal(await hud.locator('.evidence-event').count(),0);
    const wrong=structuredClone(fixture);await push(wrong);assert.equal(await hud.locator('.evidence-event').count(),0,'old map pushes cannot return into a new context');
    assert.deepEqual(forbidden,[]);assert.deepEqual(errors,[]);
    console.log('PASS no recording data HTTP requests, no polling, empty states and cancellation on match/map change');
    await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,views,widths:[1920,390],players:[10,32],sdkPushSimulator:true,forbidden,errors},null,2));console.log(`Evidence: ${scratch}`);
} finally {
    if(origin&&page)try{await page.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}
    await browser?.close();for(const extra of extraHosts)if(extra.exitCode===null)extra.kill();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),hostOutput);
}
