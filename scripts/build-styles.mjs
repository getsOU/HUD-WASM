import { readFile, writeFile } from 'node:fs/promises';
import { watch } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { compile } from 'sass';

const root = new URL('../', import.meta.url);
const target = new URL('src/BasicHud/wwwroot/hud.css', root);
async function build() {
  const result = compile(fileURLToPath(new URL('src/BasicHud/Styles/hud.scss', root)), {
    style: 'expanded', sourceMap: false, charset: false
  });
  const expected = `/* Generated from Styles/hud.scss. Edit SCSS and run npm run styles:build. */\n${result.css}\n`;
  if (process.argv.includes('--check')) {
    if ((await readFile(target, 'utf8')).replaceAll('\r\n', '\n') !== expected) {
      throw new Error('HUD styles are stale. Run npm run styles:build.');
    }
    console.log('Compiled HUD styles are current.');
  } else {
    await writeFile(target, expected, 'utf8');
    console.log('Compiled Styles/hud.scss → wwwroot/hud.css');
  }
}
await build();
if (process.argv.includes('--watch')) {
  let pending;
  watch(new URL('src/BasicHud/Styles/', root), { recursive: true }, () => {
    clearTimeout(pending);
    pending = setTimeout(() => build().catch(error => console.error(error.message)), 80);
  });
  console.log('Watching SCSS sources. Press Ctrl+C to stop.');
}
