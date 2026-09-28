# Repo Map & Interview Drill

Written after the interview debrief: the code was fine, the **navigation** was
the gap. This file is the antidote. Use it like flashcards:

1. Cover the right-hand column.
2. Answer each row **out loud** with the full path.
3. Check, mark what you missed, repeat tomorrow. Two passes and you are fluent.

## 1. Where is X?

| Question | File |
| --- | --- |
| Where does the app start / everything get wired? | `src/MusicAlbums.Api/Program.cs` |
| Where are the endpoints? | `src/MusicAlbums.Api/Endpoints/` (`ProviderEndpoints`, `CatalogEndpoints`, `LibraryEndpoints`) |
| Where are request/response DTOs and their mapping? | `src/MusicAlbums.Api/Contracts/` + `ContractMappings.cs` |
| Where do domain exceptions become HTTP status codes? | `src/MusicAlbums.Api/ErrorHandling/MusicAlbumsExceptionHandler.cs` |
| Where is the demo frontend? | `src/MusicAlbums.Api/wwwroot/` (`index.html`, `styles.css`, `app.js`); served by `UseStaticFiles` in `Program.cs` |
| Where is the provider contract (Strategy)? | `src/MusicAlbums.Core/Abstractions/IAlbumProvider.cs` |
| Where is the provider registry (Factory)? | `src/MusicAlbums.Core/Abstractions/IAlbumProviderFactory.cs` + `src/MusicAlbums.Infrastructure/Providers/AlbumProviderFactory.cs` |
| Where are the use cases? | `src/MusicAlbums.Core/Application/LibraryService.cs`, `AlbumCatalogService.cs` |
| Where are the domain models? | `src/MusicAlbums.Core/Models/` |
| Where are the repositories + unit of work? | `src/MusicAlbums.Core/Abstractions/I*Repository.cs`, `IUnitOfWork.cs`; implementations in `src/MusicAlbums.Infrastructure/Persistence/Repositories/` |
| Where is the Deezer integration? | `src/MusicAlbums.Infrastructure/Providers/Deezer/` (`DeezerAlbumProvider`, `DeezerAlbumMapper`, `DeezerApiContracts`, `DeezerOptions`) |
| Where is the Spotify integration? | `src/MusicAlbums.Infrastructure/Providers/Spotify/` (`SpotifyAlbumProvider`, `SpotifyTokenProvider`, `SpotifyAlbumMapper`, `SpotifyApiContracts`, `SpotifyOptions`, `FakeSpotifyAlbumProvider`) |
| Where is throttling / circuit breaking? | `src/MusicAlbums.Infrastructure/Providers/Throttling/ProviderThrottling.cs` (`ProviderGuard`, `ProviderGuardRegistry`, `ThrottledAlbumProvider`) |
| Where are providers registered / mode switched? | `src/MusicAlbums.Infrastructure/DependencyInjection.cs` |
| Where is the DB context? | `src/MusicAlbums.Infrastructure/Persistence/MusicAlbumsDbContext.cs` |
| Where is the schema defined (indexes, collation, converters)? | `src/MusicAlbums.Infrastructure/Persistence/Configurations/` |
| Where are migrations? | `src/MusicAlbums.Infrastructure/Persistence/Migrations/`; applied at startup in `Program.cs` |
| Where do duplicate saves become 409 instead of 500? | `src/MusicAlbums.Infrastructure/Persistence/TranslatingUnitOfWork.cs` (`SqliteErrors`) |
| Where is the concurrent user-creation race handled? | `UserRepository.GetOrCreateAsync` (`src/MusicAlbums.Infrastructure/Persistence/Repositories/UserRepository.cs`) |
| Where are unit tests? | `tests/MusicAlbums.Tests/Unit/` (incl. `ProviderGuardTests`, `ConcurrencyTests`) |
| Where are integration tests? | `tests/MusicAlbums.Tests/Integration/` |
| Where are the BDD features and steps? | `tests/MusicAlbums.ApiTests/Features/*.feature`, `StepDefinitions/ApiSteps.cs` |
| Where is the shared test harness (stubs, SQLite in-memory, WebApplicationFactory)? | `tests/MusicAlbums.TestSupport/` |
| Where is the live smoke script? | `scripts/smoke.ps1` |
| Where is the container setup? | `Dockerfile`, `docker-compose.yml`, `.dockerignore` |
| Where are the Kubernetes manifests? | `deploy/k8s/*.yaml` + `deploy/k8s/README.md` |
| Where is the verification evidence? | `DomainKnowledge/verification/` (TRX, live logs, per-version records) |

## 2. Questions they ask, answers you give

| Question | Answer skeleton (cite files) |
| --- | --- |
| "Walk me through a request end to end." | `LibraryEndpoints` binds -> `ILibraryService` (`LibraryService.cs`) resolves the provider via `IAlbumProviderFactory` -> `DeezerAlbumProvider.GetAlbumAsync` (HTTP + `DeezerAlbumMapper`) -> `IUserRepository.GetOrCreateAsync` -> `ISavedAlbumRepository` -> `TranslatingUnitOfWork.SaveChangesAsync` -> `ContractMappings` -> 201. |
| "Show me the Strategy pattern." | `IAlbumProvider`; implementations `DeezerAlbumProvider`, `SpotifyAlbumProvider`, `FakeSpotifyAlbumProvider`; selected by `AlbumProviderFactory`; decorated by `ThrottledAlbumProvider`. |
| "Add a third provider." | Implement `IAlbumProvider` under `Providers/<Name>/`, register in `DependencyInjection.cs` (typed `HttpClient`), optionally set `AlbumProviders:Default`. No endpoint/schema change. |
| "How is a user created? What if two requests race?" | `UserRepository.GetOrCreateAsync`: find, else insert; unique NOCASE index catches the race, the loser detaches and re-reads (`SqliteErrors.IsUniqueConstraintViolation`). |
| "How do you prevent duplicate albums?" | Unique index `(UserId, ProviderName, ProviderAlbumId)`; pre-check `ExistsAsync`; DB-level violation translated to `AlbumAlreadyInLibraryException` -> 409 in `TranslatingUnitOfWork`. |
| "What happens when Deezer is down or slow?" | `HttpRequestException`/timeout -> `AlbumProviderUnavailableException` -> 502; `ProviderGuard` caps concurrency and opens a circuit for `BreakDurationSeconds` after `FailureThreshold` failures. Tests: `ProviderGuardTests`. |
| "How is the DB schema defined?" | Entity configurations (`Configurations/`): NOCASE user name, unique indexes, cascade deletes, UTC-ticks converter for `DateTimeOffset`; migrations in `Migrations/`. |
| "How do migrations run?" | Automatically at startup (`Program.cs`); add via `dotnet tool run dotnet-ef migrations add <Name> ...`. |
| "How are secrets handled?" | `appsettings.json` for non-secrets; user-secrets locally; env vars in Docker; K8s Secret (optional ref). Options validated on start (`DependencyInjection.cs`). |
| "Why is Spotify fake by default?" | `Mode=Fake` keeps demos/tests deterministic; `Mode=Api` uses the real adapter; live access blocked by Spotify's Feb 2026 Premium policy (`verification/V3-verification.md`). |
| "How is it tested?" | 64 tests: 50 unit/integration (`tests/MusicAlbums.Tests`), 14 BDD scenarios (`tests/MusicAlbums.ApiTests`); real pipeline with stubbed provider transports (`TestSupport`); live gate `scripts/smoke.ps1`; evidence in `DomainKnowledge/verification/`. |
| "Where is the frontend and why there?" | `wwwroot` served by the API - one container, same origin, no CORS. |
| "How does Kubernetes run it?" | `deploy/k8s/`: Deployment (1 replica - SQLite single writer), live/ready probes, PVC for `/data`, NodePort 30080, optional Secret. |
| "Why SQLite?" | Zero-setup local/demo, snapshot semantics; provider swap is one line in `DependencyInjection.cs`. |
| "Where does HTTP error mapping live?" | `MusicAlbumsExceptionHandler.cs`: 400/404/409/502 table; ProblemDetails everywhere. |
| "What would you do next?" | Honest list: optional API-key auth, bulk add endpoint, PostgreSQL for horizontal scale, CI pipeline, retry/backoff tuning. |

## 3. The five-minute pitch (skeleton to memorize)

> It's a per-user album library in .NET 10. Three projects: Core holds the
> domain and use cases with zero dependencies, Infrastructure adapts Deezer and
> Spotify behind an `IAlbumProvider` strategy plus EF Core/SQLite, and the API is
> the composition root with minimal endpoints and a small demo UI. Saving an
> album fetches details from the provider, snapshots them with tracks and preview
> URLs, and reads never touch the network. Provider failures are isolated with a
> throttle/circuit-breaker decorator. It ships with 64 tests (unit, integration,
> BDD), live smoke scripts, Docker and Kubernetes manifests, and every release is
> verified with committed evidence.

## 4. Change playbook (if they ask you to modify something)

| Ask | Touch |
| --- | --- |
| New endpoint | `Endpoints/` group + `Contracts/` DTO + method on a Core service |
| New album field | Core model -> provider DTOs/mappers -> `Contracts/` + `ContractMappings` -> EF config -> migration -> tests |
| New provider | `Providers/<Name>/` + one DI registration + test data in `TestSupport` |
| Different error semantics | `MusicAlbumsExceptionHandler` (API-level) or `TranslatingUnitOfWork` (DB-level) |
| Rate limits/resilience | `ProviderThrottleOptions` + `ProviderGuard` |
