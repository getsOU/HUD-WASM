import assert from 'node:assert/strict';
import {spawn, execFileSync} from 'node:child_process';
import {mkdtemp, readFile, writeFile} from 'node:fs/promises';
import path from 'node:path';
import {chromium} from 'playwright';

const tool=path.resolve('artifacts/published-sdk-tools/.store/sou.hud.tool/0.3.0-preview.9/sou.hud.tool/0.3.0-preview.9/tools/net10.0/any/SOU.Hud.Tool.dll');
const pkg=path.resolve(process.argv[2] ?? 'artifacts/package');
const scratch=await mkdtemp(path.resolve('artifacts/killfeed-trades-'));
const file=path.join(scratch,'fixture.json');
execFileSync('dotnet',[tool,'fixtures','create',file]);
const fixture=JSON.parse(await readFile(file,'utf8')), base=fixture.snapshots[0];
base.managedMatch={id:'trade-demo',name:'Trade demo',bestOf:1,
    teamA:{id:'ct',name:'Demo CT',shortName:'CT',logo:null,seriesWins:0},
    teamB:{id:'t',name:'Demo T',shortName:'T',logo:null,seriesWins:0},
    ct:{side:'counterTerrorists',status:'manual',teamId:'ct',teamAScore:0,teamBScore:0},
    t:{side:'terrorists',status:'manual',teamId:'t',teamAScore:0,teamBScore:0}};
base.game.managedMatch=base.managedMatch;
base.game.roundPhase='live';
const who=(i,name)=>({entityIndex:i+1,steamId:base.game.players[i].steamId,name,side:i<5?'counterTerrorists':'terrorists'});
const C=who(0,'Current killer C'), A=who(1,"Wank&apos;ePEW"), assist=who(2,'Flash assistant'), B=who(5,'Enemy B'), D=who(6,'Earlier D');
const kill=(id,time,killer,victim)=>({id,connectionId:'11111111-1111-1111-1111-111111111111',epoch:1,gameTimeSeconds:time,
    killer,victim,assister:null,weapon:'weapon_ak47',weaponKind:'ak47',flashAssist:false,headshot:true,wallbang:true,attackerBlind:false,throughSmoke:true,noScope:true});
const oldest=kill(1,100,A,D), prior={...kill(2,102,B,A),tradedKill:oldest};
const normal={...kill(3,104,C,B),assister:assist,flashAssist:true};
const linked={...normal,tradedKill:prior};
const noAssist={...linked,assister:null};
const plainAssist={...linked,flashAssist:false};
fixture.snapshots=[normal,linked,noAssist,plainAssist,plainAssist].map((event,i)=>{
    const s=structuredClone(base);s.game.killFeed=[event];if(i===4){s.game.roundPhase='over';s.game.countdown={phase:'over',secondsRemaining:0};}return s;
});
fixture.loop=false;
await writeFile(file,JSON.stringify(fixture));
const host=spawn('dotnet',[tool,'dev',pkg,'--port','0','--fixture',file],{stdio:['ignore','pipe','pipe']});
let output='',browser,control,origin;
const frames=[];
let fixtureIndex=0;
host.stdout.on('data',v=>output+=v);host.stderr.on('data',v=>output+=v);
const until=async(fn,timeout=10000)=>{const end=Date.now()+timeout;while(!await fn()){if(Date.now()>end)throw Error('Timeout '+output);await new Promise(r=>setTimeout(r,25));}};
try{
    await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output),30000);
    origin=output.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
    browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});
    control=await browser.newPage();await control.goto(origin);
    const command=async data=>{const r=await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data});assert(r.ok(),await r.text());};
    await command({kind:'pause'});
    const manifest=JSON.parse(await readFile(path.join(pkg,'hud.json'),'utf8'));
    assert(manifest.requirements.optionalFeatures.includes('killfeed.trades.v1'));
    const hud=await browser.newPage({viewport:{width:1920,height:1080}}),errors=[];
    hud.on('pageerror',e=>errors.push(e.message));
    hud.on('websocket',ws=>ws.on('framereceived',e=>frames.push(e.payload.toString())));
    // The local SDK dev host projects its raw/V2 fixtures without TradedKill.
    // Supply the public V3 kill DTO at the transport boundary, preserving the
    // host's session, revisions, panel/options transport and real SDK receiver.
    // This is test input only; the HUD never infers or injects trade relations.
    await hud.routeWebSocket(url=>url.pathname.includes('/hud-live/v3'),route=>{
        const server=route.connectToServer();
        server.onMessage(data=>{
            const messages=data.toString().split('\x1e').filter(Boolean).map(text=>{
                const message=JSON.parse(text), game=message.item?.game;
                if(game){
                    const event=fixture.snapshots[fixtureIndex].game.killFeed[0];
                    game.objects=game.objects.filter(o=>o.kind!=='kill');
                    game.objects.push({kind:'kill',key:'11111111111111111111111111111111:1:3',removed:false,kill:event});
                }
                return JSON.stringify(message);
            });
            route.send(messages.join('\x1e')+'\x1e');
        });
    });
    await hud.goto(new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href);
    await hud.locator('.kill-event').waitFor();
    const started=Date.now();
    assert.equal(await hud.locator('.kill-trade').count(),0,'normal kill has no trade');
    await hud.evaluate(()=>{window.tradeRow=document.querySelector('.kill-event');window.tradeAnimations=window.tradeRow.getAnimations();});
    fixtureIndex=1;await command({kind:'step'});
    await hud.locator('.kill-trade').waitFor({timeout:5000});
    const roles=()=>hud.locator('[data-kill-role]').evaluateAll(es=>es.map(e=>e.dataset.killRole));
    assert.deepEqual(await roles(),['killer','assister','traded','victim']);
    assert.equal(await hud.locator('[data-kill-role="traded"]').textContent(),"Wank'ePEW");
    assert.equal(await hud.locator('.kill-trade').getAttribute('title'),"Möglicher Trade für Wank'ePEW");
    assert.equal(await hud.locator('.kill-trade.ct [data-ui-icon="trade"]').count(),1);
    assert.equal(await hud.locator('[data-ui-icon="flashassist"]').count(),1);
    assert.equal(await hud.locator('.kill-event').count(),1,'nested predecessors stay outside visible feed');
    assert.equal(await hud.locator('.kill-trade [data-ui-icon="trade"]').count(),1,'only immediate chain link');
    fixtureIndex=2;await command({kind:'step'});
    await until(async()=>await hud.locator('[data-kill-role="assister"]').count()===0);
    assert.deepEqual(await roles(),['killer','traded','victim']);
    assert.equal(await hud.locator('[data-ui-icon="flashassist"]').count(),0);
    fixtureIndex=3;await command({kind:'step'});
    await hud.locator('[data-kill-role="assister"]').waitFor();
    assert.equal(await hud.locator('[data-ui-icon="flashassist"]').count(),0,'ordinary assist has no flash icon');
    const longName=('Alias <b>literal</b> &amp; '+ 'Very long traded teammate name '.repeat(5)).trim();
    const options={players:{[A.steamId]:{name:longName}},match:{left:{id:'ct',steamIds:[C.steamId,A.steamId,assist.steamId]},right:{id:'t',steamIds:[B.steamId,D.steamId]}}};
    await command({kind:'options',options});
    await until(async()=>await hud.locator('[data-kill-role="traded"]').textContent()===longName);
    assert.equal(await hud.locator('.kill-trade').getAttribute('title'),'Möglicher Trade für '+longName);
    assert.equal(await hud.locator('.kill-trade b').count(),0,'alias stays plain text');
    const geometry=await hud.locator('.kill-trade').evaluate(e=>{
        const n=e.querySelector('.kill-name'),s=getComputedStyle(e),name=getComputedStyle(n),a=getComputedStyle(document.querySelector('.kill-assist'));
        const color=selector=>getComputedStyle(document.querySelector(selector)).color;
        return {ellipsis:name.textOverflow==='ellipsis'&&n.scrollWidth>n.clientWidth,gap:s.gap===a.gap,padding:s.padding,border:s.borderLeftWidth,
            white:color('.kill-trade .status-icon')==='rgb(255, 255, 255)'&&color('.kill-assist > [aria-hidden]')==='rgb(255, 255, 255)',
            side:color('.kill-trade .kill-name')===color('.kill-attacker .kill-name')&&color('.kill-trade .kill-name')!==color('.kill-victim')};
    });
    assert(geometry.ellipsis&&geometry.gap&&geometry.padding==='0px'&&geometry.border==='0px'&&geometry.white&&geometry.side,JSON.stringify(geometry));
    await command({kind:'panel',values:{display:{hideTrades:true}}});
    await until(async()=>await hud.locator('.kill-trade').count()===0);
    assert.equal(await hud.locator('.kill-event').count(),1);
    await command({kind:'panel',values:{display:{hideTrades:false}}});
    await hud.locator('.kill-trade').waitFor();
    fixtureIndex=4;await command({kind:'step'});
    await until(async()=>await hud.locator('.clock > strong').textContent()==='0:00');
    assert.equal(await hud.locator('[data-kill-role="traded"]').textContent(),longName,'round review retains mapping');
    assert(await hud.evaluate(()=>window.tradeRow===document.querySelector('.kill-event')&&window.tradeAnimations.every(a=>document.querySelector('.kill-event').getAnimations().includes(a))),'updates retain DOM row and animations');
    await hud.screenshot({path:path.join(scratch,'trades.png')});
    await until(async()=>await hud.locator('.kill-event').count()===0,10000);
    assert(Date.now()-started<9500,'late mappings/panels/review do not extend original lifetime');
    assert.deepEqual(errors,[]);
    await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,errors,package:pkg,tests:['normal','late nested trade','flash assist','no assist','ordinary assist','alias update','literal text','ellipsis','panel hide/show','round review','stable DOM/animations','original lifetime']},null,2));
    console.log('PASS SDK trade indicators in published WASM; evidence: '+scratch);
}finally{
    if(origin&&control)try{await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}
    await browser?.close();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),output);await writeFile(path.join(scratch,'frames.json'),JSON.stringify(frames,null,2));
}
