# Documentation Index

This folder is the canonical project record.

## Canonical documents

- **CURRENT_STATE.md** — what exists right now, known blockers, next implementation target.
- **FINDINGS.md** — verified technical findings and unknowns.
- **ARCHITECTURE.md** — planned runtime architecture and data model.
- **FEATURE_SPEC.md** — behavior and safety requirements for locking, mass salvage, and gear sets.
- **ROADMAP.md** — staged implementation plan.
- **IDEAS.md** — feature ideas that are not yet committed roadmap scope.
- **TEST_PLAN.md** — manual and technical validation matrix.
- **MICROSOFT_STORE_SETUP.md** — setup/install notes for the Xbox app / Microsoft Store edition.
- **THIRD_PARTY.md** — dependencies, references, and licensing constraints.
- **RESEARCH_LOG.md** — dated research/implementation log.
- **DECISIONS.md** — architectural decisions and rationale.
- **INVENTORY_IDENTITY.md** — item/hero identity research and the safe fallback strategy for persistent locks.

## Documentation rule

After each meaningful research or implementation pass:

1. Update `CURRENT_STATE.md`.
2. Add the findings/results to `RESEARCH_LOG.md`.
3. Update `FINDINGS.md`, `ROADMAP.md`, or `DECISIONS.md` when new information changes them.
4. Record tests actually performed. Do not mark untested behavior as working.
