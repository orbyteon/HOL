# HOL Architecture Documentation

- [`cto-stabilization-roadmap.md`](cto-stabilization-roadmap.md) — mandatory
  phased execution order for architecture hardening and AI-development speed.
- [`../../AGENTS.md`](../../AGENTS.md) — repository-wide production contracts,
  release boundaries and canonical working rules.
- [`../ci-policy.md`](../ci-policy.md) — CI ordering, cost controls and preview
  workflow policy.
- [`../playfab-auth-provisioning.md`](../playfab-auth-provisioning.md) — trusted
  production account bootstrap and Play Integrity boundary.

Architecture changes must preserve gameplay authority, visual fidelity,
localization and release safety. A refactor is not complete merely because it
compiles; its exact merge candidate must pass the relevant headless and Unity
validation gates.
