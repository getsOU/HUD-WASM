import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {mkdtemp,readFile,writeFile,mkdir} from 'node:fs/promises';
import path from 'node:path';
import {chromium} from 'playwright';
const tool=path.resolve('artifacts/published-sdk-tools/.store/sou.hud.tool/0.3.0-preview.9/sou.hud.tool/0.3.0-preview.9/tools/net10.0/any/SOU.Hud.Tool.dll');
const packagePath=path.resolve(process.argv[2]),fixture=JSON.parse(await readFile('tests/fixtures/recording-showcase.json','utf8'));
fixture.snapshots=[fixture.snapshots[0]];const source=fixture.snapshots[0];source.game.roundPhase='live';source.game.countdown={phase:'live',secondsRemaining:90};source.managedMatch.teamA.logo='/cast-media/alignment-logo.svg';source.game.managedMatch=source.managedMatch;fixture.loop=false;
await mkdir('artifacts',{recursive:true});const scratch=await mkdtemp(path.resolve('artifacts/portrait-alignment-browser-')),fixturePath=path.join(scratch,'fixture.json');await writeFile(fixturePath,JSON.stringify(fixture));
const manifest=JSON.parse(await readFile(path.join(packagePath,'hud.json'),'utf8'));
const host=spawn('dotnet',[tool,'dev',packagePath,'--port','0','--fixture',fixturePath],{stdio:['ignore','pipe','pipe']});let output='',origin,browser,page;
host.stdout.on('data',v=>output+=v);host.stderr.on('data',v=>output+=v);
const until=async fn=>{const deadline=Date.now()+30000;while(!await fn()){if(Date.now()>deadline)throw new Error('Timeout: '+output);await new Promise(r=>setTimeout(r,30));}};
try{
 await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output));origin=output.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});page=await browser.newPage();await page.goto(origin);
 const command=async data=>{const r=await page.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data});assert(r.ok(),await r.text());};await command({kind:'pause'});
 const hud=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:2}),errors=[];hud.on('pageerror',e=>errors.push(e.message));
 let photoRequests=0;await hud.route('**/cast-media/alignment-*.svg',route=>{if(route.request().url().includes('missing'))return route.fulfill({status:404,body:''});if(route.request().url().includes('portrait'))photoRequests++;return route.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="400" height="900"><rect width="400" height="150" fill="#ee4444"/><text x="35" y="100" font-size="70" fill="white">HEAD</text><rect y="150" width="400" height="500" fill="#44bb55"/><rect y="650" width="400" height="250" fill="#3388ee"/></svg>'});});
 const stem=new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href,steam=source.game.players[0].steamId;
 const profile={players:{[steam]:{avatar:'/cast-media/alignment-portrait.svg'}},match:{left:{id:'example-alpha',steamIds:[steam]},right:{id:'example-bravo',steamIds:[]}}};await command({kind:'options',options:profile});await hud.goto(stem);
 const frame=()=>hud.locator('.roster-portrait:has(img[src*="alignment-portrait.svg"])').first();await frame().waitFor();await until(async()=>await frame().locator('img').evaluate(i=>i.complete&&i.naturalHeight===900));
 const measure=()=>frame().evaluate(e=>({width:e.getBoundingClientRect().width,height:e.getBoundingClientRect().height,fit:getComputedStyle(e.querySelector('img')).objectFit,image:getComputedStyle(e.querySelector('img')).objectPosition,mask:getComputedStyle(e.querySelector('.avatar-effects')).maskPosition}));
 const base=await measure();assert.equal(base.image,'50% 0%');assert.equal(base.mask,base.image);assert.equal(base.fit,'cover');await frame().screenshot({path:path.join(scratch,'default-top.png')});
 const display={};const panel=async values=>{Object.assign(display,values);await command({kind:'panel',values:{display}});};const requests=photoRequests;
 for(const[name,position]of [['top-left','0% 0%'],['top-right','100% 0%'],['left','0% 50%'],['center','50% 50%'],['right','100% 50%'],['bottom-left','0% 100%'],['bottom','50% 100%'],['bottom-right','100% 100%'],['top','50% 0%']]){
  await panel({portraitAlignment:name});await until(async()=>(await measure()).image===position);const state=await measure();assert.equal(state.mask,position);assert.equal(state.width,base.width);assert.equal(state.height,base.height);assert.equal(state.fit,base.fit);
  await until(async()=>await hud.locator('.observed .avatar-image').evaluate(i=>getComputedStyle(i).objectPosition)===position);
  if(['center','bottom'].includes(name))await frame().screenshot({path:path.join(scratch,`${name}.png`)});
 }
 assert.equal(photoRequests,requests,'alignment-only changes do not reload the supplied portrait');
 await panel({horizontalPlayercards:true,portraitAlignment:'top'});await hud.locator('.rosters-horizontal').waitFor();await until(async()=>(await measure()).image==='50% 0%');assert.equal((await measure()).fit,'contain','existing horizontal fit remains unchanged');
 await panel({portraitAlignment:'bottom'});await until(async()=>(await measure()).mask==='50% 100%');await panel({horizontalPlayercards:false,portraitAlignment:'top-right'});
 for(const view of ['lineups','spotlight','round-compare','side-statistics','multikills','round-buys','economy-evidence']){
  await hud.goto(stem+'views/'+view);const portrait=hud.locator('img[src*="alignment-portrait.svg"]').first();await portrait.waitFor();assert.equal(await portrait.evaluate(i=>getComputedStyle(i).objectPosition),'100% 0%',view+' shares the selected alignment');
  await panel({portraitAlignment:'bottom-left'});await until(async()=>await portrait.evaluate(i=>getComputedStyle(i).objectPosition)==='0% 100%');await panel({portraitAlignment:'top-right'});await until(async()=>await portrait.evaluate(i=>getComputedStyle(i).objectPosition)==='100% 0%');
 }
 await hud.goto(stem+'views/prematch');const logo=hud.locator('img[src*="alignment-logo.svg"]');await logo.waitFor();assert.equal(await logo.evaluate(i=>getComputedStyle(i).objectPosition),'50% 50%','logos keep their independent alignment');
 await hud.goto(stem);await frame().waitFor();await panel({agentsOnly:true,portraitAlignment:'top'});await until(async()=>await hud.locator('.roster-portrait img[data-image-fallback="true"]').count()>0);assert.equal(await hud.locator('.roster-portrait .avatar-image').first().evaluate(i=>getComputedStyle(i).objectPosition),'50% 0%');
 await panel({agentsOnly:false,portraitAlignment:'bottom'});profile.players[steam].avatar='/cast-media/alignment-missing.svg';await command({kind:'options',options:profile});await until(async()=>await hud.locator('.roster-portrait img[data-image-fallback="true"]').count()===source.game.players.length);assert.equal(await hud.locator('.roster-portrait .avatar-image').first().evaluate(i=>getComputedStyle(i).objectPosition),'50% 100%','failed profile fallback retains alignment');
 assert.deepEqual(errors,[]);await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,default:'top center',alignments:9,verticalAndHorizontal:true,effectMask:true,observer:true,viewUpdates:true,fallback:true,logosUnchanged:true,errors},null,2));console.log('PASS top-anchored player images, nine panel positions, masks, live/observer/views, agent fallback and unchanged logo alignment');console.log('Evidence: '+scratch);
}finally{if(origin&&page)try{await page.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}await browser?.close();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),output);}
