import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';

const manifest = JSON.parse(readFileSync(new URL('../../.config/dotnet-tools.json', import.meta.url), 'utf8'));
export const sdkVersion = manifest.tools['sou.hud.tool'].version;
export const sdkToolPath = fileURLToPath(new URL('../../artifacts/published-sdk-tools/.store/sou.hud.tool/' + sdkVersion + '/sou.hud.tool/' + sdkVersion + '/tools/net10.0/any/SOU.Hud.Tool.dll', import.meta.url));
