import test from 'node:test';
import assert from 'node:assert/strict';
import { buildInternalTrack } from '../tools/publish-google-play.mjs';

test('Google Play publisher targets only the internal track with the requested versionCode', () => {
  const track = buildInternalTrack('0.4.0', 6);

  assert.equal(track.track, 'internal');
  assert.equal(track.releases.length, 1);
  assert.equal(track.releases[0].status, 'completed');
  assert.deepEqual(track.releases[0].versionCodes, ['6']);
  assert.match(track.releases[0].name, /0\.4\.0 \(6\) internal$/);
  assert.equal(track.releases[0].releaseNotes[0].language, 'en-US');
});
