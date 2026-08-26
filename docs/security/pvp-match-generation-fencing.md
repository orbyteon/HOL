# PvP match-generation fencing

HOL friend rooms survive rematches, so every mutation that belongs to one match
must carry that match's authoritative `matchIndex`. Without this fence, a delayed
result-screen callback from match N could alter or delete match N+1 in the same
room.

## Fenced commands

The PlayFab client and Legacy CloudScript require the live index for:

- Signals;
- rematch secret commitments;
- result acknowledgement;
- leave/room release.

`submitGuess` was already fenced. An omitted index is interpreted as match 0 for
first-match compatibility, but is rejected after the room advances. This makes
old or delayed callbacks fail closed instead of acting on a newer match.

Cancelled create/join cleanup deliberately sends match 0 and does not discover
or retry against a newer generation. A stale cleanup must never be allowed to
close a room that has already advanced.

## Diagnostics

Room-state and returned-state JSON parse failures are logged with an actionable
PlayFab diagnostic instead of being swallowed. Secrets and raw authenticated
request headers are not logged.

## Production rollout order

This repository change does not authorize a production deployment. When an
owner explicitly authorizes rollout, the safe sequence is:

1. ship and verify the fenced Unity client while the old CloudScript remains
   live; the old server ignores the additional parameters;
2. enforce the new client version only after target-device adoption is verified;
3. deploy the fenced CloudScript revision last.

Deploying the server first is unsafe for old clients after their first rematch,
because omitted indices are intentionally treated as match 0.

## Validation

The contract is covered by headless tests for explicit and omitted stale
Signal, Leave, Rematch and Ack commands, a valid current-match Leave after a
rematch, chained rematches, and source-level client command construction.
Unity EditMode, Android compile and PlayMode remain mandatory before merge.
