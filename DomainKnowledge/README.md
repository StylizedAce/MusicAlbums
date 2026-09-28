# MusicAlbums - Domain Knowledge Base

This folder is the project's durable documentation. It captures the important
decisions, contracts, and external-system behavior so that knowledge is not lost
between sessions. Update it whenever a meaningful decision changes.

## Index

| Document | Contents |
| --- | --- |
| [01-architecture.md](01-architecture.md) | Solution layout, dependency rules, patterns used |
| [02-album-provider-contract.md](02-album-provider-contract.md) | `IAlbumProvider`, how to add a provider, Spotify roadmap |
| [03-deezer-integration.md](03-deezer-integration.md) | Real Deezer API behavior, quirks, mapping |
| [04-persistence.md](04-persistence.md) | EF Core model, SQLite specifics, migrations |
| [05-api-reference.md](05-api-reference.md) | Endpoints, request/response examples, status codes |
| [06-testing-strategy.md](06-testing-strategy.md) | What is tested where, how to run, live smoke test |
| [07-decision-log.md](07-decision-log.md) | Running log of decisions and their rationale |
| [08-docker.md](08-docker.md) | Container image, compose stack, secrets and run guide |
| [09-deployment.md](09-deployment.md) | Deployment levels (local/Docker/K8s) and secrets layering |
| [10-code-walkthrough.md](10-code-walkthrough.md) | End-to-end code walkthrough: request flow, patterns, traces |
| [11-architecture-explained.md](11-architecture-explained.md) | Beginner-friendly explainers: deployment topology, k8s mechanics, IaC, compose vs k8s, microservices, concurrency, glossary |
| [12-assignment-compliance.md](12-assignment-compliance.md) | Requirement-by-requirement audit against the take-home brief, with honest gaps |
| [13-repo-map.md](13-repo-map.md) | Flashcard repo map and interview drill (where is X, likely questions) |
| [verification/V1-verification.md](verification/V1-verification.md) | V1 test evidence: 27/27 automated + live Deezer E2E |
| [verification/V2-verification.md](verification/V2-verification.md) | V2 test evidence: 30/30 automated + native/container E2E |
| [verification/V3-verification.md](verification/V3-verification.md) | V3 test evidence: album URLs + Spotify adapter + policy notes |
| [verification/V4-verification.md](verification/V4-verification.md) | V4 test evidence: Reqnroll BDD acceptance suite |
| [verification/V5-verification.md](verification/V5-verification.md) | V5 test evidence: Kubernetes E2E incl. PVC persistence |

## Ground rules captured here

- Backend only: no auth middleware. A user is a route segment (`userName`) and is
  created on the fly on the first write.
- Deezer is a **real** integration and needs **no API key** (verified 2026-09-28).
- Spotify is currently a **fake provider** that implements the same contract.
- The user's library is a **local snapshot** persisted with EF Core on SQLite;
  providers are only consulted for search and for album details at save time.
