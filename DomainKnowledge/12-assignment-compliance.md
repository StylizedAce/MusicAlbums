# Assignment Compliance Audit

Source: `linkfire-technical-assignment-music_album.pdf` (3 pages, take-home,
Backend engineering exercise - .NET).

Status legend: **MET** = fully satisfied · **MET+** = satisfied and then some ·
**PARTIAL** = satisfied in spirit, with a named deviation · **GAP** = not done.

---

## Part 1: The Service

### Users and libraries

| Requirement | Status | Evidence |
| --- | --- | --- |
| "A user is just a name. No other attributes, no profile." | **PARTIAL** | `User` = `Id` (technical surrogate key) + `Name` + `CreatedAt`. No profile, no email, no settings. `CreatedAt` is an audit column, not a user attribute - and it is redundant because every saved album already carries `SavedAt`. Removable in one small delivery if strict reading is preferred. |
| "Each user owns exactly one library, and a library belongs to exactly one user." | **MET** | The library is not a separate table: it is *the set of a user's saved albums* (`SavedAlbum.UserId` FK, non-nullable, one owner). A user cannot own two libraries, and an album cannot belong to two users. See section "Modeling note" below. |
| "Authentication isn't required, but you're welcome to add it." | **MET (declined, documented)** | None implemented; identity is the `{userName}` route segment. Recorded as decision #28 with the recommendation (an `X-Api-Key` filter is the honest middle if a reviewer wants to see auth). |

### Search for albums

| Requirement | Status | Evidence |
| --- | --- | --- |
| "Search by album name and artist name." | **MET** | Deezer's `search/album?q=` matches both fields; verified live in-cluster: `q=radiohead` -> 97 hits (first "OK Computer"), `q=discovery` -> 149 hits (first "Discovery"). |
| "Back the search with either the Spotify API or the Deezer API." | **MET+** | **Both.** Deezer real (no credentials needed). Spotify real adapter (client-credentials, cached token) + a deterministic fake selected by config. |
| "Each result should include the artist name, album name, album cover (when available), and album URL." | **MET** | `AlbumResponse` = `artist`, `title`, `coverImageUrl` (nullable -> "when available"), `externalUrl` (Deezer `link`, Spotify `external_urls.spotify`), plus `releaseDate`, `trackCount`. Live sample: `artist='Radiohead'`, `cover=True`, `externalUrl='https://www.deezer.com/album/14879699'`. |

### Manage the library

| Requirement | Status | Evidence |
| --- | --- | --- |
| "Add one or more albums from search results to the library." | **MET** | `POST /api/users/{userName}/library` (repeatable; user auto-created on first save; duplicates -> 409). Verified live with two albums. |
| "Remove albums from the library." | **MET** | `DELETE /api/users/{userName}/library/{albumId}` -> 204; verified live. |

### Test it

| Requirement | Status | Evidence |
| --- | --- | --- |
| "Include unit tests." | **MET+** | 43 unit + integration tests, plus 13 BDD acceptance scenarios = 56. |
| "Show us which parts you chose to make unit-testable, and why that shape works." | **GAP (now addressed)** | Was only in `DomainKnowledge`; the assignment wants it in the README. Added a "Testing approach" section to the README. |

### Document it

| Requirement | Status | Evidence |
| --- | --- | --- |
| "A README with everything needed to build and run the service." | **MET+** | Prerequisites first, then numbered quick start, Docker, Kubernetes, example flow, API table, config/secrets, test matrix. No machine-specific paths (verified by scan). |

## Part 2: API Test Automation

| Requirement | Status | Evidence |
| --- | --- | --- |
| "Write an automated API test covering at least one endpoint." | **MET+** | 13 scenarios across 4 features in `tests/MusicAlbums.ApiTests`, all over real HTTP. |
| "Use any test automation framework you're comfortable with." | **MET** | Reqnroll 3.3.4 (Gherkin/BDD) + xunit 2.9.3, running through `dotnet test`. |
| "Include a README covering setup and execution, complete enough that we can run the suite without asking you questions." | **MET (tightened)** | README now has an explicit "API test suite" command block (`dotnet test tests/MusicAlbums.ApiTests`) next to the overall `dotnet test`. |

## Part 3: Deployment

The task says "pick whichever you know best". We shipped **all three**, which is more
than required.

| Level | Status | Evidence |
| --- | --- | --- |
| 1. Local folder | **MET** | `dotnet run`; SQLite file created + migrated automatically. |
| 2. Docker Compose | **MET+** | `docker compose up --build`, non-root, named volume, curl healthcheck; container E2E incl. restart persistence. |
| 3. Kubernetes | **MET (with a named deviation)** | Manifests + README in `deploy/k8s/`, verified end-to-end on a local cluster incl. PVC persistence across pod restart. **Deviation:** the cluster is the one built into **Docker Desktop**, not Minikube / MicroK8s / kind. Same Kubernetes API, same manifests; only the step that makes the image visible to the nodes differs (`minikube image load ...` or a registry). The README now says so explicitly. |
| "Whichever you choose, the README is part of the deliverable." | **MET** | Root README + `deploy/k8s/README.md` + `DomainKnowledge/09-deployment.md`. |

## Technical requirements

| Requirement | Status | Evidence |
| --- | --- | --- |
| ".NET 10 or later." | **MET** | `net10.0` everywhere, SDK 10.0.108. |
| "Any third-party libraries or tools you like." | **MET** | EF Core 10, Reqnroll, xunit - all centrally versioned. |
| "The solution should be testable by design." | **MET** | Core has zero dependencies; provider seam, repository/unit-of-work abstractions, injected `TimeProvider`, and a test harness that swaps only the transport. |

## "What We're Looking For"

### 1. The code itself - structure, naming, error handling, test design, explicit trade-offs

**MET+.** Three-layer split with a one-way dependency rule; Strategy/Factory/Repository/
Unit-of-Work/Options/Composition-Root all present; exceptions translated once to
ProblemDetails with a documented status map; 35-entry decision log with rationale and
rejected alternatives.

### 2. "How you'd scale it ... Where does the provider boundary sit? What stays the same when a third one arrives? How would you handle differences in their data and rate limits?"

| Sub-question | Status | Where |
| --- | --- | --- |
| Where the provider boundary sits | **MET** | `IAlbumProvider` (Core), resolved by `IAlbumProviderFactory`; both adapters in `Infrastructure/Providers/`. |
| What stays the same when a third arrives | **MET** | One class + one DI registration; endpoints, services, schema and the uniqueness constraint are provider-agnostic. Documented in `02-album-provider-contract.md` and the walkthrough. |
| How to handle differences in their data | **MET** | Normalized `ProviderAlbum`/`ProviderTrack` + per-provider mappers; the quirks each mapper absorbs are tabulated. |
| How to handle differences in their **rate limits** | **GAP (documented design, not implemented)** | We respect declared limits where documented (Spotify search clamped to 10) and have per-call timeouts, but there is no client-side limiter, circuit breaker, retry policy, or cache. Design and the in-box .NET mechanism proposed in `11-architecture-explained.md` section 7. **This is the single most valuable next delivery.** |

### "A short section in the README explaining your architectural decisions, and anything you deliberately left out, is worth more to us than extra features."

**GAP (now addressed).** The README had neither. Both sections added, pointing at the
decision log rather than duplicating it.

## Submitting

| Requirement | Status | Evidence |
| --- | --- | --- |
| "Public Git repository ... containing the service, the API test project, deployment files, and READMEs." | **MET** | `https://github.com/StylizedAce/MusicAlbums` (public, 18+ commits, each independently verified). |

---

## Modeling note: why there is no `Library` table

The requirement is "each user owns exactly one library, and a library belongs to
exactly one user" - a 1:1 relationship. We model the library as the *aggregate* of a
user's saved albums rather than a separate entity, because:

- a 1:1 table that contains no data of its own is pure ceremony;
- the invariant is already enforced structurally: albums are reachable only through a
  non-nullable `UserId`, so an album cannot exist without an owner, and a user cannot
  have two libraries;
- naming, timestamps, or statistics for a library later means adding a `Library`
  entity with `UserId` as PK/FK - a small, additive migration that does not touch the
  album table.

The trade-off: there is no "library" row to hang metadata on today. We consider that
the right call at this size, and it is logged as a decision.

## Summary

| Area | Met | Gap/Deviation |
| --- | --- | --- |
| Part 1 (service) | 5/5 requirements | user `CreatedAt`; testing-rationale now in README |
| Part 2 (API tests) | 3/3 | - |
| Part 3 (deployment) | 4/4 | cluster is Docker Desktop's, not minikube/kind (documented) |
| Technical | 3/3 | - |
| Looking-for: code | 1/1 | - |
| Looking-for: scale | 3/4 | **rate-limit handling: design documented, not built** |
| Looking-for: README decisions | 0/1 (now added) | - |

**Bottom line:** every hard requirement is satisfied. The one substantive gap against
"What We're Looking For" is client-side rate-limit/resilience handling, which is the
recommended next delivery.
