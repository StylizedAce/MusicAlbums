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
| [verification/V1-verification.md](verification/V1-verification.md) | V1 test evidence: 27/27 automated + live Deezer E2E |
| [verification/V2-verification.md](verification/V2-verification.md) | V2 test evidence: 30/30 automated + native/container E2E |

## Ground rules captured here

- Backend only: no auth middleware. A user is a route segment (`userName`) and is
  created on the fly on the first write.
- Deezer is a **real** integration and needs **no API key** (verified 2026-09-28).
- Spotify is currently a **fake provider** that implements the same contract.
- The user's library is a **local snapshot** persisted with EF Core on SQLite;
  providers are only consulted for search and for album details at save time.
