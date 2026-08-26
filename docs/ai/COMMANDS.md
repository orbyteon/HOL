# HOL Canonical Commands

Run from the repository root unless a working directory is stated.

```bash
# JavaScript syntax
node --check playfab/cloudscript.js
node --check tools/playfab/deploy-cloudscript.mjs
node --check tools/release/write-release-config.mjs

# All headless PvP/rule/contract tests
node --test tools/test/*.test.mjs

# Provisioner
cd services/provisioner
npm ci --no-audit --no-fund
npm test
npm run check
```

Unity EditMode, PlayMode and Android compile/build remain authoritative through
GitHub Actions with the configured Unity credentials. Do not substitute an
ad-hoc compiler for a green Unity merge candidate.

Production workflows are manual, `main`-only and owner-authorized. Never deploy
PlayFab, the provisioner, a signed release, `minVersion`, or Google Play from a
feature branch.
