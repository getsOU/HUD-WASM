import {sdkToolPath, sdkVersion} from './sdk-tool.mjs';
import assert from 'node:assert/strict';
import {spawn,execFileSync} from 'node:child_process';
import {mkdtemp,readFile,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {chromium} from 'playwright';
const tool=path.resolve(sdkToolPath);
const pkg=path.resolve(process.argv[2]);
const scratch=await mkdtemp(path.resolve('artifacts/playercam-warm-browser-')),file=path.join(scratch,'fixture.json');
execFileSync('dotnet',[tool,'fixtures','create',file]);
const fixture=JSON.parse(await readFile(file,'utf8')),base=fixture.snapshots[0];
base.managedMatch={id:'warm-demo',name:'Synthetic warm camera test',bestOf:1,
    teamA:{id:'A',name:'Alpha',shortName:'A',logo:null,seriesWins:0},teamB:{id:'B',name:'Bravo',shortName:'B',logo:null,seriesWins:0},
    ct:{side:'counterTerrorists',status:'manual',teamId:'A',teamAScore:0,teamBScore:0},t:{side:'terrorists',status:'manual',teamId:'B',teamAScore:0,teamBScore:0}};
base.game.managedMatch=base.managedMatch;base.game.currentPlayer=structuredClone(base.game.players[9]);base.game.observedPlayer=structuredClone(base.game.players[0]);
const ids=base.game.players.map(p=>p.steamId), cuts=Array.from({length:12},(_,i)=>i%2===0?1:0);
const cutSnapshots=cuts.map(i=>{const s=structuredClone(base);s.game.observedPlayer=structuredClone(s.game.players[i]);return s;});
const absent=structuredClone(base);absent.game.observedPlayer=null;
const seekSessions=['11111111-1111-4111-8111-111111111111','22222222-2222-4222-8222-222222222222','33333333-3333-4333-8333-333333333333'];
const seekSnapshots=seekSessions.map((sessionId,i)=>{
    const s=structuredClone(base);s.sessionId=sessionId;s.game.roundNumber=[8,1,9][i];
    s.game.roundPhase=i===0?'over':'live';s.game.phase=i===0?'gameOver':'live';return s;
});
const newMatch=structuredClone(base);newMatch.managedMatch={...newMatch.managedMatch,id:'new-match',teamA:{...newMatch.managedMatch.teamA,id:'C'},teamB:{...newMatch.managedMatch.teamB,id:'D'},ct:{...newMatch.managedMatch.ct,teamId:'C'},t:{...newMatch.managedMatch.t,teamId:'D'}};newMatch.game.managedMatch=newMatch.managedMatch;
const newMap=structuredClone(newMatch);newMap.game.map='de_inferno';
fixture.snapshots=[base,...cutSnapshots,absent,structuredClone(base),...seekSnapshots,newMatch,newMap];fixture.loop=false;await writeFile(file,JSON.stringify(fixture));
const localCameras=process.env.SOU_TEST_LOCAL_CAMERAS==='1';
let wireSession=null,gameMessages=0;const forwardedSeekSessions=new Set();
const camera=n=>(localCameras?'/playercams/local/view/'+n.toString(16).padStart(32,'0'):'https://getsou.cam/playercams/view')+'#'+n.toString(16).padStart(32,'0')+':'+(n+100).toString(16).padStart(64,'0');
const host=spawn('dotnet',[tool,'dev',pkg,'--port','0','--fixture',file],{stdio:['ignore','pipe','pipe']});
let output='',origin,browser,control;host.stdout.on('data',v=>output+=v);host.stderr.on('data',v=>output+=v);
const until=async fn=>{const end=Date.now()+30000;while(!await fn()){if(Date.now()>end)throw Error('Timeout '+output);await new Promise(r=>setTimeout(r,25));}};
try{
    await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output));origin=output.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
    browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});
    const context=await browser.newContext({viewport:{width:1920,height:1080},reducedMotion:'reduce'}),unexpected=[],requests=[];
    // The dev tool fixes the source session ID. Inject complete public V3 game
    // baselines at the transport boundary to model actual statistics-session resets.
    await context.routeWebSocket('**/*',socket=>{
        const server=socket.connectToServer(),objects=new Map();let sourceSession=null,lastSession=null;
        server.onMessage(message=>{
            if(typeof message!=='string'){socket.send(message);return;}
            const parts=message.split('\x1e').map(part=>{
                if(!part)return part;let envelope;try{envelope=JSON.parse(part);}catch{return part;}
                const game=envelope.item?.game;
                if(game?.schemaVersion===3&&Array.isArray(game.objects)){
                    gameMessages++;
                    if(game.kind==='snapshot'||sourceSession!==game.sessionId)objects.clear();
                    sourceSession=game.sessionId;
                    for(const object of game.objects){const key=object.kind+'\0'+object.key;if(object.removed)objects.delete(key);else objects.set(key,object);}
                    if(wireSession){
                        game.sessionId=wireSession;
                        if(lastSession!==wireSession){game.kind='snapshot';game.baseRevision=null;game.objects=[...objects.values()];}
                        forwardedSeekSessions.add(wireSession);
                    }
                    lastSession=game.sessionId;
                }
                return JSON.stringify(envelope);
            });
            socket.send(parts.join('\x1e'));
        });
    });
    await context.route('**/*',route=>{
        const u=new URL(route.request().url());
        if((['https://getsou.cam','https://www.getsou.app'].includes(u.origin)&&u.pathname==='/playercams/view')||(u.origin===new URL(origin).origin&&/^\/playercams\/local\/view\/[a-f0-9]{32}$/.test(u.pathname))){
            requests.push(u.origin+u.pathname);
            return route.fulfill({contentType:'text/html',body:'<!doctype html><html style="background:transparent"><body style="margin:0;background:transparent"><div id="surface" style="display:none;position:fixed;inset:0;background:#21b978"></div><script>window.instance=crypto.randomUUID();</script></body></html>'});
        }
        if(u.origin===new URL(origin).origin)return route.continue();
        unexpected.push(u.origin+u.pathname);return route.abort();
    });
    control=await context.newPage();await control.goto(origin);
    const command=async data=>{const r=await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data});assert(r.ok(),await r.text());};
    await command({kind:'pause'});
    const manifest=JSON.parse(await readFile(path.join(pkg,'hud.json'),'utf8'));
    assert.equal(manifest.id,'sou-basic-wasm');assert.equal(manifest.sdkVersion,sdkVersion);
    const page=await context.newPage(),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    const url=new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href;
    await page.goto(url);await page.locator('.observed .portrait img').waitFor();await page.evaluate(()=>document.fonts.ready);
    const pool=page.locator('.observed-playercam'), active=()=>page.locator('.observed-playercam.is-active');
    const forId=id=>page.locator(`.observed-playercam[data-playercam-player="${id}"]`);
    const waitPool=async count=>until(async()=>await pool.count()===count);
    const capture=async()=>{
        const result=new Map();
        for(const id of await pool.evaluateAll(es=>es.map(e=>e.dataset.playercamPlayer))){
            const element=await forId(id).elementHandle(),frame=await element.contentFrame();
            await frame.waitForFunction(()=>!!window.instance);
            result.set(id,{element,frame,instance:await frame.evaluate(()=>window.instance)});
        }
        return result;
    };
    const assertKept=async saved=>{for(const [id,v]of saved){assert(await v.element.evaluate(e=>e.isConnected),id+' DOM stays warm');assert(!v.frame.isDetached());assert.equal(await v.frame.evaluate(()=>window.instance),v.instance);assert.equal(await forId(id).count(),1);}};
    const assertGone=async saved=>{for(const v of saved.values())await until(()=>v.frame.isDetached());};
    const checkActive=async id=>{
        await until(async()=>await active().count()===(id?1:0));
        if(id)await until(async()=>await active().getAttribute('data-positioned')==='true');
        if(id)await until(async()=>await active().getAttribute('data-playercam-player')===id);
        const values=await pool.evaluateAll(es=>es.map(e=>({id:e.dataset.playercamPlayer,active:e.classList.contains('is-active'),opacity:getComputedStyle(e).opacity,display:getComputedStyle(e).display,visibility:getComputedStyle(e).visibility,hidden:e.hasAttribute('hidden'),loading:e.loading})));
        assert(values.every(v=>v.opacity===(v.id===id?'1':'0')&&v.active===(v.id===id)&&v.display!=='none'&&v.visibility==='visible'&&!v.hidden&&v.loading==='eager'),'only the right camera is visible; every receiver remains rendered eagerly');
    };
    assert.equal(await pool.count(),0);
    let options={players:{[ids[0]]:{camera:camera(1)},[ids[1]]:{camera:camera(2)},[ids[9]]:{camera:camera(3)}},match:{left:{id:'A',steamIds:ids.slice(0,5)},right:{id:'B',steamIds:ids.slice(5)}}};
    const baselineMessages=gameMessages;await command({kind:'options',options});await waitPool(3);await checkActive(ids[0]);
    assert.equal(gameMessages,baselineMessages,'pool creation does not require GSI');
    let saved=await capture();const a=saved.get(ids[0]);
    assert.equal(await forId(ids[0]).getAttribute('allow'),"autoplay; camera 'none'; microphone 'none'");assert.equal(await forId(ids[0]).getAttribute('referrerpolicy'),'no-referrer');assert.equal(await forId(ids[0]).getAttribute('sandbox'),'allow-scripts allow-same-origin');
    await a.frame.locator('#surface').evaluate(e=>e.style.display='block');
    await page.locator('.observed-hero').screenshot({path:path.join(scratch,'synthetic-A.png')});
    // Unobserved metadata must update the pool while keeping A and C alive.
    options={...options,players:{...options.players,[ids[1]]:{camera:camera(4)}}};await command({kind:'options',options});await until(async()=>await forId(ids[1]).getAttribute('src')===camera(4));await until(()=>saved.get(ids[1]).frame.isDetached());await assertKept(new Map([[ids[0],a],[ids[9],saved.get(ids[9])]]));
    options={...options,players:{...options.players,[ids[1]]:{}}};await command({kind:'options',options});await waitPool(2);await checkActive(ids[0]);
    options={...options,players:{...options.players,[ids[1]]:{camera:camera(2)}}};await command({kind:'options',options});await waitPool(3);
    saved=await capture();options={...options,players:{...options.players,[ids[0]]:{camera:camera(5)}}};await command({kind:'options',options});await until(async()=>await forId(ids[0]).getAttribute('src')===camera(5));await until(()=>saved.get(ids[0]).frame.isDetached());await assertKept(new Map([...saved].filter(([id])=>id!==ids[0])));
    assert.equal(gameMessages,baselineMessages,'all profile replacements/removals were options-only');
    saved=await capture();await saved.get(ids[0]).frame.locator('#surface').evaluate(e=>e.style.display='block');
    const warmedRequests=requests.length;
    for(const [cut,index] of cuts.entries()){
        await command({kind:'step'});await checkActive(ids[index]);await assertKept(saved);assert.equal(requests.length,warmedRequests,'a pure cut starts no new viewer navigation');
        if(cut===0){
            assert.equal(await saved.get(ids[1]).frame.locator('#surface').evaluate(e=>getComputedStyle(e).display),'none');
            assert(await saved.get(ids[1]).frame.evaluate(()=>[document.documentElement,document.body].every(e=>getComputedStyle(e).backgroundColor==='rgba(0, 0, 0, 0)')));
            assert(await page.locator('.observed .portrait img').isVisible());
            await page.locator('.observed-hero').screenshot({path:path.join(scratch,'synthetic-B-offline.png')});
        }
    }
    // A missing observer hides every camera while retaining their exact receiver windows.
    await command({kind:'step'});await checkActive(null);await assertKept(saved);assert.equal(await page.locator('.observed').count(),0);
    assert.equal(requests.length,warmedRequests,'missing observer starts no viewer navigation');
    await command({kind:'step'});await waitPool(3);await checkActive(ids[0]);await assertKept(saved);
    assert.equal(requests.length,warmedRequests,'returning observer reuses the same viewers');saved=await capture();
    // Permission / bad URL removals are immediate, including unobserved streams.
    options={...options,players:{...options.players,[ids[1]]:{camera:'https://external.invalid/playercams/view#bad'}}};await command({kind:'options',options});await waitPool(2);await until(()=>saved.get(ids[1]).frame.isDetached());await assertKept(new Map([...saved].filter(([id])=>id!==ids[1])));
    options={...options,players:{...options.players,[ids[1]]:{camera:camera(2)}}};await command({kind:'options',options});await waitPool(3);
    saved=await capture();await command({kind:'panel',values:{display:{agentsOnly:true}}});await waitPool(0);await assertGone(saved);await command({kind:'panel',values:{display:{agentsOnly:false}}});await waitPool(3);
    saved=await capture();await command({kind:'panel',values:{display:{hideObservedPlayer:true}}});await waitPool(0);await assertGone(saved);await command({kind:'panel',values:{display:{hideObservedPlayer:false}}});await waitPool(3);
    // Twelve assigned cameras still create only the first ten roster receivers.
    const manyIds=[...ids,'76561198000010001','76561198000010002'];
    const many={players:Object.fromEntries(manyIds.map((id,i)=>[id,{camera:camera(20+i)}])),match:{left:{id:'A',steamIds:manyIds.slice(0,5)},right:{id:'B',steamIds:manyIds.slice(5)}}};
    await command({kind:'options',options:many});await waitPool(10);assert.deepEqual(await pool.evaluateAll(es=>es.map(e=>e.dataset.playercamPlayer)),ids);await checkActive(ids[0]);
    saved=await capture();const beforeSeekNavigations=requests.length;
    for(const [index,sessionId] of seekSessions.entries()){
        wireSession=sessionId;await command({kind:'step'});
        await until(async()=>await page.locator('.clock-label').textContent()===['FINAL','ROUND 1','ROUND 9'][index]);
        assert(forwardedSeekSessions.has(sessionId),'a full V3 game baseline with the changed source session was forwarded before the new round rendered');
        await waitPool(10);await checkActive(ids[0]);await assertKept(saved);
        assert.equal(requests.length,beforeSeekNavigations,'statistics-session/round rollback retains all ten viewer windows');
    }
    saved=await capture();await command({kind:'options',options:{...many,players:{}}});await waitPool(0);await assertGone(saved);assert(await page.locator('.observed .portrait img').isVisible());
    await command({kind:'options',options});await waitPool(3);
    for(const view of manifest.subviews.filter(v=>!v.route.startsWith('views/roster-'))){
        console.log('CHECK cold outside live:',view.route);saved=await capture();await page.goto(url+view.route);await page.locator('.broadcast-scene').waitFor();assert.equal(await page.locator('iframe').count(),0);await assertGone(saved);await page.goto(url);await waitPool(3);
    }
    saved=await capture();await command({kind:'step'});await waitPool(0);await assertGone(saved);
    options={...options,match:{left:{id:'C',steamIds:ids.slice(0,5)},right:{id:'D',steamIds:ids.slice(5)}}};await command({kind:'options',options});await waitPool(3);
    saved=await capture();await command({kind:'reconnect'});await assertGone(saved);await waitPool(3);
    saved=await capture();await command({kind:'step'});await assertGone(saved);await waitPool(3);await checkActive(ids[0]);
    const frame=await (await forId(ids[0]).elementHandle()).contentFrame();await frame.waitForFunction(()=>!!window.instance);
    assert(await frame.evaluate(()=>[document.documentElement,document.body].every(e=>getComputedStyle(e).backgroundColor==='rgba(0, 0, 0, 0)')));assert(await page.locator('.observed .portrait img').isVisible());
    for(const [width,height] of [[1920,1080],[1280,720],[390,844],[2560,1080]]){await page.setViewportSize({width,height});await page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));assert(await pool.evaluateAll(es=>es.every(e=>{const a=e.getBoundingClientRect(),target=[...document.querySelectorAll('[data-playercam-target]')].find(t=>t.dataset.playercamTarget===e.dataset.playercamPlayer);if(!target||e.dataset.playercamActive!=='true')return true;const b=target.getBoundingClientRect();return Math.abs(a.width-b.width)<1&&Math.abs(a.height-b.height)<1&&Math.abs(a.left-b.left)<1&&Math.abs(a.top-b.top)<1;})));}
    assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
    await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,package:pkg,version:manifest.version,localCameras,cuts:cuts.length,extraViewerNavigationsDuringCuts:0,cap:10,subviews:manifest.subviews.length,syntheticViewers:true,statisticsSessionResets:[...forwardedSeekSessions],sessionResetMethod:"test-only WebSocket proxy of complete public V3 game baselines",tenReceiversPreservedAcrossSeek:true,missingObserverPreservesReceivers:true,physicalCamera:false,liveKitLoadMeasured:false,errors,unexpected},null,2));
    console.log('PASS warm playercam identities, cuts, options-only updates, cap10, teardown, fallback and sizing; evidence: '+scratch);
}finally{
    if(origin&&control)try{await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}
    await browser?.close();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),output);
}
