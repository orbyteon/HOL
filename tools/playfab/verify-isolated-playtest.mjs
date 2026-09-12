import assert from 'node:assert/strict';
import fs from 'node:fs';
import { pathToFileURL } from 'node:url';

export function verifyManifest(m) {
  assert.equal(m.titleId, '11CB9E');
  assert.equal(m.packageId, 'com.Orbyteon.HOL.pvptest');
  assert.equal(m.productName, 'HOL PvP Test');
  assert.equal(m.unityVersion, '2022.3.62f3');
  assert.equal(m.cloudScriptSha256, '121556521FF8633DE5035FD8462C50D85A80363539211FB7F318C4D470BEABF0');
  assert.equal(m.development, true);
  assert.equal(m.operatorProvisioningOnly, true);
  assert.deepEqual(m.scenes, ['Assets/Scenes/SplashScene.unity', 'Assets/Scenes/MainMenu.unity']);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  assert.equal(process.argv.length, 3, 'Pass exactly the build manifest path');
  verifyManifest(JSON.parse(fs.readFileSync(process.argv[2], 'utf8')));
  console.log('Verified isolated HOL PvP Test build manifest: 11CB9E, no production provisioning.');
}
