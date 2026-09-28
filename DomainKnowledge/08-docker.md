# Running MusicAlbums in Docker

## Files

| File | Purpose |
| --- | --- |
| `Dockerfile` | Multi-stage: SDK 10 build (layer-cached csproj restore) -> aspnet:10.0 runtime |
| `docker-compose.yml` | `api` service, port `5080:8080`, named SQLite volume, healthcheck |
| `.dockerignore` | Trims the build context (bin/obj, tests, docs, databases, secrets) |
| `scripts/smoke.ps1` | Parameterized E2E smoke test used natively and against the container |

## Image design

- Build stage copies `Directory.Build.props` + project files first so `dotnet restore`
  is cached; source is copied afterwards.
- Runtime stage:
  - `curl` installed for the container healthcheck (`curl -fsS http://localhost:8080/health/live`).
  - Runs as the non-root `app` user.
  - `/data` is created and owned by `app`; the named volume inherits that ownership,
    so SQLite can create/write `musicalbums.db`.
  - `ASPNETCORE_HTTP_PORTS=8080`, `EXPOSE 8080`.
  - Default `ConnectionStrings__Library="Data Source=/data/musicalbums.db"`
    (overridable by compose/environment).
- Local image: ~410MB, `amd64/linux`, user `app`.

## Run

```powershell
docker compose up --build        # build + start
docker compose ps                # health should be "healthy" within ~15s
docker compose logs -f api
docker compose down              # stop (keeps the volume)
docker compose down --volumes    # stop and wipe the SQLite volume
```

- API: http://localhost:5080 - health at `/health/live` and `/health/ready`.
- Data: named volume `musicalbums-data` (compose project name is pinned to
  `musicalbums` so container/volume names are stable).

## Configuration and secrets

| Setting | Default in image | Compose | Notes |
| --- | --- | --- | --- |
| `ConnectionStrings__Library` | `Data Source=/data/musicalbums.db` | same | relocate the DB by changing this |
| `AlbumProviders__Default` | `deezer` | `deezer` | provider used when the request omits one |
| `AlbumProviders__Spotify__Mode` | `Fake` | `Fake` | `Api` fails fast until V3 |
| `AlbumProviders__Spotify__ClientId` / `__ClientSecret` | - | `.env` in V3 | never commit; env vars in containers, user-secrets locally |

## Verification evidence

- `DomainKnowledge/verification/V2-live-container-smoke.txt` - container E2E: health
  gate, full smoke (real Deezer), restart persistence with the DB file visible in
  the volume owned by `app`.
- `DomainKnowledge/verification/V2-live-native-smoke.txt` - the same script against
  a native `dotnet run`.

## TuxComp / phone deployment (V3 preview)

TuxComp parses this compose file, supports named volumes, and rewrites the Dockerfile
for proot (drops `USER`, rewrites `WORKDIR`/`COPY`/`CMD`). The V3 delivery adds the
`x-tuxcomp.deploy` block and a phone-targeted compose; this PC is not the deployment
target - the user runs the single deploy command against the phone.
