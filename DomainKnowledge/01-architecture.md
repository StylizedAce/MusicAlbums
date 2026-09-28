# Architecture

## Solution layout

```
MusicAlbums.slnx
Directory.Build.props          shared build settings (net10.0, nullable, warnings as errors)
Directory.Packages.props       central package management (CPM)
dotnet-tools.json              repo-local dotnet-ef tool
src/
  MusicAlbums.Core/            abstractions + domain models + use-case services (no dependencies)
  MusicAlbums.Infrastructure/  EF Core persistence, provider adapters, DI extension
  MusicAlbums.Api/             minimal API endpoints, DTOs, composition root
tests/
  MusicAlbums.Tests/           unit tests + E2E integration tests (xunit)
DomainKnowledge/               this documentation set
```

## Dependency rule

```
Api ──▶ Infrastructure ──▶ Core
Api ──────────────────────▶ Core
Tests ──▶ all
```

- `Core` references nothing. It contains domain models, the `IAlbumProvider`
  contract, repository/unit-of-work abstractions, exceptions and application
  services (`LibraryService`, `AlbumCatalogService`).
- `Infrastructure` implements Core abstractions: EF Core `DbContext` and
  repositories, the Deezer adapter, the fake Spotify provider, the provider
  factory, and `AddInfrastructure(...)`.
- `Api` is the composition root: option binding, DI registration, endpoint
  groups, DTO mapping, and exception-to-ProblemDetails translation.

## Patterns in use

| Pattern | Where |
| --- | --- |
| Strategy | `IAlbumProvider` with `DeezerAlbumProvider` / `SpotifyAlbumProvider` |
| Factory | `IAlbumProviderFactory` resolves providers by name, case-insensitively |
| Repository | `IUserRepository`, `ISavedAlbumRepository` implemented over EF Core |
| Unit of Work | `IUnitOfWork.SaveChangesAsync`, implemented by `MusicAlbumsDbContext` |
| Options | `DeezerOptions`, `AlbumProviderOptions` bound from configuration, validated on start |
| Composition root | `Program.cs` wires API + application + infrastructure |
| Adapter | Deezer DTOs mapped to `ProviderAlbum` in `DeezerAlbumMapper` |
| Exception translator | `MusicAlbumsExceptionHandler` maps domain exceptions to ProblemDetails |
| Test double | `StubHttpMessageHandler` lets tests run the real Deezer adapter without network |

## Request flow: save an album

```
POST /api/users/alice/library { provider, providerAlbumId }
  │
  ├─ LibraryEndpoints (Api)
  ├─ LibraryService.AddAlbumAsync (Core)
  │    ├─ IAlbumProviderFactory.GetRequired/GetDefault  → provider strategy
  │    ├─ IAlbumProvider.GetAlbumAsync                  → ProviderAlbum
  │    ├─ IUserRepository.FindByNameAsync / Add         → user auto-created
  │    ├─ ISavedAlbumRepository.ExistsAsync / Add       → snapshot persisted
  │    └─ IUnitOfWork.SaveChangesAsync
  └─ 201 Created + Location header  (or 400/404/409/502 via exception handler)
```

## Why the application services live in Core

`LibraryService` and `AlbumCatalogService` depend only on Core abstractions, so
they are pure use-case orchestration with no infrastructure knowledge. This keeps
them unit-testable with stubbed providers and a real in-memory SQLite database,
and it keeps EF Core out of the domain layer.
