import {sdkToolPath} from './sdk-tool.mjs';
import assert from 'node:assert/strict';
import {spawn, execFileSync} from 'node:child_process';
import {mkdtemp, readFile, writeFile} from 'node:fs/promises';
import path from 'node:path';
import {chromium} from 'playwright';

const pkg=path.resolve(process.argv[2]);
const tool=path.resolve(sdkToolPath);
const scratch=await mkdtemp(path.resolve('artifacts/camera-rosters-browser-')), fixturePath=path.join(scratch,'fixture.json');
execFileSync('dotnet',[tool,'fixtures','create',fixturePath]);
const fixture=JSON.parse(await readFile(fixturePath,'utf8')),base=fixture.snapshots[0];
fixture.intervalMilliseconds=5000;
base.managedMatch={id:'roster-demo',name:'Synthetic rosters',bestOf:1,
    teamA:{id:'A',name:'Synthetic Alpha',shortName:'A',logo:'/cast-media/alpha.svg',seriesWins:0},
    teamB:{id:'B',name:'Synthetic Bravo',shortName:'B',logo:'/cast-media/bravo.svg',seriesWins:0},
    ct:{side:'counterTerrorists',status:'manual',teamId:'A',teamAScore:0,teamBScore:0},t:{side:'terrorists',status:'manual',teamId:'B',teamAScore:0,teamBScore:0}};
base.game.managedMatch=base.managedMatch;base.game.observedPlayer=structuredClone(base.game.players[0]);
const ids=base.game.players.map(p=>p.steamId);
const half=structuredClone(base);half.managedMatch.ct.teamId='B';half.managedMatch.t.teamId='A';half.game.managedMatch=half.managedMatch;
half.game.players.forEach(p=>p.side=p.side==='counterTerrorists'?'terrorists':'counterTerrorists');half.game.observedPlayer=structuredClone(half.game.players[0]);
const noGame=structuredClone(half);noGame.game=null;
const incomplete=structuredClone(base);
incomplete.game.players.forEach((p,i)=>{if(i!==0){p.observerSlot=null;if(i%3===0)p.side=null;else if(i%3===1)p.side='unknown';}});
incomplete.game.observedPlayer=structuredClone(incomplete.game.players[0]);
fixture.snapshots=[base,half,noGame,incomplete];fixture.loop=false;await writeFile(fixturePath,JSON.stringify(fixture));
const localCameras=process.env.SOU_TEST_LOCAL_CAMERAS==='1';
const camera=n=>(localCameras?'/playercams/local/view/'+n.toString(16).padStart(32,'0'):'https://getsou.cam/playercams/view')+'#'+n.toString(16).padStart(32,'0')+':'+(n+100).toString(16).padStart(64,'0');
const host=spawn('dotnet',[tool,'dev',pkg,'--port','0','--fixture',fixturePath],{stdio:['ignore','pipe','pipe']});
let output='',origin,browser,control;host.stdout.on('data',v=>output+=v);host.stderr.on('data',v=>output+=v);
const until=async(fn,label='condition')=>{const end=Date.now()+30000;while(!await fn()){if(Date.now()>end)throw Error('Timeout: '+label);await new Promise(r=>setTimeout(r,25));}};
try {
    await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output),'host');origin=output.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
    browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});
    const context=await browser.newContext({viewport:{width:1920,height:1080},reducedMotion:'reduce'}),requests=[],unexpected=[];
    await context.route('**/*',route=>{
        const u=new URL(route.request().url());
        if((u.origin==='https://getsou.cam'&&u.pathname==='/playercams/view') || (u.origin===new URL(origin).origin&&/^\/playercams\/local\/view\/[a-f0-9]{32}$/.test(u.pathname))){
            requests.push(u.origin+u.pathname);
            return route.fulfill({contentType:'text/html',body:'<!doctype html><html style="background:transparent"><body style="margin:0;background:transparent"><div id="surface" style="display:none;position:fixed;inset:0;place-items:center;color:white;background:linear-gradient(125deg,#214d70,#14352b);font:700 24px sans-serif"></div><script>window.instance=crypto.randomUUID();</script></body></html>'});
        }
        if(u.origin===new URL(origin).origin){
            if(u.pathname==='/cast-media/missing-portrait.png')return route.fulfill({status:404,body:''});
            if(u.pathname.startsWith('/cast-media/player-photo-'))return route.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="320" height="360"><rect width="320" height="360" fill="#285a72"/><circle cx="160" cy="112" r="62" fill="#b6def5"/><path d="M40 360v-75a120 120 0 0 1 240 0v75" fill="#b6def5"/></svg>'});
            if(u.pathname.startsWith('/cast-media/'))return route.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80"><path fill="#b6def5" d="M40 4 76 40 40 76 4 40Z"/></svg>'});
            return route.continue();
        }
        unexpected.push(u.origin+u.pathname);return route.abort();
    });
    control=await context.newPage();await control.goto(origin);
    const command=async data=>{const r=await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data});assert(r.ok(),await r.text());};
    await command({kind:'pause'});await control.locator('#hud').evaluate(e=>e.remove());
    const manifest=JSON.parse(await readFile(path.join(pkg,'hud.json'),'utf8'));
    for(const side of ['left','right'])assert(manifest.subviews.some(v=>v.route==='views/roster-'+side));
    const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
    const url=new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href;
    await page.goto(url);await page.locator('.observed-hero').waitFor();
    const longName="Demo Wank'ePEW <em>Long Player Name with Unicode Ω</em> 0123456789";
    let options={players:Object.fromEntries(ids.map((id,i)=>[id,{name:i===0?longName:'Demo Player '+(i+1),...(i<5?{camera:camera(i+1)}:{})}])),
        match:{left:{id:'A',steamIds:ids.slice(0,5)},right:{id:'B',steamIds:ids.slice(5)}}};
    const pool=page.locator('.playercam-receiver'),forId=id=>page.locator(`.playercam-receiver[data-playercam-player="${id}"]`);
    const count=async n=>until(async()=>await pool.count()===n,'pool '+n);
    const capture=async()=>{const saved=new Map();for(const id of await pool.evaluateAll(es=>es.map(e=>e.dataset.playercamPlayer))){const element=await forId(id).elementHandle(),frame=await element.contentFrame();await frame.waitForFunction(()=>!!window.instance);saved.set(id,{element,frame,instance:await frame.evaluate(()=>window.instance)});}return saved;};
    const kept=async saved=>{for(const v of saved.values()){assert(await v.element.evaluate(e=>e.isConnected));assert(!v.frame.isDetached());assert.equal(await v.frame.evaluate(()=>window.instance),v.instance);}};
    const scene=async name=>{await command({kind:'panel',values:{display:{scene:name}}});await page.locator('.scene-'+name).waitFor();};
    const rosterIds=()=>page.locator('[data-roster-player]').evaluateAll(es=>es.map(e=>e.dataset.rosterPlayer));
    const visible=async expected=>{
        await until(async()=>JSON.stringify(await pool.evaluateAll(es=>es.filter(e=>getComputedStyle(e).opacity==='1').map(e=>e.dataset.playercamPlayer)))===JSON.stringify(expected),'visible receivers');
        assert(await pool.evaluateAll(es=>es.every(e=>getComputedStyle(e).display!=='none'&&getComputedStyle(e).visibility==='visible'&&e.loading==='eager'&&!e.hasAttribute('hidden'))));
    };
    const fits=async()=>until(async()=>await page.evaluate(()=>[...document.querySelectorAll('.playercam-receiver.is-active')].every(e=>{
        const target=[...document.querySelectorAll('[data-playercam-target]')].find(t=>t.dataset.playercamTarget===e.dataset.playercamPlayer);
        if(!target)return false;const a=e.getBoundingClientRect(),b=target.getBoundingClientRect();return ['left','top','width','height'].every(k=>Math.abs(a[k]-b[k])<1);
    })),'camera geometry');
    await command({kind:'options',options});await count(5);let saved=await capture(),navs=requests.length;
    if(localCameras)for(let i=0;i<5;i++){
        assert.equal(await forId(ids[i]).getAttribute('src'),camera(i+1),'preserve root-relative local viewer address');
        assert.equal(saved.get(ids[i]).frame.url(),new URL(camera(i+1),origin).href,'resolve local viewer against the HUD loopback origin, not the package base');
    }
    await scene('roster-left');await visible(ids.slice(0,5));await kept(saved);assert.equal(requests.length,navs);assert.deepEqual(await rosterIds(),ids.slice(0,5));
    assert.equal(await page.locator('.camera-roster-name').first().textContent(),longName);assert.equal(await page.locator('.camera-roster-name em').count(),0);assert.equal(await page.locator('.camera-roster-logo').count(),1);
    assert(await page.locator('.camera-roster-name').first().evaluate(e=>e.scrollWidth>e.clientWidth&&getComputedStyle(e).textOverflow==='ellipsis'));
    await scene('roster-right');await visible([]);await kept(saved);assert.deepEqual(await rosterIds(),ids.slice(5));assert.equal(await page.locator('.camera-roster-portrait img').count(),5);
    options={...options,players:Object.fromEntries(ids.map((id,i)=>[id,{...options.players[id],camera:camera(i+1)}]))};await command({kind:'options',options});await count(10);await visible(ids.slice(5));saved=await capture();navs=requests.length;
    for(const [i,id]of ids.entries())if(i%2===0)await saved.get(id).frame.locator('#surface').evaluate((e,n)=>{e.style.display='grid';e.textContent='SYNTHETIC FEED '+n;},i+1);
    for(const side of ['left','right','left','right']){
        await scene('roster-'+side);await visible(side==='left'?ids.slice(0,5):ids.slice(5));await kept(saved);assert.equal(requests.length,navs);await fits();
        const geometry=await page.locator('.camera-roster').boundingBox();assert(geometry.x<1&&geometry.width>1900,'roster uses the full presentation canvas');
        assert.deepEqual(await page.locator('[data-roster-row]').evaluateAll(es=>es.map(e=>e.querySelectorAll('[data-roster-player]').length)),[3,2]);
        assert(await page.evaluate(()=>getComputedStyle(document.querySelector('.viewport')).backgroundImage!=='none'&&getComputedStyle(document.body).backgroundColor==='rgba(0, 0, 0, 0)'));
        await page.screenshot({path:path.join(scratch,'roster-'+side+'.png'),omitBackground:true});
    }
    options={...options,match:{left:{id:'B',steamIds:ids.slice(5)},right:{id:'A',steamIds:ids.slice(0,5)}}};
    await command({kind:'options',options});await scene('roster-left');await visible(ids.slice(5));await kept(saved);assert.equal(requests.length,navs);
    options={...options,match:{left:{id:'A',steamIds:ids.slice(0,5)},right:{id:'B',steamIds:ids.slice(5)}}};
    await command({kind:'options',options});await scene('roster-right');await visible(ids.slice(5));
    // Moving the team boundary leaves the combined source order untouched.
    options={...options,match:{left:{id:'A',steamIds:ids.slice(0,4)},right:{id:'B',steamIds:ids.slice(4)}}};await command({kind:'options',options});
    await visible(ids.slice(4));await kept(saved);assert.deepEqual(await rosterIds(),ids.slice(4));assert.equal(requests.length,navs);
    options={...options,match:{left:{id:'A',steamIds:ids.slice(0,5)},right:{id:'B',steamIds:ids.slice(5)}}};await command({kind:'options',options});await visible(ids.slice(5));
    await scene('live');await visible([ids[0]]);await kept(saved);assert.equal(requests.length,navs);
    await scene('roster-left');await visible(ids.slice(0,5));await command({kind:'step'});await page.locator('.camera-roster.t').waitFor();assert.deepEqual(await rosterIds(),ids.slice(0,5));await kept(saved);
    for(const [width,height]of [[1920,1080],[1280,720],[390,844],[2560,1080]]){
        await page.setViewportSize({width,height});await fits();
        assert(await page.locator('.camera-roster-player').evaluateAll(es=>es.every(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.height>0&&r.left>=-1&&r.right<=innerWidth+1&&r.top>=-1&&r.bottom<=innerHeight+1;})));
    }
    await page.setViewportSize({width:1920,height:1080});
    options={...options,players:{}};await command({kind:'options',options});await count(0);assert.equal(await page.locator('[data-roster-player]').count(),5);assert.equal(await page.locator('.camera-roster-portrait img').count(),5);
    for(const n of [2,3,5]){
        options={...options,match:{left:{id:'A',steamIds:ids.slice(0,n)},right:{id:'B',steamIds:ids.slice(5)}}};await command({kind:'options',options});
        await until(async()=>(await rosterIds()).length===n,'roster size '+n);
        assert.deepEqual(await page.locator('[data-roster-row]').evaluateAll(es=>es.map(e=>e.querySelectorAll('[data-roster-player]').length)),[Math.ceil(n/2),Math.floor(n/2)]);
    }
    options={...options,match:{left:{id:'A',steamIds:ids.slice(0,3)},right:{id:'B',steamIds:ids.slice(5)}}};await command({kind:'options',options});await until(async()=>(await rosterIds()).length===3,'partial roster');
    options={...options,match:{left:{id:'A',steamIds:[]},right:{id:'B',steamIds:ids.slice(5)}}};await command({kind:'options',options});await page.locator('.camera-roster-empty').waitFor();assert.equal(await page.locator('[data-roster-player]').count(),0,'empty verified roster has no fabricated or other-team players');
    options={players:Object.fromEntries(ids.map((id,i)=>[id,{name:'Demo Player '+(i+1),camera:camera(i+1)}])),match:{left:{id:'A',steamIds:ids.slice(0,5)},right:{id:'B',steamIds:ids.slice(5)}}};await command({kind:'options',options});await count(10);
    await command({kind:'panel',values:{display:{scene:'roster-left',agentsOnly:true}}});await count(0);
    await command({kind:'panel',values:{display:{scene:'roster-left',agentsOnly:false}}});await count(10);
    await command({kind:'panel',values:{display:{scene:'roster-left',hideRosters:true}}});await count(0);assert.equal(await page.locator('.camera-roster').count(),0);
    await command({kind:'panel',values:{display:{scene:'roster-left',hideRosters:false}}});await count(10);
    for(const side of ['left','right']){
        const old=await capture();await page.goto(url+'views/roster-'+side);await page.locator('.camera-roster-'+side).waitFor();await count(10);
        await visible(side==='left'?ids.slice(0,5):ids.slice(5));await fits();
        for(const v of old.values())await until(()=>v.frame.isDetached(),'old document closed');
    }
    await page.goto(url);await page.locator('.camera-roster-left').waitFor();await count(10);
    await command({kind:'step'});await count(10);await visible(ids.slice(0,5));await until(async()=>(await rosterIds()).length===5,'roster before game data');
    assert.equal(await page.locator('.camera-roster-portrait').count(),5,'metadata identity fallbacks remain before game data while authorized camera receivers remain active');
    // The declared routes also load as actual documents. Such navigation reconnects.
    for(const side of ['left','right']){await page.goto(url+'views/roster-'+side);await page.locator('.camera-roster-'+side).waitFor();await until(async()=>JSON.stringify(await rosterIds())===JSON.stringify(side==='left'?ids.slice(0,5):ids.slice(5)),'route roster '+side);}
    // With no GSI, remove every camera grant: portraits must be actual bundled
    // images on both selected teams, never the name/initial monogram.
    const beforeRemoval=await capture();
    options={...options,players:Object.fromEntries(ids.map((id,i)=>[id,{name:'Synthetic Player '+(i+1),...(i===0||i===5?{avatar:'/cast-media/player-photo-'+i+'.svg'}:{})}]))};
    await command({kind:'options',options});await count(0);
    for(const old of beforeRemoval.values())await until(()=>old.frame.isDetached(),'revoked camera iframe disposed');
    const portraitSources=async()=>page.locator('[data-roster-player]').evaluateAll(es=>es.map(e=>({id:e.dataset.rosterPlayer,src:e.querySelector('.camera-roster-portrait img')?.getAttribute('src')})));
    const loadedPortraits=async()=>{
        await until(async()=>await page.locator('.camera-roster-portrait img').count()===5,'five portrait images');
        await until(()=>page.locator('.camera-roster-portrait img').evaluateAll(es=>es.every(e=>e.complete&&e.naturalWidth>0)),'portrait images decoded');
        assert.equal(await page.locator('.camera-roster-portrait.monogram').count(),0);
        assert.equal(await pool.count(),0);
    };
    const noGsiAgents=new Map();
    for(const side of ['left','right']){
        await page.goto(url+'views/roster-'+side);await loadedPortraits();
        for(const p of await portraitSources()){
            if(p.id===ids[0]||p.id===ids[5])assert(p.src.includes('/cast-media/player-photo-'),'configured photo wins without GSI');
            else {assert(/^assets\/agents\/(ct|t)-[0-9]\.png$/.test(p.src),'missing identity has a bundled decorative agent');noGsiAgents.set(p.id,p.src);}
        }
        await page.screenshot({path:path.join(scratch,'no-camera-no-gsi-'+side+'-1920.png')});
        await page.setViewportSize({width:390,height:844});await loadedPortraits();
        await page.screenshot({path:path.join(scratch,'no-camera-no-gsi-'+side+'-390.png')});
        await page.setViewportSize({width:1920,height:1080});
        await page.reload();await loadedPortraits();
        for(const p of await portraitSources())if(noGsiAgents.has(p.id))assert.equal(p.src,noGsiAgents.get(p.id),'stable decorative image after document reload');
    }
    // No photos and one failed photo must all fall back to agents, still without GSI.
    await command({kind:'panel',values:{display:{scene:'roster-left'}}});
    await page.goto(url);await page.locator('.camera-roster-left').waitFor();await loadedPortraits();
    options={...options,players:Object.fromEntries(ids.map((id,i)=>[id,{name:'Renamed Synthetic '+(i+1),...(i===1?{avatar:'/cast-media/missing-portrait.png'}:{})}]))};
    await command({kind:'options',options});
    for(const side of ['left','right']){
        await scene('roster-'+side);await loadedPortraits();
        await until(async()=>(await portraitSources()).every(p=>/^assets\/agents\/(ct|t)-[0-9]\.png$/.test(p.src)),'all agent fallbacks');
        for(const p of await portraitSources())if(noGsiAgents.has(p.id))assert.equal(p.src,noGsiAgents.get(p.id),'alias changes keep the same decorative image');
        await page.screenshot({path:path.join(scratch,'agents-only-no-gsi-'+side+'.png')});
    }
    await command({kind:'step'});
    for(const side of ['left','right']){
        await scene('roster-'+side);await loadedPortraits();
        assert((await portraitSources()).every(p=>/^assets\/agents\/(ct|t)-[0-9]\.png$/.test(p.src)));
        if(side==='left')await until(async()=>(await portraitSources()).find(p=>p.id===ids[0])?.src==='assets/agents/ct-'+base.game.players[0].observerSlot+'.png','regular side/slot agent retained');
        await page.screenshot({path:path.join(scratch,'incomplete-side-slot-'+side+'.png')});
    }
    assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
    await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,package:pkg,version:manifest.version,localCameras,receiverCounts:[5,10],sameDocumentReceiverReuse:true,crossDocumentReuse:false,rosterSizes:[0,2,3,4,5,6],twoRowSplits:[[1,1],[2,1],[3,2]],cameraReceiversBeforeGsi:true,noCameraAgentFallback:true,removedCameraAgentFallback:true,noGsiPortraits:true,incompleteSideSlotPortraits:true,configuredPhotosPreserved:true,failedPhotosFallBack:true,selectedMatchTeamOrder:true,longNames:true,textOnly:true,partialFeeds:true,halftime:true,boundaryTransfer:true,viewports:[390,1280,1920,2560],realMedia:false,errors,unexpected},null,2));
    console.log('PASS roster left/right, five/ten warm receivers, same-document reuse, team transfer, halftime, fallback, names, full-screen presentation and scaling; evidence: '+scratch);
} finally {
    if(origin&&control)try{await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}
    await browser?.close();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),output);
}
