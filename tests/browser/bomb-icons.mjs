import {sdkToolPath} from './sdk-tool.mjs';
import assert from 'node:assert/strict';
import {spawn,execFileSync} from 'node:child_process';
import {mkdtemp,readFile,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {chromium} from 'playwright';
const tool=path.resolve(sdkToolPath);
const pkg=path.resolve(process.argv[2]??'artifacts/package');
const scratch=await mkdtemp(path.resolve('artifacts/shared-bomb-icons-')),fixturePath=path.join(scratch,'fixture.json');
execFileSync('dotnet',[tool,'fixtures','create',fixturePath]);
const fixture=JSON.parse(await readFile(fixturePath,'utf8')),base=fixture.snapshots[0];
base.managedMatch={id:'bomb-icons-demo',name:'Bomb icons demo',bestOf:1,
    teamA:{id:'ct',name:'Demo CT',shortName:'CT',logo:null,seriesWins:0},teamB:{id:'t',name:'Demo T',shortName:'T',logo:null,seriesWins:0},
    ct:{side:'counterTerrorists',status:'manual',teamId:'ct',teamAScore:0,teamBScore:0},t:{side:'terrorists',status:'manual',teamId:'t',teamAScore:0,teamBScore:0}};
base.game.managedMatch=base.managedMatch;
const carrier=base.game.players.find(p=>p.side==='terrorists');
carrier.weapons.push({slot:'weapon_bomb',name:'weapon_c4',type:'bomb',state:'active',paintKit:null,ammoClip:null,ammoClipMax:null,ammoReserve:null,kind:'c4'});
for(const w of carrier.weapons)if(w.type!=='bomb')w.state='holstered';
base.game.observedPlayer=structuredClone(carrier);
base.game.bomb={state:'planting',playerSteamId:carrier.steamId,position:carrier.position,secondsRemaining:2.5};
base.game.roundPhase='live';base.game.countdown={phase:'live',secondsRemaining:90};base.game.killFeed=[];
const victim={entityIndex:1,steamId:base.game.players[0].steamId,name:'Victim',side:'counterTerrorists'};
const killer={entityIndex:6,steamId:carrier.steamId,name:'Named killer',side:'terrorists'};
const kill=(id,weapon,weaponKind,killer=null)=>({id,connectionId:'22222222-2222-2222-2222-222222222222',epoch:1,gameTimeSeconds:100+id,killer,victim,assister:null,weapon,weaponKind,flashAssist:false,headshot:false,wallbang:false,attackerBlind:false,throughSmoke:false,noScope:false});
const events=[kill(1,'planted_c4','c4'),kill(2,'trigger_hurt','unknown'),kill(3,'world','c4'),kill(4,'weapon_c4','c4',killer)];
fixture.snapshots=[structuredClone(base),...events.map(k=>{const s=structuredClone(base);s.game.bomb={state:'exploded',playerSteamId:null,position:null,secondsRemaining:0};s.game.killFeed=[k];return s;})];
fixture.loop=false;await writeFile(fixturePath,JSON.stringify(fixture));
const host=spawn('dotnet',[tool,'dev',pkg,'--port','0','--fixture',fixturePath],{stdio:['ignore','pipe','pipe']});
let output='',origin,browser,control;
host.stdout.on('data',v=>output+=v);host.stderr.on('data',v=>output+=v);
const until=async fn=>{const end=Date.now()+30000;while(!await fn()){if(Date.now()>end)throw Error('Timeout '+output);await new Promise(r=>setTimeout(r,25));}};
try{
    await until(()=>/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/.test(output));origin=output.match(/Development preview: (http:\/\/127\.0\.0\.1:\d+\/)/)[1];
    browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});
    control=await browser.newPage();await control.goto(origin);
    const command=async data=>{const r=await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data});assert(r.ok(),await r.text());};
    await command({kind:'pause'});
    const manifest=JSON.parse(await readFile(path.join(pkg,'hud.json'),'utf8'));
    const hud=await browser.newPage({viewport:{width:1920,height:1080}}),errors=[];
    hud.on('pageerror',e=>errors.push(e.message));
    const url=new URL(`hud-packages/${manifest.id}/${manifest.version}/`,origin).href;
    await hud.goto(url);
    await hud.locator('.bomb-glyph.shown[data-glyph="bomb"]').waitFor();
    for(const selector of ['.roster [data-ui-icon="bomb"]','.observed-loadout [data-ui-icon="bomb"]','.radar-bomb [data-ui-icon="bomb"]']){
        await hud.locator(selector).first().waitFor();
        assert.equal(await hud.locator(selector).first().locator('use').getAttribute('href'),'assets/icons/hud-ui.svg#bomb');
    }
    assert.equal(await hud.locator('[data-glyph="bomb"] use').getAttribute('href'),'assets/icons/hud-ui.svg#bomb');
    assert(await hud.locator('.radar-bomb [data-ui-icon="bomb"]').evaluate(e=>{
        const b=e.getBoundingClientRect(),p=e.parentElement.getBoundingClientRect();
        return b.width>0&&b.height>0&&b.width<=p.width+1&&b.height<=p.height+1;
    }),'radar bomb fits its small marker instead of inheriting the global icon minimum');
    assert.equal(await hud.locator('img[src$="/c4.svg"],img[src$="/planted_c4.svg"]').count(),0);
    await hud.screenshot({path:path.join(scratch,'carrier.png')});
    await hud.goto(url+'views/lineups');
    await hud.locator('.inventory [data-ui-icon="bomb"]').first().waitFor();
    assert.equal(await hud.locator('.inventory [data-ui-icon="bomb"]').first().locator('use').getAttribute('href'),'assets/icons/hud-ui.svg#bomb');
    await hud.goto(url);await hud.locator('.observed').waitFor();
    for(const event of events){
        await command({kind:'step'});
        const row=hud.locator(`.kill-event[data-event-id="${event.id}"]`);await row.waitFor();
        const bomb=event.weaponKind==='c4';
        assert.equal(await row.locator('[data-ui-icon="bomb"]').count(),bomb?1:0);
        assert.equal(await row.locator('[data-ui-icon="world"]').count(),bomb?0:1);
        assert(!/WORLD|TRIGGER HURT/.test(await row.textContent()));
        if(event.killer)assert.match(await row.textContent(),/Named killer/);
        else assert.equal(await row.locator('.kill-attacker .status-icon').evaluate(e=>getComputedStyle(e).color),'rgb(255, 255, 255)');
        await row.screenshot({path:path.join(scratch,`source-${event.id}.png`)});
    }
    assert.deepEqual(errors,[]);await writeFile(path.join(scratch,'result.json'),JSON.stringify({passed:true,package:pkg,errors},null,2));
    console.log('PASS shared bomb icon in matchbar, roster, observer, inventory, radar and killfeed; evidence: '+scratch);
}finally{
    if(origin&&control)try{await control.request.post(new URL('__dev/command',origin).href,{headers:{'X-SOU-DEV':'1'},data:{kind:'stop'},timeout:2000});}catch{}
    await browser?.close();if(host.exitCode===null)host.kill();await writeFile(path.join(scratch,'host.log'),output);
}
