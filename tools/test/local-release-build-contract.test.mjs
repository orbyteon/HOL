import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';

const read = p => fs.readFileSync(new URL('../../' + p, import.meta.url), 'utf8');
const source = read('Assets/Editor/HolLocalReleaseBuild.cs');
const meta = read('Assets/Editor/HolLocalReleaseBuild.cs.meta');

// Source-contract checks for the previously built Windows-only Editor helper.
// These do not claim a new Unity build, live login or signing test.
test('local AAB helper keeps stable meta, public config empty, and no machine credentials', () => {
  assert.match(meta, /^guid: 45349d05bc2e4b228c8620122f68bfc4\r?$/m);
  assert.deepEqual(JSON.parse(read('Assets/Resources/HOLReleaseConfig.json')),
    { playFabTitleId: '', provisioningUrl: '', googleCloudProjectNumber: 0 });
  assert.doesNotMatch(source, /C:\\Users\\|ridecore|195DDC|X-SecretKey|PLAYFAB_DEV_SECRET_KEY/);
  assert.match(source, /Environment\.GetEnvironmentVariable\(name\)/);
  assert.match(source, /-storepass:env HOL_LOCAL_CERT_CHECK_PASS/);
  assert.doesNotMatch(source, /-storepass\s*\\?"?\s*\+/);
});

test('local AAB path preserves release guard and blocks test targets and wrong scene order', () => {
  assert.match(source, /cfg\.playFabTitleId == PvpIsolatedPlaytest\.TitleId/);
  assert.match(source, /PvpIsolatedPlaytest\.Enabled/);
  assert.match(source, /HOL_PVP_ISOLATED_PLAYTEST/);
  assert.match(source, /Contains\("-holReleaseBuild"\)/);
  assert.match(source, /Application\.unityVersion != "2022\.3\.62f3"/);
  assert.match(source, /"Assets\/Scenes\/SplashScene\.unity", "Assets\/Scenes\/MainMenu\.unity"/);
  assert.match(source, /SequenceEqual\(Scenes\)/);
  assert.match(source, /EditorUserBuildSettings\.activeBuildTarget != BuildTarget\.Android/);
});

test('local AAB validates external paths, key identity and non-debuggable package settings', () => {
  assert.match(source, /path\.StartsWith\(project, StringComparison\.OrdinalIgnoreCase\)/);
  assert.match(source, /output\.StartsWith\(project, StringComparison\.OrdinalIgnoreCase\) \|\| File\.Exists\(output\)/);
  assert.match(source, /output\.EndsWith\("\.aab"/);
  assert.match(source, /output\.Contains\("PrivateKeyEntry"\)/);
  assert.match(source, /expected, StringComparison\.OrdinalIgnoreCase/);
  assert.match(source, /const string Package = "com\.Orbyteon\.HOL"/);
  assert.match(source, /ScriptingImplementation\.IL2CPP/);
  assert.match(source, /AndroidArchitecture\.ARMv7 \| AndroidArchitecture\.ARM64/);
  assert.match(source, /AndroidSdkVersions\.AndroidApiLevel36/);
  assert.match(source, /EditorUserBuildSettings\.development = false/);
  assert.match(source, /options = BuildOptions\.None/);
});

test('local AAB restores its temporary payload and settings, and never publishes', () => {
  const finalizer = source.slice(source.lastIndexOf('finally'));
  for (const expression of [
    'File.WriteAllBytes(PayloadPath, originalPayload)',
    'PlayerSettings.SetApplicationIdentifier(group, originalPackage)',
    'PlayerSettings.bundleVersion = originalVersion',
    'PlayerSettings.Android.bundleVersionCode = originalCode',
    'PlayerSettings.SetScriptingBackend(group, originalBackend)',
    'PlayerSettings.Android.targetArchitectures = originalArchitectures',
    'PlayerSettings.Android.targetSdkVersion = originalSdk',
    'EditorUserBuildSettings.buildAppBundle = originalBundle',
    'EditorUserBuildSettings.development = originalDevelopment',
    'PlayerSettings.Android.keystoreName = originalKeyPath',
    'PlayerSettings.Android.keyaliasName = originalAlias',
    'PlayerSettings.Android.keystorePass = originalStorePass',
    'PlayerSettings.Android.keyaliasPass = originalKeyPass',
    'PlayerSettings.Android.useCustomKeystore = originalCustom',
  ]) assert.ok(finalizer.includes(expression), expression);
  assert.doesNotMatch(source, /UnityWebRequest|HttpClient|UploadCloudScript|PlayerPrefs\./);
  const workflow = read('.github/workflows/build-release.yml');
  assert.match(workflow, /refs\/heads\/main/);
  assert.match(workflow, /environment: production/);
  assert.match(workflow, /Confirmation must be exactly BUILD/);
  assert.match(workflow, /customParameters: -holReleaseBuild/);
});
