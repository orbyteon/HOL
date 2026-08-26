# HOL AI Development Entry Point

Before changing HOL, read in this order:

1. root [`AGENTS.md`](../../AGENTS.md) for mandatory production contracts
2. [`../architecture/README.md`](../architecture/README.md) for ownership and
   refactor sequencing
3. [`../ci-policy.md`](../ci-policy.md) for the authoritative validation order
4. the focused source, tests and approved assets for the screen/service being
   changed

## Working constraints

- Work from current `main` on one focused feature branch.
- Do not create parallel implementations of the same screen.
- Preserve real callbacks and controller authority; do not build visual clones.
- Required production art must fail closed rather than reveal a generated
  fallback.
- Per-match PvP mutations carry the current `matchIndex`.
- Do not rely on hierarchy names, scene-wide searches or frame-count polling for
  new required dependencies.
- Add the smallest focused regression test that would have caught the bug.
- Keep `CHANGELOG.md` current.

The long-term migration order is defined in
[`../architecture/cto-stabilization-roadmap.md`](../architecture/cto-stabilization-roadmap.md).
