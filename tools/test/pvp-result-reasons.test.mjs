import test from 'node:test';
import assert from 'node:assert/strict';
import { loadCloudScript, startMatch, guess } from './cloudscript-harness.mjs';

function match(opener = 'host') {
  const cs = loadCloudScript({ random: () => opener === 'host' ? 0.25 : 0.75 });
  return { cs, ...startMatch(cs, { hostSecret: 42, guestSecret: 77 }) };
}
function move(cs, roomId, side, value, lock = false) {
  const result = guess(cs, roomId, side, value, lock);
  assert.equal(result.ok, true, result.error);
  return cs.view(result);
}
function read(cs, roomId, player = 'HOST') {
  return cs.view(cs.call('getRoom', player, { roomId }));
}
function emptyReason(state) {
  assert.equal(state.resultReason, '');
  assert.equal(state.resultHostCandidates, 0);
  assert.equal(state.resultGuestCandidates, 0);
  assert.equal(state.resultForfeitedSide, '');
}

for (const opener of ['host', 'guest']) {
  test(`${opener}: reason is final-only and one correct player wins`, () => {
    const { cs, roomId, state } = match(opener);
    emptyReason(state);
    const other = opener === 'host' ? 'guest' : 'host';
    emptyReason(move(cs, roomId, opener, opener === 'host' ? 77 : 42));
    const final = move(cs, roomId, other, 50);
    assert.equal(final.winner, opener);
    assert.equal(final.resultReason, 'only_correct');
    for (const player of ['HOST', 'GUEST']) {
      const poll = read(cs, roomId, player);
      assert.equal(poll.resultReason, final.resultReason);
      assert.equal(poll.resultForfeitedSide, '');
      assert.equal('hostSecret' in poll, false);
      assert.equal('guestSecret' in poll, false);
    }
  });
  for (const locker of ['host', 'guest', 'both', 'neither']) {
    test(`${opener}: same-round finish with ${locker} LOCK records its real criterion`, () => {
      const { cs, roomId } = match(opener);
      const other = opener === 'host' ? 'guest' : 'host';
      emptyReason(move(cs, roomId, opener, opener === 'host' ? 77 : 42,
        locker === opener || locker === 'both'));
      const final = move(cs, roomId, other, other === 'host' ? 77 : 42,
        locker === other || locker === 'both');
      const tied = locker === 'both' || locker === 'neither';
      assert.equal(final.winner, tied ? 'draw' : locker);
      assert.equal(final.resultReason, tied ? 'draw' : 'lock');
      assert.equal(final.resultHostCandidates, 100);
      assert.equal(final.resultGuestCandidates, 100);
    });
  }
}

for (const bothLock of [false, true]) {
  test(`equal LOCK status (${bothLock}): smaller PRE-correct range decides`, () => {
    const { cs, roomId } = match();
    move(cs, roomId, 'host', 70); // 71..100 = 30
    move(cs, roomId, 'guest', 50); // 1..49 = 49
    emptyReason(move(cs, roomId, 'host', 77, bothLock));
    const final = move(cs, roomId, 'guest', 42, bothLock);
    assert.equal(final.resultReason, 'range');
    assert.equal(final.winner, 'host');
    assert.equal(final.resultHostCandidates, 30);
    assert.equal(final.resultGuestCandidates, 49);
  });
}
test('a consumed missed-LOCK forfeit in the final round is reported, not a future debt', () => {
  const { cs, roomId } = match();
  move(cs, roomId, 'host', 50, true);
  move(cs, roomId, 'guest', 30);
  const final = move(cs, roomId, 'guest', 42);
  assert.equal(final.resultReason, 'only_correct');
  assert.equal(final.resultForfeitedSide, 'host');
  const second = match();
  move(second.cs, second.roomId, 'host', 77);
  const noConsumedSlot = move(second.cs, second.roomId, 'guest', 50, true);
  assert.equal(noConsumedSlot.resultForfeitedSide, '');
});
test('a past forfeit is not attributed to a later winning round; rematch clears all reasons', () => {
  const { cs, roomId } = match();
  move(cs, roomId, 'host', 50, true);
  move(cs, roomId, 'guest', 30);
  move(cs, roomId, 'guest', 35); // completes forfeited round, not a win
  move(cs, roomId, 'host', 77);
  const final = move(cs, roomId, 'guest', 40);
  assert.equal(final.resultForfeitedSide, '');
  const args = { roomId, matchIndex: final.matchIndex, secret: 60 };
  assert.equal(cs.call('requestRematch', 'HOST', args).ok, true);
  assert.equal(cs.call('requestRematch', 'GUEST', { ...args, secret: 20 }).ok, true);
  emptyReason(read(cs, roomId));
});
test('legacy finalized rooms expose empty metadata without guessing a reason', () => {
  const { cs, roomId } = match();
  move(cs, roomId, 'host', 77);
  move(cs, roomId, 'guest', 42);
  const group = cs.store.groups.get(roomId);
  const old = JSON.parse(group.state);
  for (const field of Object.keys(old))
    if (field.startsWith('result')) delete old[field];
  group.state = JSON.stringify(old);
  emptyReason(read(cs, roomId));
});
