# HOL — Release checklist

Release blockers and manual go-live steps, in order.

## 0. CI activation

`.github/workflows/ci.yml` always runs static integrity checks. Unity EditMode
and Android compile verification require the repository's Unity credentials;
missing credentials now **fail CI** instead of producing a misleading green
run with the real jobs skipped.

- [ ] Follow https://game.ci/docs/github/activation to produce a `.ulf`
      license file for Unity 2022.3.x
- [ ] Repo → Settings → Secrets and variables → Actions: add
      `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`
- [ ] Re-run CI and confirm all three checks are green:
      `Static integrity`, `EditMode tests`, and `Build Android (compile check)`
- [ ] Do not merge a release candidate while the Unity credential gate is red

## 1. Smoke test in Unity

Pull the release branch, open the project in Unity 2022.3.62f3, let package
resolution finish, then verify:

Splash:

- [ ] Indigo gradient background, faint drifting digits
- [ ] Logo blooms/breathes; cyan→magenta seam + tagline
- [ ] Gold loading hairline fills; tap skips splash

Main menu / solo:

- [ ] Consent dialog appears on first launch
- [ ] Choose **No** → LevelPlay remains disabled and the game still plays normally
- [ ] Choose **Yes** → ads initialize; Settings → Ads privacy can withdraw permission
- [ ] Reopen Ads privacy after switching EN/EL → dialog follows the current language
- [ ] First launch on a Greek-language device starts in Greek
- [ ] Blank player name uses the localized default instead of storing English `Player`
- [ ] Difficulty buttons highlight the active choice in gold
- [ ] Stats label shows wins/losses/streak/best/fastest win
- [ ] Play → searching → Cancel and Android back both stop the search
- [ ] Opponent-found stinger plays only when an opponent is found
- [ ] Buttons click/squash and panels fade+rise
- [ ] Match end → Rematch returns to number entry; win confetti/stinger work
- [ ] Solo Android back mid-match: first press shows the localized warning;
      second press within two seconds exits cleanly
- [ ] Simulated-opponent disclosure is visible on solo matchmaking screens

PvP:

- [ ] Create room produces a 5-character code and second device can join
- [ ] A third simultaneous join attempt is rejected as room full
- [ ] Inspect PlayFab responses: live `getRoom` state contains no host/guest secret
- [ ] Modify a client request to submit an invented `side` field → server ignores it;
      identity comes from `currentPlayerId`
- [ ] Double-submit the same turn → only one server turn claim succeeds
- [ ] Higher/lower/correct hint is returned by the server and displays correctly
- [ ] Win count is correct even if result polling arrives before the submit callback
- [ ] A loss reveals only the opponent secret after `phase == done`
- [ ] Normal Leave removes the PlayFab room and notifies the other client
- [ ] Completed match is removed after both clients acknowledge the result
- [ ] Back out during create/join, then try the old code → no late UI hijack
- [ ] PvP Android back navigates create/join/menu; mid-match uses explicit Leave

Other regressions:

- [ ] Solo out-of-range guess is rejected without clearing typed input
- [ ] STOP GAME and number placeholder follow selected language
- [ ] Save-streak reward restores the streak without emitting a second MatchEnded event
- [ ] Reload MainMenu twice on the same day → DailyStreak event fires only once
- [ ] Force-update `minVersion` higher than build → Update + Quit dialog
- [ ] Player Settings icon is HOL artwork, not Unity default

## 2. PlayFab — mandatory production hardening

### Title + CloudScript

- [ ] Create the PlayFab Studio/Title and copy the Title ID
- [ ] Paste the Title ID into `PvpRuntimeUI.playFabTitleId` in `MainMenu`
- [ ] Automation → CloudScript (Legacy) → deploy the exact current
      `playfab/cloudscript.js`
- [ ] Confirm the deployed revision exposes:
      `createRoom`, `joinRoom`, `getRoom`, `submitGuess`, `ackResult`, `leaveRoom`

### Disable client Shared Group authority

The Unity PlayFab client no longer needs Shared Group APIs. Before release,
disable the **Client** Shared Group methods in the PlayFab API Access Policy
(Create/Get/Update Shared Group data and member-management methods). Room data
is private and is accessed only by CloudScript/server APIs.

- [ ] Verify a modified client cannot call `GetSharedGroupData` successfully
- [ ] Verify a modified client cannot call `UpdateSharedGroupData` successfully
- [ ] Verify normal PvP still works because it uses `ExecuteCloudScript`

### Provision first-time anonymous players from a trusted server

Release builds deliberately send `CreateAccount:false` to
`Client/LoginWithCustomID`. Do **not** solve first-time login by enabling
client-side anonymous account creation for production.

- [ ] Deploy a trusted provisioning service that calls PlayFab
      `Server/LoginWithCustomID` with the Title Secret Key and `CreateAccount:true`
- [ ] Never embed the Title Secret Key in Unity, CloudScript parameters, repo,
      or downloadable app assets
- [ ] Protect/rate-limit the provisioning service; a public unauthenticated
      endpoint is not a meaningful trust boundary
- [ ] Provision a fresh install, then confirm the app's normal
      `Client/LoginWithCustomID(CreateAccount:false)` succeeds
- [ ] Keep `allowClientAccountCreationInDebugBuilds` for development only

See `docs/playfab-auth-provisioning.md` for the required boundary.

### Version gate

- [ ] Optional: Title Data → `minVersion` (for example `0.2`)
- [ ] ForceUpdate reuses the same PlayFab session as PvP and remains fail-open
      when PlayFab/title data is unavailable

## 3. Release keystore

- [ ] Player Settings → Keystore Manager → create `hol-release.keystore`, alias `hol`
- [ ] Back up keystore + passwords offline immediately
- [ ] Keep keystore out of git (`*.keystore` is ignored)
- [ ] Never reuse another title's signing key

## 4. Advertising + Play Console

- [ ] LevelPlay app registered with the production Android app key and bundle
      `com.Orbyteon.HOL`
- [ ] `Interstitial_Android` and `Rewarded_Android` units active
- [ ] Verify consent **No** produces no ads and no LevelPlay initialization on a fresh launch
- [ ] Verify consent **Yes** loads interstitial/rewarded units normally
- [ ] Review the final Play Console Data safety answers against `docs/privacy.html`
      and the exact mediated networks enabled in the LevelPlay dashboard
- [ ] Upload the signed AAB, content rating, EN/EL listing copy, screenshots,
      feature graphic, icon, and optional promo video

## 5. Privacy policy hosting

Host `docs/privacy.html` at a stable public HTTPS URL before publishing.

- [ ] Host the policy (GitHub Pages when available, or another static host)
- [ ] Open the URL without authentication/incognito and verify it loads
- [ ] Link that exact URL in Play Console
- [ ] Re-read the policy after final PlayFab and LevelPlay dashboard configuration;
      update it if actual data handling differs

## 6. Security hygiene

- [ ] Rotate any development credentials/tokens that were exposed outside their
      intended secret store
- [ ] Confirm no Title Secret Key, keystore, passwords, service credentials, or
      private provisioning tokens are committed
- [ ] Run a repository secret scan before making the repository public
