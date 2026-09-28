# Architecture, Explained (beginner-friendly)

Written as a companion to `10-code-walkthrough.md` (which assumes you already know
the vocabulary). This document explains the *big* ideas in plain language, expands
every abbreviation used in this repo, and answers the questions an interviewer is
likely to ask about running and evolving the system.

---

## 0. Glossary

Programming / .NET

| Term | Meaning |
| --- | --- |
| **.NET / CLR / BCL** | .NET is the platform; the CLR (Common Language Runtime) executes your compiled C#; the BCL (Base Class Library) is the shipped toolbox (`System.*`, `Microsoft.Extensions.*`). |
| **csproj** | The project file. It declares the project's language, target framework and its package references. MSBuild reads it to produce the output. |
| **Assembly** | One compiled .dll (or the runnable .exe/apphost). Our solution has three: `MusicAlbums.Core.dll`, `MusicAlbums.Infrastructure.dll`, `MusicAlbums.Api.dll`. "One deployable, three assemblies" means one shipped artifact containing all three. |
| **POCO** | "Plain Old C# Object" - a class with only properties, no database or serialization attributes needed. Our domain models are POCOs. |
| **DTO** | "Data Transfer Object" - a class that exists only to shape data crossing a boundary (e.g. over HTTP). `AlbumResponse` is a DTO; it is not persisted. |
| **DI / IoC** | "Dependency Injection / Inversion of Control" - classes receive their collaborators through their constructor instead of creating them (`new`) themselves, so the container (ASP.NET Core) decides what the concrete types are. |
| **I/O** | "Input/Output" - anything that leaves or enters the process: HTTP calls, database queries, file access. This is the stuff that is slow, flaky, and hard to test; our abstractions exist mostly to isolate it. |
| **SRP / OCP / DIP** | Single Responsibility (one reason to change), Open/Closed (open to extension, closed to modification), Dependency Inversion (depend on abstractions, not concretions). The SOLID principles. Our layer split and the provider seam are the concrete instances of these. |
| **nullable reference types** | `string?` means "may be null". Enabling `Nullable` makes the compiler enforce that, which is why the build is warning-free. |
| **collection expression** | C# 12 sugar for building lists concisely: `[track1, track2]`. |

Data / persistence

| Term | Meaning |
| --- | --- |
| **EF Core** | Entity Framework Core - Microsoft's ORM (Object-Relational Mapper). You write C# classes; EF generates SQL, tracks changes, and applies migrations. |
| **ORM** | Object-Relational Mapper - the category of tools EF Core belongs to. |
| **DbContext** | EF's main class. It holds your DbSets (tables), holds the connection, and tracks pending changes. |
| **DbSet\<T\>** | The EF handle for a table, e.g. `DbSet<SavedAlbum>`. |
| **Migration** | A versioned, generated script describing a schema change (`CREATE TABLE`, `ADD COLUMN`). Applying them in order upgrades the database. |
| **Repository** | A small interface + class that hides database queries behind domain-shaped methods (`Add`, `ExistsAsync`). |
| **Unit of Work** | The object that commits a batch of changes as one transaction. Our DbContext *is* the unit of work, so "save everything once" is one call. |
| **AsNoTracking** | EF query option: return plain objects without wiring up change tracking. Faster for read-only queries. |
| **Include / eager loading** | Fetching related data (a saved album's tracks) in the same round trip instead of a second query per album. |
| **SQLite** | An embedded, file-based database engine. No server, no installation - the whole database is one file. |
| **NOCASE** | A SQLite collation (text comparison rule) that treats "A" and "a" as equal. We apply it to the user name so "Alice" and "alice" are the same person. |
| **UTC ticks** | A timestamp stored as a single 64-bit integer counting 100-nanosecond intervals since 0001-01-01 UTC. Integers sort correctly and compactly, which is why we use them for `SavedAt`/`CreatedAt` (see `04-persistence.md`). |
| **Value converter** | An EF feature that translates a CLR property type on the way to/from the database (e.g. `DateTimeOffset` <-> `long` ticks). |
| **RWO** | "ReadWriteOnce" - a PersistentVolumeClaim access mode meaning the volume can be mounted read-write by exactly one node at a time. |
| **PVC / PV** | PersistentVolumeClaim (the *request* for storage) / PersistentVolume (the actual storage). You write the PVC; the cluster provides the PV. |
| **DDL** | "Data Definition Language" - the SQL that creates tables (`CREATE TABLE ...`). |

Providers / integrations

| Term | Meaning |
| --- | --- |
| **Strategy pattern** | One interface, several interchangeable implementations chosen at runtime (our Deezer vs Spotify vs fake). |
| **Provider** | Our domain name for a Strategy. In .NET the word is also used for other things (configuration providers, logger providers); here it means "a music catalogue integration". |
| **Factory / registry** | An object that builds or looks up things. `AlbumProviderFactory` resolves a provider *by name* at runtime. |
| **DI lifetime** | How long a registered object lives: `Singleton` (whole app), `Scoped` (one request), `Transient` (every time it's asked for). Getting this right matters for performance and for thread safety. |
| **Typed HttpClient** | Registering `AddHttpClient<MyProvider>()` so the provider gets an `HttpClient` with configured base URL and timeout injected by the framework, instead of calling `new HttpClient()` (which causes socket exhaustion). |
| **JWT / OAuth** | JSON Web Token / OAuth 2.0 - the standard ways third parties grant access. Spotify uses "client credentials" (server-to-server, no user login). |
| **TTL** | "Time To Live" - how long something stays valid. Deezer preview URLs are *signed* and have a TTL (they expire), which is why the UI hides the play button if a link dies. |
| **HMAC** | A keyed hash used by Deezer to sign preview URLs; the query string we saw (`hdnea=...hmac=...`) is the proof. |
| **Rate limit** | A provider's cap on how many requests you may make per second/minute. Exceeding it yields HTTP 429. This is the one design area we have documented but *not* yet implemented (see section 7). |
| **Circuit breaker** | After repeated failures, stop calling a broken provider for a while and fail fast, instead of piling up timeouts. |
| **Resilience / Polly** | Microsoft's `Microsoft.Extensions.Http.Resilience` library (Polly under the hood) for retries, timeouts, circuit breakers. |

HTTP / API

| Term | Meaning |
| --- | --- |
| **Minimal API** | ASP.NET Core's low-boilerplate style: `app.MapGet("/route", handler)` instead of controller classes. |
| **Route / endpoint** | A URL path + method (e.g. `GET /api/albums/search`) mapped to a handler function. |
| **DTO mapping** | Converting domain objects into response DTOs so the wire format is decoupled from the internal model. |
| **ProblemDetails (RFC 9457)** | A standard JSON error body: `{ "title", "status", "detail", "traceId" }`. `application/problem+json`. |
| **Status codes** | 200 OK, 201 Created, 204 No Content, 400 Bad Request, 404 Not Found, 409 Conflict, 502 Bad Gateway. |
| **OpenAPI** | A machine-readable description of the API (the document at `/openapi/v1.json`). |
| **CORS** | "Cross-Origin Resource Sharing" - the browser rule that blocks a page on host A from calling host B. We avoid needing it because the UI is served by the API itself (same origin). |
| **SPA** | "Single-Page Application" - a frontend that stays on one HTML page and talks to an API via JavaScript. Our demo UI is a tiny SPA. |

Quality / testing

| Term | Meaning |
| --- | --- |
| **Unit test** | Tests one class in isolation with fakes (no HTTP, no database server). Fast, many. |
| **Integration test** | Tests real components together (HTTP pipeline + EF Core + adapters) with only the true externals faked. |
| **BDD / Gherkin** | "Behavior-Driven Development" - tests written as human-readable `Given/When/Then` sentences (`.feature` files) that map to code step definitions. Reqnroll is the .NET tool that runs them. |
| **Acceptance test** | Verifies the API does what a user needs, usually over HTTP. Our BDD suite is exactly this. |
| **E2E** | "End to End" - exercising the whole system including real third parties. Our `scripts/smoke.ps1` against live Deezer. |
| **SUT** | "System Under Test" - the thing being tested. |
| **TRX** | The XML test-result format `dotnet test` can emit (`--logger trx`). We commit these as evidence. |
| **Arrange/Act/Assert** | The three phases of a test: set up, do the thing, assert. |
| **Test double / stub / fake** | Stand-ins for real dependencies. "Stub" = returns canned answers. "Fake" = a working in-memory version. |
| **Determinism** | Same result every run - no network, no clock, no randomness. Our test harness is fully deterministic. |
| **Code coverage** | The percentage of lines executed by tests. The assignment explicitly does *not* want 100% - they want deliberate choices. |
| **Flaky test** | A test that passes/fails randomly. A design smell; ours are deterministic by construction. |

Delivery / operations

| Term | Meaning |
| --- | --- |
| **CI** | "Continuous Integration" - automatically building and testing on every push. (Not currently set up; noted as a deliberate omission.) |
| **Container / image** | An image is a packaged filesystem + runtime; a container is a running instance of it. |
| **Dockerfile** | The build recipe for an image (base image, copy files, restore, publish, run). |
| **Docker Compose** | A YAML file describing containers to run *together on one machine* (a `docker compose up`). |
| **Docker Desktop's Kubernetes** | A single-node Kubernetes cluster shipped inside Docker Desktop (a k3s-based distribution). One `docker build` image store is shared with the cluster. |
| **Kubernetes (k8s)** | A cluster orchestrator: you submit desired state; controllers make reality match, and keep it matching. |
| **API server / etcd** | The Kubernetes control plane: the API server is where you submit YAML; etcd is the database storing cluster state. |
| **Pod** | The smallest schedulable unit - one or more containers sharing a network namespace and a lifecycle. |
| **Node** | A machine in the cluster. In our case, the single Docker Desktop node. |
| **kubelet** | The agent on each node that actually runs your pods' containers and reports health. |
| **Controller / ReplicaSet** | A controller watches desired vs actual and acts. The ReplicaSet's job is literally "keep N copies of this pod running". |
| **Deployment** | A YAML resource that manages a ReplicaSet for you: replicas, rolling updates, rollback. |
| **rollout status** | A `kubectl` command that blocks until the new pods are ready. |
| **Labels / selectors** | Key-value tags on pods; a Deployment finds its pods by matching labels. |
| **Service** | A stable in-cluster address/port in front of changing pods. Types: `ClusterIP` (internal only), `NodePort` (opens a port on the node - what we use), `LoadBalancer` (cloud). |
| **Ingress** | An HTTP router in front of Services for path/host-based public routing. We deliberately skip it (nothing to route but one service). |
| **Probes** | Periodic HTTP calls the kubelet makes: `livenessProbe` (failing it restarts the pod), `readinessProbe` (failing it removes the pod from Service traffic). |
| **Recreate strategy** | During an update, delete the old pod before creating the new one - required for single-writer storage like our SQLite file. |
| **fsGroup** | A group ID the kubelet applies to a mounted volume so a non-root container user can write to it. |
| **resource requests/limits** | CPU/memory reserved and capped for a container (QoS). |
| **Namespace** | A logical partition of cluster resources; deleting it deletes everything inside (a tidy teardown). |
| **kubectl** | The CLI that talks to the cluster. |
| **kubectl apply** | "Make reality match this YAML" (declarative and repeatable - applying twice is harmless). |
| **IaC** | "Infrastructure as Code" - describing infrastructure in version-controlled files instead of clicking it in a UI. Our k8s YAML *is* IaC. Terraform/Helm/Kustomize are richer IaC tools we do not use yet. |
| **Helm** | A package manager/template engine for k8s YAML (values per environment). |
| **Kustomize** | A lighter alternative to Helm: a base manifest plus per-environment overlays, rendered by `kubectl apply -k`. Bundled with kubectl. |
| **Manifest** | A YAML file describing one k8s resource (Deployment, Service, ...). |
| **TuxComp** | *Your* tool (a Python CLI) that runs compose-style stacks on Termux phones via proot. We discussed it and then dropped it from the roadmap - it added no value to the interview. |
| **ADR** | "Architecture Decision Record" - a short note capturing a decision and its rationale. Ours is the decision log (`07-decision-log.md`), now 35 entries. |

---

## 1. Would each provider be its own container on Kubernetes?

**Today: no.** There is **one Deployment, one Pod, one container** (the API image) and
inside that one process both providers are registered. Kubernetes knows nothing about
Deezer or Spotify; it only runs our API and restarts it if it fails.

**Could it be? Yes, and the seam is already there.** The interesting question is
*where the boundary sits* - and the answer is `IAlbumProvider`. Because the API and
use cases depend on that interface and not on a concrete class, you can replace the
in-process adapter with a **remote adapter** that calls a separate "Deezer
connector" service over HTTP, without touching a single endpoint, service, or
schema:

```csharp
// today: in-process
services.AddHttpClient<DeezerAlbumProvider>(...);
services.AddTransient<IAlbumProvider>(sp => sp.GetRequiredService<DeezerAlbumProvider>());

// a future split: remote connector (illustrative, not implemented)
services.AddHttpClient<HttpDeezerAlbumProvider>(c => c.BaseAddress = new Uri("http://deezer-connector"));
services.AddTransient<IAlbumProvider>(sp => sp.GetRequiredService<HttpDeezerAlbumProvider>());
```

**What you gain by splitting each provider into its own deployment:** independent
release cadence (ship a Deezer fix without redeploying core), independent scaling
(Spotify search is heavier), a real security boundary (that container holds the
Spotify secret and nothing else), and per-provider resource limits.

**What it costs:** a network hop adds latency and a new failure mode; service-to-service
authentication becomes necessary; tracing and debugging span two processes; and you
now own two more deployment manifests plus a real database-per-service story.

**My recommendation:** stay as one deployable until a concrete pressure appears
(independent release cadence or a genuine scale difference), because the interface
makes the split cheap when it does. That is precisely the kind of trade-off the
assignment asks you to make explicit.

---

## 2. How do we deploy to Kubernetes, step by step?

Deploying = "make the cluster's reality match this YAML."

**Step 1 - build the image** (k8s runs containers; the image is what it runs):

```bash
docker build -t musicalbums-api:local .
```

Our image is built by the **same Dockerfile** that Docker Compose uses. That is the
key insight: Docker/k8s are not alternatives for *building*, only for *running*.

**Step 2 - make sure the cluster can see the image.** We verified Docker Desktop's
built-in Kubernetes shares the Docker image store, so a locally built tag just works
(`imagePullPolicy: IfNotPresent`). On minikube/kind you would instead run
`minikube image load musicalbums-api:local` or push to a registry. Nothing in our
YAML changes - only how the image gets there.

**Step 3 - apply the manifests in dependency order** (namespace first, because every
other object must live inside it):

```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml -f deploy/k8s/pvc.yaml \
              -f deploy/k8s/deployment.yaml -f deploy/k8s/service.yaml
```

`apply` is declarative and idempotent: run it twice and nothing breaks.

**Step 4 - watch it converge.** `kubectl apply` returns almost instantly; the
*controllers* do the work asynchronously:

```
Deployment applied
   -> Deployment controller creates a ReplicaSet (the "keep 1 copy alive" controller)
      -> ReplicaSet creates a Pod; the scheduler picks a node; the kubelet starts the container
```

```bash
kubectl -n musicalbums rollout status deployment/musicalbums-api
kubectl -n musicalbums get pods,svc,pvc
```

**Step 5 - the probes take over.** Every 10s the kubelet calls our health endpoints:

- `/health/live` failing -> the container is **restarted** (it is broken).
- `/health/ready` failing -> the pod is marked unready and the Service stops routing
  to it (it cannot serve), but it is **not** restarted.

**Step 6 - reach it.** Our `Service` is a `NodePort` on 30080, so Docker Desktop maps
it to `http://localhost:30080`. Alternatives: `kubectl port-forward svc/musicalbums-api 8080:80`,
or an `Ingress` for real public routing.

**Step 7 - operate it.**

```bash
kubectl -n musicalbums logs -f deployment/musicalbums-api   # logs
kubectl -n musicalbums delete pod -l app=musicalbums-api    # force a restart; PVC keeps data
kubectl -n musicalbums rollout undo deployment/musicalbums-api  # undo the last change
kubectl delete namespace musicalbums                         # full teardown, PVC included
```

**What self-heals automatically:** container crash, pod deletion, node reboot,
replica count, rolling updates, and service routing. **What does not:** the database
file, configuration mistakes, and application bugs.

---

## 3. Where is our IaC (Infrastructure as Code)?

**We have IaC - in layers - but not the full "cloud provisioning" discipline.**

| Layer | Our file | What it declares |
| --- | --- | --- |
| Dependencies | `Directory.Packages.props` | every NuGet version, centrally |
| Tooling | `dotnet-tools.json` | the `dotnet-ef` version (pinned) |
| Build | `Dockerfile` | how the runtime artifact is produced |
| Local runtime | `docker-compose.yml` | the single-host topology (one service, one volume, one port) |
| App config, non-secret | `appsettings.json` | defaults + provider modes |
| App config, cluster | `deploy/k8s/configmap.yaml` | the same, per environment |
| App config, secret | `deploy/k8s/secret.example.yaml` | *shape* only; real values via `kubectl create secret` |
| Cluster runtime | `deploy/k8s/*.yaml` | namespace, storage, deployment, service |

**What we do not have, and would add in production:** Terraform (to create VPCs,
nodes, load balancers - infrastructure *before* k8s exists), Helm (one parameterized
chart instead of six files, values per environment), Kustomize overlays
(`base/` + `overlays/dev|prod`, rendered by `kubectl apply -k` - and kustomize is
already bundled with our kubectl, so this is a cheap next step), drift detection
(does reality still match the files?), and promotion between environments.

The honest framing for an interview: *our YAML is the application-level IaC; cluster
lifecycle IaC (Terraform/Helm) is a deliberate next step, not an omission of
understanding.*

---

## 4. Docker Compose vs Kubernetes - what is the difference?

They answer the same question - "what should be running?" - at different scales, with
the **same** Dockerfile underneath.

| | Docker Compose | Kubernetes |
| --- | --- | --- |
| Scope | One machine, one Docker engine | Cluster of nodes, many machines |
| Who does the work | `docker compose up` directly creates/starts containers | You submit desired state; controllers converge; kubelet runs containers |
| Topology | A compose file per project | Resources (Deployments/Services/etc.) in any number of namespaces |
| Self-healing | No (a crashed container stays crashed unless `restart: always` + docker restarts it) | Yes - ReplicaSet recreates pods, Deployment reschedules them |
| Scaling | `docker compose up --scale api=3` (manual) | change `replicas`, or an HPA (auto) based on metrics |
| Networking | One bridge network per project, ports published to the host | Service DNS, network policies, ingress controllers |
| Config & secrets | `.env` files, env vars | ConfigMaps, Secrets, optional refs, external secret managers |
| Rolling updates | Recreate by hand | `kubectl rollout`, health-gated, reversible |
| Best for | Development, CI, small single-box demos | Production, multi-node, HA, many services/teams |

**Analogy:** Compose is "start these programs on my laptop". Kubernetes is "submit a
job description to a facilities team; they keep the lights on, replace broken
machines, and tell you when something is unhealthy".

Practically, for this project: Compose and k8s are **not** alternatives we must
choose between at build time. `docker compose up` runs the same image on one host;
`kubectl apply` runs the same image in a cluster. Compose is the fast inner loop for
development, k8s the supervised environment.

---

## 5. Are we one deployable with three assemblies? Is going to microservices "just a compose file change"?

**First half - correct.** One deployable (one container image, one Deployment)
containing three assemblies: `Core.dll` and `Infrastructure.dll` are libraries
compiled into the same image; `MusicAlbums.Api.dll` is the entry point the container
executes. That shape is called a **modular monolith** - one deployable unit, but
internally split into modules with an enforced dependency direction. (The assignment
calls it "a microservice"; the practical reading is "one service", and one service is
one deployment.)

**Second half - this is the important correction: no, it is not just a compose file
change.** The *code* seams make it feasible; the *operational* work is substantial.
Going from one deployable to N microservices means you must:

1. **Choose the boundary** (which of our three assemblies becomes a service? - the
   provider adapters are the natural first candidate).
2. **Turn interface calls into network calls.** `IAlbumProvider.GetAlbumAsync(...)`
   becomes an HTTP/gRPC client call. Same interface, new implementation, but now it
   has a network in between, so you must add timeouts, retries, and error handling
   for *every* call, plus serialization of the contract.
3. **Authenticate service-to-service** (a shared secret token, mTLS, or a service
   mesh) - on one host, process calls needed no credentials.
4. **Split the data.** The fundamental change: cross-service joins stop being
   possible. The library feature needs provider data + saved albums; that query can no
   longer be a single SQL join across both, so you either duplicate data
   (eventual consistency) or accept a distributed read.
5. **Ship N images and N sets of manifests**, N CI pipelines (or one pipeline, N
   artifacts), and N dashboards/alerts.
6. **Operate the new failure modes**: a slow connector now shows up as user-facing
   latency; a partial outage becomes a possibility; debugging spans processes
   (tracing/correlation IDs).

**What stays the same, and this is the payoff of the current design:** the domain
contracts, the provider abstraction, the HTTP API surface, the response shapes, the
error model, and nearly all the tests (they move to contract tests at the boundary).
The seams were placed for this.

**Also worth saying out loud in an interview:** for a per-user album library, N
services is *over*-engineering. A modular monolith is the right answer at this
scale; knowing exactly what you would change, and why you have not yet, is the
signal they are looking for.

---

## 6. The duplicate/concurrency issue - precisely what it is

**First, the case you asked about, which is NOT a problem:** two *different* users
saving the *same* album. Each user has their own row, keyed by `UserId`; the uniqueness
constraint is `(UserId, ProviderName, ProviderAlbumId)`, so Alice's Discovery and
Bob's Discovery are two different rows and both succeed. We have a test for this
(`AddAlbumAsync_SameAlbumForDifferentUsers_KeepsLibrariesSeparate`) and the running
API demonstrates it.

**The actual problem is the same user, same album, two requests at the same time.**
Think of a user double-clicking **Save**, or two browser tabs saving the same album
simultaneously. The save is a check-then-act sequence:

```
Request A                          Request B
---------                          ---------
ExistsAsync? -> NO                 (A has not committed yet)
Add(album)                         ExistsAsync? -> NO   (B sees the same empty state)
SaveChanges                        Add(album)
                                    SaveChanges
```

Both requests saw "it is not in the library". Both try to insert. **The database's
unique index then rejects the second insert** - so **no duplicate can ever exist**
(correctness is safe), but the loser receives a raw database exception, which our
exception handler maps to a generic **500** instead of the friendly **409 Conflict**
the sequential path returns.

So the honest summary: **not a data-integrity bug, a UX/robustness gap.** The fix is
small - catch `DbUpdateException` (and SQLite's "UNIQUE constraint failed") around
`SaveChangesAsync` and translate it to `AlbumAlreadyInLibraryException` (409) - or
serialize writes per user. I would fix it before adding retry logic, and I have it
logged as the next recommended delivery.

---

## 7. Rate limits: what the assignment asks, and what we do today

The assignment explicitly asks: *"How would you handle differences in their data and
rate limits?"*

**Data differences - solved today.** Every provider normalizes into the same
`ProviderAlbum`/`ProviderTrack` shape; each provider has a dedicated mapper for its
quirks (Deezer's HTTP-200 error envelope, Spotify's `release_date_precision`,
Deezer's signed previews). Callers never see provider-specific shapes.

**Rate limits - designed, not yet implemented.** Today: a 15s HTTP timeout per call,
and we *respect* provider-declared limits where documented (Spotify search is clamped
to 10 per its Dev Mode policy; Deezer's ~50 requests / 5 seconds is documented in
`03-deezer-integration.md`). What is missing:

1. **Client-side rate limiting** - a per-provider limiter so *we* never exceed the
   provider's cap. .NET has this in-box (`System.Threading.RateLimiting`): a
   `ConcurrencyLimiter` (cap in-flight requests per provider) plus a token-bucket for
   requests-per-second, registered per provider name.
2. **Circuit breaking** - if a provider starts failing, stop calling it for a while
   and return 502 fast instead of piling up 15s timeouts
   (`Microsoft.Extensions.Http.Resilience` / Polly).
3. **Retry with jitter** - only for idempotent GETs, honoring `Retry-After`.
4. **Short-lived caching** - the same album gets fetched repeatedly (search then save);
   a small in-memory cache per provider cuts both latency and rate-limit consumption.
5. **Observability** - log provider call counts/latency so limits are tuned with data,
   not guesses.

**Why it is not in yet:** it is real complexity guarding a problem we have not
observed (Deezer has never throttled us in live testing), and the assignment warns
against gold-plating. It is the first thing I would add next, and the design above
is the plan.

---

## 8. Quick orientation map (what to open, in what order)

1. `src/MusicAlbums.Core/Abstractions/IAlbumProvider.cs` - the seam (8 lines).
2. `src/MusicAlbums.Core/Application/LibraryService.cs` - the use case (62 lines).
3. `src/MusicAlbums.Infrastructure/DependencyInjection.cs` - the wiring (72 lines).
4. `src/MusicAlbums.Infrastructure/Providers/Deezer/DeezerAlbumProvider.cs` - a real integration (74 lines).
5. `src/MusicAlbums.Infrastructure/Providers/Spotify/SpotifyTokenProvider.cs` - concurrency (81 lines).
6. `src/MusicAlbums.Infrastructure/Persistence/Configurations/` - the data model.
7. `tests/MusicAlbums.TestSupport/MusicAlbumsApiFactory.cs` - the test harness trick (49 lines).
8. `deploy/k8s/deployment.yaml` - the runtime contract with the cluster (63 lines).
9. `DomainKnowledge/07-decision-log.md` - 35 decisions with rationale.
10. `DomainKnowledge/verification/` - proof it ran.
