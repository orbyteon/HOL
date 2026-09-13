import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { requireSuccessfulMainPush, verifyRelease } from '../release/require-release-ci.mjs';

const sha = 'a'.repeat(40);
const run = (patch = {}) => ({id: 1, head_sha: sha, head_branch: 'main', event: 'push', status: 'completed', conclusion: 'success', ...patch});

test('release gate accepts only successful main push evidence for the exact SHA', () => {
  assert.equal(requireSuccessfulMainPush([run()], sha), 1);
  for (const patch of [{head_sha: 'b'.repeat(40)}, {head_branch: 'feature'},
    {event: 'pull_request'}, {conclusion: 'failure'}, {status: 'in_progress'}, {conclusion: 'cancelled'}])
    assert.throws(() => requireSuccessfulMainPush([run(patch)], sha));
  assert.throws(() => requireSuccessfulMainPush([], sha));
  assert.throws(() => requireSuccessfulMainPush([run(), run({id: 2, status: 'queued', conclusion: null})], sha));
});

test('release verification requires both workflows and fails closed on API errors', async () => {
  const env = {RELEASE_SHA: sha, GITHUB_REF: 'refs/heads/main', GITHUB_REPOSITORY: 'orbyteon/HOL', GH_TOKEN: 'test-token'};
  const urls = [];
  const request = async url => { urls.push(url); return {ok: true, json: async () => ({workflow_runs: [run()]})}; };
  assert.equal((await verifyRelease(env, request)).length, 2);
  assert.ok(urls.some(url => url.includes('production-visual-integrity.yml')));
  await assert.rejects(verifyRelease({...env, GITHUB_REF: 'refs/heads/feature'}, request));
  await assert.rejects(verifyRelease(env, async () => ({ok: false, status: 403})));
  await assert.rejects(verifyRelease(env, async () => ({ok: true, json: async () => ({})})));
});

test('every release entry point runs the gate before its side effects', () => {
  for (const name of ['build-release', 'deploy-playfab', 'deploy-provisioner', 'set-min-version', 'publish-internal-test']) {
    const source = fs.readFileSync(new URL(`../../.github/workflows/${name}.yml`, import.meta.url), 'utf8');
    assert.match(source, /actions: read/);
    assert.match(source, /RELEASE_SHA: \$\{\{ github.sha \}\}/);
    const gate = source.indexOf('run: node tools/release/require-release-ci.mjs');
    assert.ok(gate > source.indexOf('uses: actions/checkout@'));
    for (const action of ['run: node tools/playfab/deploy-cloudscript.mjs', 'uses: azure/login@', 'uses: game-ci/unity-builder@', 'run: node tools/playfab/set-min-version.mjs']) {
      if (source.includes(action)) assert.ok(gate < source.indexOf(action), `${name}: ${action}`);
    }
  }
});
