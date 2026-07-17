# Sharkable Quality Improvement Plan

**Date:** 2026-07-19
**Baseline commit:** `ecb25c0` (post v0.6.1, after SHARK-SEC-001..024 rounds)
**Scope:** `src/Sharkable` (core). Cross-repo items for `Sharkable.Cache.Redis` / `Sharkable.AutoCrud.SqlSugar` are noted where relevant but tracked in their own repos.
**Method:** Full manual code review of the core library (~170 files), building on the prior audits in `local/security-audit.md` and `local/SECURITY-MASTER.md`. Issues already fixed in the SHARK-SEC rounds are **not** repeated here; every item below is verified against current code.

---

## 1. Guiding principles (apply to every item)

1. **General-purpose framework first** — every feature must make sense for an arbitrary ASP.NET Core minimal-API app, not for one specific product.
2. **NativeAOT-compatible** — no new unguarded reflection, `MakeGenericType`, `Activator.CreateInstance`, or runtime-type serialization in request paths. Public APIs that require dynamic code must be annotated (`RequiresDynamicCode` / `DynamicallyAccessedMembers`).
3. **Minimal intrusion** — opt-in by default for anything that changes pipeline behavior; never hijack app-wide services (e.g. shared `IMemoryCache`); never change user-visible response shapes silently.
4. **Sensible defaults + full customizability** — every built-in default implementation gets a factory / options hook so users can replace it (`XxxFactory` pattern, consistent with the existing `IdempotencyStoreFactory`, `RateLimitStoreFactory`, `SagaStoreFactory`, `CronJobStoreFactory`, `ErrorLocalizerFactory`, `AuthorizationInterceptorFactory`).
5. **One logical change per commit**, `CHANGELOG.md` updated in the same commit (per AGENTS.md). Breaking changes are batched into minor versions and documented in the migration notes.

## 2. What is already in good shape (do not regress)

- JWT hardening: algorithm allowlist, 30 s clock skew, mandatory audiences, user-callback ordering (`SharkExtension.cs`).
- Constant-time API-key comparison with hot-reload (`ApiKeyMiddleware.cs`).
- Bounded idempotency/rate-limit stores with size caps (`MemoryIdempotencyStore`, `MemoryRateLimitStore`).
- ETag streaming hash with size cap, weak/`If-None-Match`-list handling (`ETagMiddleware.cs`).
- Graceful shutdown drain via `Task.Delay` polling (no more `Thread.Sleep`).
- Health-check aggregate timeout, correlation-id validation, Scalar credential gating outside Development.
- Startup `ConfigurationValidator` with fail-fast errors.
- Cron/Saga distributed-lock renewal protocol.

## 3. Finding summary

| Area | File | P0 | P1 | P2 | P3 | Total |
|---|---|---|---|---|---|---|
| Correctness / bugs | [01-correctness-and-bugs.md](01-correctness-and-bugs.md) | 0 | 3 | 7 | 2 | 12 |
| Performance | [02-performance.md](02-performance.md) | 0 | 3 | 4 | 4 | 11 |
| Memory & lifetime | [03-memory-and-lifetime.md](03-memory-and-lifetime.md) | 0 | 1 | 4 | 2 | 7 |
| Security | [04-security.md](04-security.md) | 0 | 1 | 2 | 3 | 6 |
| AOT / trimming | [05-aot-and-trimming.md](05-aot-and-trimming.md) | 0 | 3 | 3 | 1 | 7 |
| Architecture & extensibility | [06-architecture-and-extensibility.md](06-architecture-and-extensibility.md) | 0 | 3 | 6 | 5 | 14 |
| Feature suggestions | [07-feature-suggestions.md](07-feature-suggestions.md) | — | — | — | — | 15 |

**Severity scale:** P0 = exploit/crash/data-loss now · P1 = fix in the next release · P2 = scheduled fix · P3 = polish / opportunistic.
**Effort scale:** S < 0.5 day · M = 1–2 days · L = 3–5 days.

## 4. Roadmap (proposed phasing)

| Phase | Theme | Items | Breaking? | Target |
|---|---|---|---|---|
| **1** | Correctness & security hotfixes | BUG-01…03, SEC-01, BUG-05, PERF-02, ARCH-01, ARCH-02, MEM-01 | No | v0.6.2 |
| **2** | AOT / trimming hardening | AOT-01…05, BUG-10, PROC items (analyzer gate) | Internal-only | v0.7.0 |
| **3** | Performance pass + benchmarks | PERF-01, 03…07, MEM-02…05, PROC-02 | No | v0.7.x |
| **4** | Architecture consolidation | ARCH-03…05, ARCH-08, ARCH-12, BUG-06, BUG-08, BUG-09 | Minor (opt-in) | v0.8.0 |
| **5** | Extensibility & new features | ARCH-06, 07, 09, 10 + FEAT-01…08 | Additive | v0.9.0 |
| **6** | Process & ecosystem | PROC-01…04, FEAT-09…15 (triage) | Additive | v1.0.0-rc |

Phase order is a recommendation, not a contract — every finding is written to be independently implementable. When an item is completed, mark it `✅ Done (commit <hash>)` in its section file and move on; do not delete history.

## 5. Process items (apply from Phase 1 onward)

| ID | Item |
|---|---|
| PROC-01 | Every BUG/SEC fix ships with a regression test in `src/Sharkable.Tests` (the project exists but is empty of coverage for the items below). |
| PROC-02 | Add a BenchmarkDotNet project (`bench/Sharkable.Benchmarks`) covering: unified-wrap filter, rate-limit middleware, ETag middleware, audit middleware. Run before/after each PERF item. |
| PROC-03 | Enable `<IsAotCompatible>true</IsAotCompatible>` (or `IsTrimmable` + trim analyzers) on `src/Sharkable` so IL2xxx/IL3xxx warnings surface in CI; keep `Sharkable.NativeTest` publishing AOT as a smoke gate. |
| PROC-04 | Doc-site: every Phase-4+ behavioral change gets a migration note in the docs repo (EN + zh-cn) per AGENTS.md checklist. |

## 6. How to read the finding files

Each finding uses this template:

```
### <ID> — <title>
**Severity** Px · **Effort** S/M/L · **Breaking** Yes/No/Maybe
**Location:** path/to/File.cs:line
**Problem:** what is wrong, why it matters.
**Proposal:** concrete fix direction, including the option/factory surface to add.
```

IDs are stable — refer to them in commit messages (e.g. `fix(cron): wire shutdown token into job execution (BUG-01)`).
