import {sdkToolPath} from './sdk-tool.mjs';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { mkdtemp, readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';
const tool=path.resolve(sdkToolPath);
const packagePath=path.resolve(process.argv[2]);const fixture=JSON.parse(await readFile('tests/fixtures/recording-showcase.json','utf8'));
const first=structuredClone(fixture.snapshots[0]),second=structuredClone(first),unknown=structuredClone(first);
second.managedMatch.ct.teamId='example-alpha';second.managedMatch.t.teamId='example-bravo';second.game.managedMatch=second.managedMatch;
second.game.players.forEach(p=>p.side=p.side==='counterTerrorists'?'terrorists':p.side==='terrorists'?'counterTerrorists':p.side);second.game.observedPlayer=second.game.players[0];
unknown.managedMatch.ct.status='unknown';unknown.managedMatch.t.status='unknown';unknown.game.managedMatch=unknown.managedMatch;
fixture.snapshots=[first,second,unknown];fixture.loop=false;
await mkdir('artifacts',{recursive:true});const scratch=await mkdtemp(path.resolve('artifacts/team-colors-browser-')),fixturePath=path.join(scratch,'fixture.json');await writeFile(fixturePath,JSON.stringify(fixture));
const manifest=JSON.parse(await readFile(path.join(packagePath,'hud.json'),'utf8'));
const host=spawn('dotnet',[tool,'dev',packagePath,'--port','0','--fixture',fixturePath],{stdio:['ignore','pipe','pipe']});let output='',browser,page,origin;
host.stdout.on('data',v=>output+=v);host.stderr.on('data',v=>output+=v);
const until=async fn=>{const end=Date.now()+30000;while(!await fn()){if(Date.now()>end)throw new Error('Timeout '+output);await new Promise(r=>setTimeout(r,30));}};
try{
 await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output));origin=output.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
 browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});page=await browser.newPage();await page.goto(origin);
 const command=async data=>{const response=await page.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data});assert(response.ok(),await response.text());};await command({kind:'pause'});
 const hud=await browser.newPage({viewport:{width:1920,height:1080}}),errors=[];let sockets=0;hud.on('pageerror',e=>errors.push(e.message));hud.on('websocket',w=>{if(w.url().includes('/hud-live/recordings'))sockets++;});
 const stem=new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href;
 await command({kind:'panel',values:{recordingEconomy:{round:'2'},recordingCompare:{roundA:'1',roundB:'2'},recordingBuys:{round:'1'},recordingEvidence:{round:'1'}}});
 const visit=async view=>{await hud.goto(stem+'views/'+view);await hud.locator('.broadcast-scene').waitFor();if(['team-economy','round-compare','round-buys','economy-evidence','multikills','side-statistics'].includes(view))await hud.locator('.recording-scene[data-state="available"]').waitFor();};
 const rgb=async(locator,property='color')=>locator.evaluate((e,p)=>getComputedStyle(e)[p],property);
 const steam=first.game.players[0].steamId,ct='rgb(182, 222, 245)',t='rgb(238, 225, 160)';
 const checkViews=async(alpha,beta)=>{
  for(const view of ['prematch','break','veto','postmatch']){await visit(view);await hud.locator('.duel-team h2').first().waitFor();assert.equal(await rgb(hud.locator('.duel-team h2').first()),alpha,view+' team A');assert.equal(await rgb(hud.locator('.duel-team h2').nth(1)),beta,view+' team B');}
  await visit('lineups');await hud.locator('.lineup-team .player-card').first().waitFor();assert.equal(await rgb(hud.locator('.lineup-team h2').first()),alpha);
  await visit('round-compare');assert.equal(await rgb(hud.locator(`[data-player="${steam}"]`),'borderLeftColor'),alpha);
  await visit('round-buys');assert.equal(await rgb(hud.locator('.buy-team').first(),'borderTopColor'),alpha);
  await visit('economy-evidence');assert.equal(await rgb(hud.locator('.force-assessment').first(),'borderLeftColor'),alpha);assert.equal(await rgb(hud.locator(`.evidence-player[data-player="${steam}"]`),'borderLeftColor'),alpha);
  await visit('multikills');assert.equal(await rgb(hud.locator(`[data-player="${steam}"]`),'borderLeftColor'),alpha);assert.equal(await hud.locator('.multikill-player .stat-card-side').count(),0);
  await visit('side-statistics');assert.equal(await rgb(hud.locator('.ct .stat-card-side').first()),ct);assert.equal(await rgb(hud.locator('.t .stat-card-side').first()),t);
  await visit('team-economy');assert.equal(await rgb(hud.locator('.economy-headline .team-name').first()),alpha);assert.equal(await rgb(hud.locator('.economy-headline .team-name').nth(1)),beta);
  assert.equal(await rgb(hud.locator('.chart-line.chart-team-0').first(),'stroke'),alpha);assert.equal(await rgb(hud.locator('.chart-point.chart-team-0').first(),'fill'),alpha);assert.equal(await rgb(hud.locator('.chart-coverage.chart-team-0').first(),'fill'),alpha);
 };
 // Use a historical pre-halftime round while the live teams already changed sides.
 await checkViews(t,ct);const geometry=()=>hud.locator('.chart-point').evaluateAll(points=>points.map(p=>({cx:p.getAttribute('cx'),cy:p.getAttribute('cy'),round:p.dataset.round})));
 const before=await geometry(),beforeSockets=sockets;await hud.screenshot({path:path.join(scratch,'current-bravo-ct.png')});
 await command({kind:'step'});await until(async()=>await rgb(hud.locator('.economy-headline .team-name').first())===ct);
 assert.deepEqual(await geometry(),before,'side color changes must not swap or alter historical team values');assert.equal(sockets,beforeSockets,'current team recognition changes do not add recording subscriptions');
 await checkViews(ct,t);await hud.screenshot({path:path.join(scratch,'current-alpha-ct.png')});
 const knownGeometry=await geometry();await command({kind:'step'});await until(async()=>!([ct,t].includes(await rgb(hud.locator('.economy-headline .team-name').first()))));
 assert.notEqual(await rgb(hud.locator('.economy-headline .team-name').first()),await rgb(hud.locator('.economy-headline .team-name').nth(1)),'unknown teams stay neutral but distinguishable');assert.deepEqual(await geometry(),knownGeometry);
 assert.deepEqual(errors,[]);await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,currentTeamColors:true,liveSwap:true,unchangedHistoricalValues:true,unknownNeutral:true,historicalSideSamplesPreserved:true,errors},null,2));console.log('PASS current team colors across graph, recorded views, lineups and match views; historical values and side samples preserved');console.log('Evidence: '+scratch);
}finally{if(origin&&page)try{await page.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}await browser?.close();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),output);}
