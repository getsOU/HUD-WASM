# Hybrides Icon-System

Das freigegebene Kontur-System gilt jetzt im gesamten HUD: Live-Playercards, Observercard, Killfeed, Rundenhistorie sowie Inventare und Playercards in Lineups und Spotlight. Beide Teams und beide Roster-Anordnungen verwenden dieselben semantischen Symbole.

## Struktur und Regeln

- `Components/StatusIcon.razor` leitet bekannte UI-Symbole an `HudUiIcon.razor` weiter. `Presentation/HudUiIcons.cs` enthält die gemeinsame Zuordnung. C4 behält das Original; unbekannte Statuswerte erhalten ihre Textbezeichnung.
- `EquipmentIcon.razor` verwendet für die sechs bekannten Granatenschlüssel dieselben Konturen wie `UtilityIcon.razor` und `UtilityIcons.razor`. Waffen, Messer, Zeus, C4 und unbekannte Ausrüstung behalten ihre ursprünglichen Grafiken und den vorhandenen Fehler-/Text-Fallback.
- `Ammunition.razor` zeigt geladene Patronen und Reserve mit den beiden Munitionsicons in Observercard und Inventar. Nicht gelieferte oder negative Werte entfallen einzeln; eine gemeldete Null bleibt sichtbar. Messer und Granaten erhalten keine Munitionsanzeige.
- Das lokale Sprite `wwwroot/assets/icons/hud-ui.svg` enthält 23 Symbole: zehn unveränderte Lucide-Geometrien und 13 eigene CS2-Symbole. Neue Icons verwenden 24×24, `fill="none"`, `stroke="currentColor"`, Strichstärke 2 und runde Linienenden/Übergänge. `--hud-ui-stroke` bleibt anpassbar. Der quadratische Rahmen ist mindestens 12 Pixel groß; optischer Original-Zuschnitt und Weißfilter werden auf Konturen nicht angewendet.
- In Player- und Observercards sind Status-, Utility- und Munitionsicons teamfarben. Schrift und Zahlen bleiben weiß, nur Geldbeträge grün. Geld erscheint als `$ Zahl` mit einem Leerzeichen und teamfarbenem Dollarzeichen; fehlende Werte bleiben ein weißer Gedankenstrich. Waffen, Kit, Bombe und Kevlar/Helm bleiben in den Karten weiß. Im Rundenverlauf übernehmen dagegen alle Ergebnis-Icons die CT-/T-Farbe der Gewinnerseite, auch Kit und C4. Das Original-C4 wird ausschließlich dort per lokaler CSS-Maske gefärbt; seine SVG-Datei und sein Ladefehler-Fallback bleiben erhalten. Die Killfeed-Farben bleiben bestehen.
- Keine neuen Laufzeitabhängigkeiten, CDN-Aufrufe, Icon-Fonts oder JavaScript-Icon-Renderer. Lizenz und Herkunft stehen in `LICENSES/LUCIDE-ICONS.md`, `LUCIDE-LICENSE.txt` und dem hashgesicherten Inventar `hud-ui-icons.json`.

## Vollständige Zuordnung

| Bedeutung | Bisheriges Asset | Gemeinsames Icon | Verwendung |
| --- | --- | --- | --- |
| Gesundheit | `ui/health.svg` | Lucide `heart` → `health` | Zugeordnet, aktuell ohne sichtbaren Aufruf; Karten zeigen HP als Zahl |
| Kills | `ui/crosshair_circle.svg` | Lucide `crosshair` → `kills` | Live-/Detail-Playercard, Observercard |
| Deaths / tot | `ui/skull.svg` | Lucide `skull` → `dead` | K/D, vertikaler Todesindikator, Detailkarte, Rundenhistorie |
| Kevlar | `ui/armor.svg` | Lucide `shield` → `armor` | Player-/Observercards; weiß und nur bei positiver Armor |
| Kevlar + Helm | `weapons/armor_helmet.svg` | Eigene `helmet` auf Lucide-Schildbasis | Player-/Observercards; weiß und nur bei positiver Armor |
| Defuse-Kit | `ui/defuser_white.svg` | Eigene `kit`: schlichte Zange | Playercards weiß; Rundenhistorie in Gewinnerfarbe |
| Timer / Zeitablauf | `ui/clock.svg` | Lucide `clock` → `time` | Rundenhistorie |
| Geblendeter Angreifer | `killfeed/blind_kill.svg` | Lucide `eye-off` → `blind` | Killfeed beim Angreifer |
| Kill durch Smoke | `killfeed/smoke_kill.svg` | Lucide `cloud-fog` → `smoke` | Killfeed beim Kill |
| Flash-Assist | `weapons/flashbang_assist.svg` | Lucide `zap` → `flashassist` | Killfeed beim vorhandenen Assister |
| Feuerstatus | `ui/burning.svg` | Lucide `flame` → `fire` | Zugeordnet, weiterhin ohne zusätzlichen Indikator; Avataranimation bleibt bestehen |
| Geiselrettung | `ui/hostage_rescued.svg` | Lucide `person-standing` → `hostage` | Rundenhistorie bei entsprechendem Ergebnis |
| Headshot | `killfeed/icon_headshot.svg` | Eigene `headshot`: Totenkopf mit Trefferstern an der Schläfe | Killfeed |
| Wallbang | `killfeed/penetrate.svg` | Eigene `wallbang`: Projektil durch Wand | Killfeed |
| No-Scope | `killfeed/noscope.svg` | Eigene `noscope`: durchgestrichenes Visier | Killfeed |
| HE | `weapons/hegrenade.svg` | Eigene `utility-hegrenade` | Utility-Reihe, aktive Granate, Inventar, Killfeed |
| Flashbang | `weapons/flashbang.svg` | Eigene `utility-flashbang` | Utility-Reihe, aktive Granate, Inventar, Killfeed |
| Smoke | `weapons/smokegrenade.svg` | Eigene `utility-smokegrenade` | Utility-Reihe, aktive Granate, Inventar, Killfeed |
| Decoy | `weapons/decoy.svg` | Eigene `utility-decoy` | Utility-Reihe, aktive Granate, Inventar, Killfeed |
| Molotov | `weapons/molotov.svg` | Eigene `utility-molotov` | Utility-Reihe, aktive Granate, Inventar, Killfeed |
| Brandgranate | `weapons/incgrenade.svg` | Eigene `utility-incgrenade` | Utility-Reihe, aktive Granate, Inventar, Killfeed |
| Geladene Munition | Bisher Zahl | Eigene `rounds`: zwei Patronen | Observercard und Inventar: Zahl vor Icon |
| Reservemunition | Bisher Zahl | Eigene `magazine`: Magazin | Observercard und Inventar: Zahl vor Icon, breiterer Abstand zur geladenen Munition |
| C4 / Bombenträger | `weapons/c4.svg`, `weapons/planted_c4.svg` | Original | Weiß; nur als Rundenergebnis in Gewinnerfarbe |
| Waffen, Messer, Zeus | `weapons/*.svg` | Original | Unverändert, weiß |

Eigene CS2-Icons erhalten die Unterschiede zwischen Kevlar und Kevlar mit Helm sowie Defuse-Kit, Headshot, Wallbang und No-Scope. Alle sechs Utility-Icons besitzen dieselbe sichtbare Höhe und gleich große Rahmen. SDK-Reihenfolge, doppelte Flashbangs und Mengen bleiben erhalten; tote Spieler zeigen kein veraltetes Inventar.

## Unverändert

Die 121 ursprünglichen SVG-Dateien bleiben bytegleich mit ihren Herkunftsnachweisen erhalten: 102 Waffen-/Ausrüstungsdateien, neun UI-Dateien und zehn Killfeed-Dateien. Alte Status- und Utility-Dateien dienen nicht mehr als aktive Darstellung bekannter UI-Symbole. `Presentation/Cs2Icons.cs` dokumentiert weiterhin die Legacy-Quellen und liefert insbesondere das Original-C4.

Die 23 Radar-WebPs, Karten, Projektionen, Spielerpositionen, Blickrichtungen, Wirkungsflächen und Animationen werden nicht geändert. Dasselbe gilt für die Maskierung der Avatar-Effekte. Teamlogos, Agentenporträts, Wettbewerbslogo und Sponsoren bleiben Bildmedien. Die Reserve-Assets für Assists, Suicide, Domination, Revenge, In-Air-Kill und Smoke-Impact bleiben ungenutzt; neue Ereignisse werden nicht erfunden.

Datenbindungen, Killfeed-Reihenfolge und vollständige verwaltete Namen bleiben erhalten. Rundenhistorie zeigt dieselben Resultate und Phasen. Die produktiven Bereiche behalten ihre getrennten SDK-Abonnements; reine Positionsänderungen aktualisieren keine Playercards.

## Kartenlayout und Vorschau

Die Observercard behält Namen und Armor/HP in der oberen Infozeile. Unten stehen K/D links und die engere Gruppe aus weißer Waffe und Munition rechts. Die Waffe ist auf 5cqw Breite und 1.35cqw Höhe begrenzt und behält ihre Proportionen. Das weiße Kevlar-/Helm-Icon steht links neben HP, ohne separate Armor-Zahl oder Health-Icon. Horizontale tote Playercards bleiben kleiner, ohne großen Totenkopf, und unten ausgerichtet.

Die Debug-Vorschau unter `http://127.0.0.1:5178/?preview=live&portraits=missing` zeigt unterschiedliche Utility-Kombinationen mit zwei bis vier Granaten und CT-Brandgranaten/T-Molotovs. Aster (Slot 1) und Sable (9) haben nur Kevlar; Orion (3) und Flint (6) haben keine Rüstung. Die MVP-Vorschau wechselt in der Observercard zwischen Aster und Flint.

Der Vorschau-Killfeed enthält zehn gleichzeitig sichtbare Beispiele: Ein AWP-Kill zeigt alle sechs unterstützten Modifikatoren (geblendeter Angreifer, Flash-Assist, Smoke, No-Scope, Wallbang, Headshot); weitere Zeilen zeigen AK, Messer, HE, Flashbang, Smoke, Decoy, Molotov, Brandgranate und C4/WORLD. Nur die synthetischen Ereignis-IDs werden alle sechs Sekunden erneuert, damit die Beispiele beim normalen Ablauf nach acht Sekunden nicht verschwinden. Das produktive Killfeed-Verhalten bleibt unverändert.

`http://127.0.0.1:5178/?preview=icons&portraits=missing` ergänzt eine feste Freeze-Time mit allen fünf Rundenergebnissen: Elimination, Bombenexplosion, Entschärfung, Zeitablauf und Geiselrettung. Beide Seiten gewinnen Beispielrunden; jedes Icon folgt der zugehörigen Gewinnerfarbe. Die reguläre Live-Vorschau behält ihre laufende Runde.

Rendering-Prüfungen decken Statussymbole, Killfeed-Modifikatoren, Original-C4, unbekannte Assets, Utility-Mengen und fehlende/negative/Null-Munition ab. Browserprüfungen prüfen sichtbare SVG-Geometrie, CSS-Farbe, Mindestgröße sowie die unveränderte Radar- und SDK-Aktualisierungslogik. Die Herkunftsinventare sichern die Originaldateien und das neue Sprite mit SHA-256 ab.
