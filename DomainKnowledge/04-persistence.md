# Persistence (EF Core 10 + SQLite)

The library is a **local snapshot**: saving an album copies the provider metadata
into our database, so library reads never depend on a third party being online.

## Model

```
Users                         SavedAlbums                           SavedTracks
─────                         ───────────                           ───────────
Id        GUID PK             Id             GUID PK                Id              GUID PK
Name      TEXT unique NOCASE  UserId         FK -> Users (cascade)  SavedAlbumId    FK -> SavedAlbums (cascade)
CreatedAt INTEGER (UTC ticks) ProviderName   TEXT                   Position        INTEGER
                              ProviderAlbumId TEXT                  Title           TEXT
                              Title          TEXT                   DurationSeconds INTEGER?
                              ArtistName     TEXT
                              ReleaseDate    TEXT (DateOnly?)
                              CoverImageUrl  TEXT?
                              TrackCount     INTEGER?
                              SavedAt        INTEGER (UTC ticks)
```

Constraints and indexes:

- `Users.Name` unique with SQLite `NOCASE` collation, so `alice` and `Alice` are
  the same user (also enforces one library per person).
- `SavedAlbums` unique index on `(UserId, ProviderName, ProviderAlbumId)` - the
  database guarantees "an album cannot be saved twice for the same user".
- Deleting a user cascades to albums, deleting an album cascades to tracks.

## SQLite specifics worth remembering

- **`DateTimeOffset` cannot be used in `ORDER BY` on SQLite** (EF throws
  `NotSupportedException`). `SavedAt` and `CreatedAt` are stored as
  `UtcTicks` (`long`, INTEGER) through a value converter. This is
  order-preserving, so "newest saved first" works in SQL.
- `DateOnly` is stored as TEXT and needs no conversion.
- SQLite ignores `HasMaxLength` values, but they document intent and are enforced
  if the database is ever swapped for SQL Server/PostgreSQL.

## Migrations

The `dotnet-ef` tool is pinned repo-locally in `dotnet-tools.json`
(no global install needed):

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> `
  --project src/MusicAlbums.Infrastructure `
  --startup-project src/MusicAlbums.Api `
  --output-dir Persistence/Migrations
```

`Program.cs` applies migrations automatically at startup, so a fresh clone just
runs. Migrations live in `src/MusicAlbums.Infrastructure/Persistence/Migrations`.

## Connection string

`appsettings.json`:

```json
"ConnectionStrings": { "Library": "Data Source=musicalbums.db" }
```

`AddInfrastructure` throws at startup if it is missing. The database file is a
local artifact and is gitignored.

## Why the database provider is swappable

Only `AddInfrastructure` knows about `UseSqlite`. Swapping to SQL Server or
PostgreSQL means changing that one call (and regenerating migrations); the
repositories, services and tests (which use SQLite in-memory) are unaffected.
