# NuGet package upgrade assessment

_Mode: **quick assessment** — package API diffs only; no per-project source scan was run._

## Recommended versions

- **CarpaNet**: not referenced by any scoped project.

## Public API changes

No package API diffs were produced (no scoped project moves any requested package to a new version).

## Breaking-change findings

- Version divergence findings (Pkg.0003): 0
- Requested-version-unsupported findings (Pkg.0002): 0

- Quick mode does not scan source, so there are no per-line `PkgApi` usage findings. Review the
  per-package API diffs above and rely on build errors during execution to pinpoint affected code.
- A full code scan can locate the exact source location of every breaking-change usage across the repo.
  It is opt-in and slower — re-run the assessment with `fullScan=true` only if the user requests it.

## Next steps

1. Proceed to planning to triage the API changes above and plan the code fixes (if any).

