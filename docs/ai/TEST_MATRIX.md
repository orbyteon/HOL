# HOL Test Command Map

Run the narrowest applicable fast gate first, then the authoritative Unity gate.

| Change area | Fast local/headless gate | Authoritative gate |
|---|---|---|
| CloudScript / PvP protocol | `node --check playfab/cloudscript.js` and `node --test tools/test/*.test.mjs` | CI rules-tests, EditMode, Android compile |
| Provisioner | `npm test` and `npm run check` in `services/provisioner/` | CI provisioner-test |
| Production assets/imports | focused Node/static checks where present | Unity EditMode + Production Visual Integrity |
| Runtime UI/layout | focused EditMode tests | PlayMode + relevant native Android preview |
| Gameplay rules | Node CloudScript tests plus focused C# tests | Unity EditMode + Android compile |
| Release configuration | release/static scripts only | guarded manual release workflow after approval |

A failed gate must retain enough artifacts to diagnose the exact commit. Re-run
only after the cause is understood or the branch changes; repeated blind reruns
are not validation.
