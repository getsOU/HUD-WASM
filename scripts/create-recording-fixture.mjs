import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
// Input is produced by `sou-hud fixtures create`; output uses only portable public contracts.
const template = JSON.parse(await readFile(process.argv[2] ?? 'artifacts/recording-template.json', 'utf8'));
const snapshot = structuredClone(template.snapshots[0]);
const matchId = 'recording-showcase-example', map = 'de_mirage';
const ids = snapshot.game.players.slice(0, 4).map(p => p.steamId);
const names = ['Example <b>text only</b> · Alexandra With A Very Long Player Name', 'Example Bravo', 'Example Charlie', 'Example Delta'];
const team = i => i < 2 ? 'example-alpha' : 'example-bravo';
const side = (i, round) => (i < 2) === (round <= 3) ? 'counterTerrorists' : 'terrorists';
const time = (round, offset = 0) => new Date(Date.UTC(2026, 10, 10, 8, round * 2, offset)).toISOString();
const weapon = (i, empty = false) => empty ? [] : [{ slot: 'weapon_0', name: i < 2 ? 'weapon_m4a1' : 'weapon_ak47', kind: i < 2 ? 'm4A4' : 'ak47', type: 'rifle', state: 'active', paintKit: null, ammoClip: i === 0 ? 0 : 20, ammoClipMax: 30, ammoReserve: 60 }, { slot: 'weapon_1', name: 'weapon_flashbang', kind: 'flashbang', type: 'grenade', state: 'holstered', paintKit: null, ammoClip: 1, ammoClipMax: null, ammoReserve: null }];
const match = { id: matchId, name: 'SDK EXAMPLE · Recording & Economy', bestOf: 1,
    teamA: { id: 'example-alpha', name: 'Example Alpha', shortName: 'ALP', logo: null, seriesWins: 0 },
    teamB: { id: 'example-bravo', name: 'Example Bravo', shortName: 'BRV', logo: null, seriesWins: 0 },
    ct: { side: 'counterTerrorists', status: 'manual', teamId: 'example-bravo', teamAScore: 2, teamBScore: 3 },
    t: { side: 'terrorists', status: 'manual', teamId: 'example-alpha', teamAScore: 2, teamBScore: 3 },
    maps: [{ id: 'example-map', name: 'Mirage', gameName: map, number: 1, kind: 'defuse', status: 'live', scoreA: 2, scoreB: 3, winnerTeamId: null }] };
snapshot.managedMatch = match;
snapshot.game.managedMatch = match;
snapshot.game.map = map; snapshot.game.roundNumber = 6; snapshot.game.roundPhase = 'over';
snapshot.game.players = snapshot.game.players.slice(0, 4).map((p, i) => ({ ...p, name: names[i], side: side(i, 6) }));
snapshot.game.observedPlayer = snapshot.game.players[0];
snapshot.game.killFeed = []; snapshot.game.radar = null;
const history = { schemaVersion: 1, matchId, map, status: 'available', revision: 'example-raw-1', updatedAt: time(6, 55), rounds: [], gaps: [{ at: time(3), reason: 'sourceGap' }] };
const analysis = { ...history, revision: 'example-analysis-2', buyRuleVersion: 'equipment-v1', players: [], rounds: [] };
const economy = { ...history, revision: 'example-economy-3', rounds: [] };
for (let round = 1; round <= 6; round++) {
    const players = ids.map((steamId, i) => ({ steamId, name: names[i], side: round === 6 && i === 0 ? null : side(i, round), teamId: team(i), health: 100,
        money: (round === 1 && i < 2) || (round === 2 && i === 0) ? 0 : round === 3 && i === 1 ? null : 800 + round * 350 + i * 100,
        equipmentValue: round === 2 && i === 0 ? 0 : round === 3 && i === 1 ? null : round % 2 ? 3000 : 4500,
        armor: i === 0 ? 0 : 100, helmet: i === 0 ? false : i === 1 ? null : true, defuseKit: i === 0 ? false : i === 1 ? null : side(i, round) === 'counterTerrorists', weapons: round === 3 && i === 1 ? null : weapon(i, round === 2 && i === 0) }));
    const checkpoint = (kind, offset, source = players) => ({ kind, receivedAt: time(round, offset), sourceTimestamp: null, ctScore: round - 1, tScore: 0,
        ctTeamId: round <= 3 ? 'example-alpha' : 'example-bravo', tTeamId: round <= 3 ? 'example-bravo' : 'example-alpha', players: structuredClone(source) });
    const end = players.map((p, i) => ({ ...p, health: i % 2 === 0 ? 100 : 0, equipmentValue: i % 2 === 0 ? p.equipmentValue : 0, weapons: i % 2 === 0 ? p.weapons : [] }));
    const checkpoints = [checkpoint('freezeStart', 0, players.map(p => ({ ...p, money: p.money === null ? null : round === 2 && p.steamId === ids[1] ? p.money - 200 : p.money + 1500 }))), checkpoint('freezeLast', 10), checkpoint('liveStart', 15)];
    if (round !== 3) checkpoints.push(checkpoint('roundEnd', 55, end));
    history.rounds.push({ number: round, status: round === 3 ? 'incomplete' : 'ended', checkpoints });
    const analyzed = players.map((p, i) => ({ steamId: p.steamId, name: p.name, side: p.side, teamId: p.teamId,
        kills: round === 3 ? null : i === 0 ? [2, 3, 0, 4, 5, 0][round - 1] : 0,
        headshotSampleKills: round === 3 || (round === 2 && i === 0) ? null : i === 0 ? [2, 3, 0, 4, 5, 0][round - 1] : 0,
        headshotKills: round === 3 || (round === 2 && i === 0) ? null : i === 0 && round < 3 ? 1 : 0, damage: round === 3 ? null : i === 0 ? 100 : 0,
        buyClass: p.equipmentValue === null ? 'unknown' : p.equipmentValue < 2000 ? 'eco' : p.equipmentValue < 4000 ? 'light' : 'full',
        buyEquipmentValue: p.equipmentValue, survived: round === 3 ? null : i % 2 === 0,
        savedEquipmentValue: round === 3 ? null : end[i].equipmentValue, savedWeapons: round === 3 ? null : end[i].weapons }));
    const teams = ['counterTerrorists', 'terrorists'].map(s => {
        const group = analyzed.filter(p => p.side === s), raw = players.filter(p => p.side === s);
        const sum = key => raw.every(p => p[key] !== null) ? raw.reduce((a, p) => a + p[key], 0) : null;
        const classes = new Set(group.map(p => p.buyClass));
        return { side: s, teamId: raw[0].teamId, buyPlayers: raw.length, money: sum('money'), equipmentValue: sum('equipmentValue'),
            buyClass: classes.has('unknown') ? 'unknown' : classes.size > 1 ? 'mixed' : [...classes][0], endPlayers: round === 3 ? 0 : group.length,
            survivors: round === 3 ? null : group.filter(p => p.survived).length,
            savedEquipmentValue: round === 3 ? null : group.reduce((a, p) => a + p.savedEquipmentValue, 0) };
    });
    analysis.rounds.push({ number: round, status: round === 3 ? 'incomplete' : 'ended', buyReceivedAt: time(round, 10), endReceivedAt: round === 3 ? null : time(round, 55), players: analyzed, teams });
    const events = round === 3 ? [] : [
        { id: `example-${round}-purchase`, receivedAt: time(round, 3), gameTimeSeconds: round * 120 + 3, phase: 'freezeTime', kind: 'purchase', steamId: ids[0], item: 'weapon_m4a1', evidence: 'confirmed' },
        { id: `example-${round}-pickup`, receivedAt: time(round, 4), gameTimeSeconds: round * 120 + 4, phase: 'freezeTime', kind: 'pickup', steamId: ids[1], item: 'weapon_m4a1', evidence: 'confirmed' },
        { id: `example-${round}-removal`, receivedAt: time(round, 5), gameTimeSeconds: round * 120 + 5, phase: 'freezeTime', kind: 'removal', steamId: ids[1], item: 'weapon_m4a1', evidence: 'confirmed' }
    ];
    if (round === 1) for (const i of [1, 2]) events.push({ ...events[0], id: `example-1-purchase-${i}`, steamId: ids[i] });
    events.sort((a, b) => a.gameTimeSeconds - b.gameTimeSeconds);
    for (const event of events) event.id = createHash('sha256').update(event.id).digest('hex');
    economy.rounds.push({ number: round, status: round === 3 ? 'incomplete' : 'ended', source: round === 3 ? null : 'hlae', eventCoverage: 'unknown', hasGap: round === 3, events,
        players: players.map((p, i) => ({ steamId: p.steamId, name: p.name, side: p.side,
            observedPurchases: round === 3 ? null : i === 0 || (round === 1 && i < 3) ? 1 : 0, observedPickups: round === 3 ? null : i === 1 ? 1 : 0,
            observedRemovals: round === 3 ? null : i === 1 ? 1 : 0, purchaseEvidence: round !== 3 && (i === 0 || (round === 1 && i < 3)) ? 'confirmed' : 'unknown',
            refundEvidence: round === 2 && i === 1 ? 'derived' : 'unknown', observedCashDecrease: round === 3 ? null : round === 2 && i === 1 ? 0 : 1500,
            observedCashIncrease: round === 3 ? null : round === 2 && i === 1 ? 200 : 0,
            cashFlow: round === 2 && i === 1 ? { decrease: 1000, increase: 1200, changes: 3, hasGap: false } : null,
            carriedWeapons: round === 5 && i % 2 === 0 ? [{ item: i < 2 ? 'weapon_m4a1' : 'weapon_ak47', fromRound: 4, evidence: i === 0 ? 'confirmed' : 'derived', weaponIdentity: i === 0 ? 'a'.repeat(64) : null }] : [] })),
        teams: ['counterTerrorists', 'terrorists'].map(s => ({ side: s, ruleVersion: 'force-observed-v1', classification: round === 1 ? s === 'counterTerrorists' ? 'forceLikely' : 'noForceSignal' : 'unknown', evidence: round === 1 ? 'derived' : 'unknown', players: players.filter(p => p.side === s).length,
            firstObservedMoney: round === 1 ? s === 'counterTerrorists' ? 3000 : 5800 : null, lastObservedMoney: round === 1 ? s === 'counterTerrorists' ? 0 : 2800 : null, observedCashDecrease: round === 1 ? 3000 : null,
            equipmentValue: round === 1 ? 6000 : null, observedBuyers: round === 1 ? s === 'counterTerrorists' ? 2 : 1 : null })) });
}
// Deliberately independent samples. Zero damage with covered rounds is a real ADR zero.
for (const [i, steamId] of ids.entries()) {
    for (const s of ['counterTerrorists', 'terrorists', null]) {
        const rounds = analysis.rounds.filter(r => r.players[i].side === s);
        if (rounds.length === 0) continue;
        const covered = rounds.filter(r => r.players[i].kills !== null);
        const kills = covered.reduce((n, r) => n + r.players[i].kills, 0);
        const headshotCovered = rounds.filter(r => r.players[i].headshotSampleKills !== null);
        const hsKills = headshotCovered.reduce((n, r) => n + r.players[i].headshotSampleKills, 0);
        const hs = headshotCovered.reduce((n, r) => n + r.players[i].headshotKills, 0);
        const damage = covered.reduce((n, r) => n + r.players[i].damage, 0);
        const bucket = n => covered.filter(r => n === 5 ? r.players[i].kills >= 5 : r.players[i].kills === n).length;
        analysis.players.push({ steamId, name: names[i], side: s, rounds: rounds.length, killRounds: covered.length, kills,
            headshotRounds: headshotCovered.length, damageRounds: covered.length, headshotSampleKills: hsKills, headshotKills: hs, damage,
            headshotPercentage: hsKills > 0 ? 100 * hs / hsKills : null, averageDamagePerRound: covered.length > 0 ? damage / covered.length : null,
            coversObservedRounds: covered.length === rounds.length && headshotCovered.length === rounds.length, doubleKills: bucket(2), tripleKills: bucket(3), quadKills: bucket(4), fivePlusKills: bucket(5) });
    }
}
await mkdir('tests/fixtures', { recursive: true });
const nextContext = structuredClone(snapshot);
nextContext.managedMatch.id = 'recording-showcase-context-two';
nextContext.managedMatch.name = 'SDK EXAMPLE · Context two';
nextContext.managedMatch.maps[0] = { ...nextContext.managedMatch.maps[0], id: 'example-map-two', name: 'Dust2', gameName: 'de_dust2' };
nextContext.game.managedMatch = nextContext.managedMatch;
nextContext.game.map = 'de_dust2';
await writeFile('tests/fixtures/recording-showcase.json', JSON.stringify({ schemaVersion: 1, intervalMilliseconds: 5000, loop: false, snapshots: [snapshot, nextContext], recordingSnapshot: { schemaVersion: 1, matchId, map, status: "available", revision: createHash("sha256").update(JSON.stringify([history,analysis,economy])).digest("hex"), updatedAt: time(6,55), roundHistory: history, mapAnalysis: analysis, economyHistory: economy } }, null, 2) + '\n');
