# HOL PvP isolated two-device playtest

This is a Development-only, separately installed internal test, not a release.
Human visual and real two-device gameplay acceptance remain pending; PR #90 stays draft.

## Pinned environment

- New Title: **HOL-PvP-Test / 11CB9E**, My Game Studio; Development mode,
  new player namespace, zero players verified at creation.
- Protected Titles **HOL / 195DDC** and **My Game / 23E1A** must not be changed.
- Gameplay/server checkpoint: `7b36303abcd0ee7a2b1154e42f5d7e1222930ede`.
- Committed `playfab/cloudscript.js`: 33,456 LF bytes, SHA-256
  `EBB9DEE03FE4D147E63B555DA36EA5D56AAFEE85DB91F72BC56938B7963DEEB5`.
- Pinned source uploaded through Game Manager; **Revision 2 (live)** observed.
  Admin API read-back must independently confirm Version, Revision, source hash,
  and `IsPublished` before calling deployment verification complete.

## Separate installation and build

Use the existing Private Room Android Preview workflow's manual
`isolated_playtest=true` mode only after green CI for the exact head.
The existing merge-ref and CI guards remain in force. This mode skips the emulator
capture job intentionally, rather than rerunning the historical failed capture.

`MainMenuPreviewBuild.BuildIsolatedPvpPlaytest` uses the existing shared Android
Development IL2CPP ARM64+x86_64/GLES3 build, SplashScene then MainMenu.
It temporarily sets package **com.Orbyteon.HOL.pvptest** and label **HOL PvP Test**,
then restores editor values. The build-only `HOL_PVP_ISOLATED_PLAYTEST` define
does not persist in ProjectSettings. The server hash and empty production config
are checked before building. A public build manifest accompanies the APK.

Install beside HOL: do **not** uninstall HOL or clear either application's data.
Android package isolation preserves HOL's existing saves and gives this test its
own profile/Onboarding. Choose the desired saved name and avatar inside the test
app. No real secret key, production release config or provisioning endpoint belongs
in this APK, its manifest, Git, screenshots or logs.

## Device-bound authentication

The actual CustomID is exactly `SystemInfo.deviceUniqueIdentifier`, as in the
existing client. Do not substitute a random ID, PlayFab ID, serial or fixture ID.
This may differ from the production install because Android scopes device IDs.

1. Install the same verified APK on Marinos's and Andreas's devices.
2. Start **HOL PvP Test** normally, with no capture intent extras.
3. With the device connected to Android Platform Tools, read only the diagnostic:

   `adb -s <device-serial> logcat -d -s Unity | findstr HOL_PVP_TEST_DEVICE`

   It reports `title=11CB9E package=com.Orbyteon.HOL.pvptest customId=<actual-id>`.
   Retain the two actual IDs privately with their device labels. No data clearing
   or reinstall is required. If the line has rotated out, reopen the test app.
4. A trusted operator calls **Server/LoginWithCustomID** on
   `https://11CB9E.playfabapi.com`, with each actual CustomID and
   `CreateAccount=true`, using only the new Title's X-SecretKey outside the client.
   Do not use `LoginWithServerCustomId`, which is a different identity contract.
5. Verify each account using **Client/LoginWithCustomID**, the same CustomID,
   TitleId `11CB9E`, `CreateAccount=false`. Verify distinct PlayFab IDs without
   recording session tickets. Reopen the app and use the normal room flow.

The APK always uses client `CreateAccount=false`. It cannot invoke the production
Play Integrity provisioner; missing accounts stay closed until the operator
prepares them. Its transport rejects a wrong Title, package, non-Android or
non-Development build, or any Server/Admin endpoint.

## Server completion gate

With the new Title's operator key, read `Admin/GetCloudScriptVersions` then
`Admin/GetCloudScriptRevision` for the observed published revision. Require only
the pinned source and its full hash. No redeploy is necessary when this matches.
Retain the existing server-authoritative Private Shared Group and mutation-fencing
logic. Verify/add the repository's five Client Shared Group deny statements on
**11CB9E only** and the existing `cleanupExpiredRooms` five-minute scheduled task.
Never modify production workflows, credentials, permissions or guards.

## Two-device acceptance

- Use distinct names and selectable avatar IDs 1 and 6. Both devices must show
  the correct two identities in waiting, match and result.
- A hosts/B joins, then reverse A/B roles. Check real room codes and secret entry;
  neither device may see the other's secret before the authoritative reveal.
- Waiting → match → terminal result → rematch; no stale identities, duplicate
  turns, stuck keyboard, late callback or skipped result.
- Switch EN/EL and cold-restart; saved profiles remain correct in the test app.
- Disconnect/exit one device, verify the other resolves correctly; reconnect and
  create a fresh room. Check Back/cancel during waiting.
- Record APK SHA, deployed Version/Revision/hash, device labels and failures.
  Fixtures and green CI cannot substitute for this human acceptance.
