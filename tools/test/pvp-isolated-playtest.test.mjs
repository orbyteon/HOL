import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import { verifyManifest } from '../playfab/verify-isolated-playtest.mjs';

const read = path => fs.readFileSync(new URL('../../' + path, import.meta.url), 'utf8');
const runtime = read('Assets/SCRIPT/PvP/PvpIsolatedPlaytest.cs');
const client = read('Assets/SCRIPT/PvP/PlayFabPvpClient.cs');
const builder = read('Assets/Editor/MainMenuPreviewBuild.cs');
const workflow = read('.github/workflows/private-room-android-preview.yml');
const valid = () => ({
  titleId: '11CB9E', packageId: 'com.Orbyteon.HOL.pvptest', productName: 'HOL PvP Test',
  unityVersion: '2022.3.62f3', development: true, operatorProvisioningOnly: true,
  cloudScriptSha256: 'EBB9DEE03FE4D147E63B555DA36EA5D56AAFEE85DB91F72BC56938B7963DEEB5',
  scenes: ['Assets/Scenes/SplashScene.unity', 'Assets/Scenes/MainMenu.unity'],
});

test('isolated APK manifest accepts only the exact non-production target and pinned server', () => {
  verifyManifest(valid());
  for (const patch of [
    { titleId: '195DDC' }, { titleId: '23E1A' }, { titleId: '' },
    { packageId: 'com.Orbyteon.HOL' }, { development: false },
    { operatorProvisioningOnly: false }, { unityVersion: '2022.3.61f1' },
    { cloudScriptSha256: 'wrong' }, { scenes: ['Assets/Scenes/MainMenu.unity'] },
  ]) assert.throws(() => verifyManifest({ ...valid(), ...patch }));
});

test('isolated build uses a temporary define and separate installation without touching saves', () => {
  assert.match(builder, /options\.extraScriptingDefines = new\[\] \{ "HOL_PVP_ISOLATED_PLAYTEST" \}/);
  assert.match(builder, /BuildPreview\(false\)/);
  assert.match(builder, /BuildPreview\(true\)/);
  assert.equal((builder.match(/new BuildPlayerOptions/g) || []).length, 1);
  assert.match(builder, /BuildOptions\.Development/);
  assert.match(builder, /PlayerSettings\.SetApplicationIdentifier\(group, previousPackage\)/);
  assert.match(builder, /PlayerSettings\.productName = previousProduct/);
  assert.match(builder, /PvpIsolatedPlaytest\.CloudScriptSha256/);
  assert.doesNotMatch(builder, /SetScriptingDefineSymbols/);
  assert.match(runtime, /#if HOL_PVP_ISOLATED_PLAYTEST/);
  assert.match(runtime, /SystemInfo\.deviceUniqueIdentifier/);
  assert.doesNotMatch(runtime, /PlayerPrefs|X-SecretKey|SessionTicket|UnityWebRequest/);
});

test('all PlayFab transport requests and both provisioning paths use the isolation guard', () => {
  assert.match(client, /PvpIsolatedPlaytest\.AllowsClientCreation\(PvpIsolatedPlaytest\.Enabled/);
  assert.match(client, /PvpIsolatedPlaytest\.AllowsProductionProvisioning\(\s*PvpIsolatedPlaytest\.Enabled/);
  const post = client.slice(client.indexOf('IEnumerator PostOnce('));
  assert.ok(post.indexOf('PvpIsolatedPlaytest.AllowsRequest') < post.indexOf('new UnityWebRequest'));
  assert.match(post, /HOL_PVP_TEST_TARGET_REJECTED[\s\S]*yield break/);
  assert.match(read('Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs'),
    /backend\.titleId = playFabTitleId;\s*PvpIsolatedPlaytest\.Configure\(backend\)/);
  assert.deepEqual(JSON.parse(read('Assets/Resources/HOLReleaseConfig.json')),
    { playFabTitleId: '', provisioningUrl: '', googleCloudProjectNumber: 0 });
});

test('manual isolated preview preserves green-CI and merge-ref guards without captures', () => {
  assert.match(workflow, /isolated_playtest:[\s\S]*type: boolean\s*default: false/);
  assert.match(workflow, /uses: \.\/.github\/actions\/require-ci-green/);
  assert.match(workflow, /uses: \.\/.github\/actions\/resolve-pr-merge-ref/);
  assert.match(workflow, /capture:\s*if: \$\{\{ !inputs\.isolated_playtest \}\}/);
  assert.match(workflow, /MainMenuPreviewBuild\.BuildIsolatedPvpPlaytest/);
  assert.match(workflow, /hol-pvp-test-11CB9E-development/);
  assert.doesNotMatch(workflow, /PLAYFAB_DEV_SECRET_KEY|PLAYFAB_SECRET_KEY|environment: production/);
});
