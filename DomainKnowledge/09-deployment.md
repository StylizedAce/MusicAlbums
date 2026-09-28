# Deployment Levels

The service is verified at three levels, each self-contained.

## 1. Local (native)

```powershell
dotnet tool restore
dotnet build
dotnet test
dotnet run --project src/MusicAlbums.Api --launch-profile http
```

- API + frontend: <http://localhost:5080> (UI at `/`, OpenAPI at `/openapi/v1.json`)
- Database: `src/MusicAlbums.Api/musicalbums.db` (created + migrated on startup, gitignored)

## 2. Docker

```powershell
docker compose up --build
.\scripts\smoke.ps1
```

- Port `5080:8080`, named volume `musicalbums-data:/data` (SQLite).
- Details: [08-docker.md](08-docker.md); evidence: `verification/V2-live-container-smoke.txt`.

## 3. Kubernetes (Docker Desktop)

```powershell
docker build -t musicalbums-api:local .
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml -f deploy/k8s/pvc.yaml -f deploy/k8s/deployment.yaml -f deploy/k8s/service.yaml
kubectl -n musicalbums rollout status deployment/musicalbums-api --timeout=180s
```

- UI + API: <http://localhost:30080> (NodePort), readiness: `/health/ready`.
- 1 replica with the `Recreate` strategy because SQLite allows a single writer
  (the persistence layer is provider-swappable if that ever changes).
- The Deployment runs non-root (UID 1654) with `fsGroup: 1654` so the PVC is
  writable, and probes both health endpoints.
- Full guide: [deploy/k8s/README.md](../deploy/k8s/README.md);
  evidence: `verification/V5-kubernetes.txt`.

## Secrets layering (the conventional arrangement)

| Environment | Non-sensitive config | Sensitive values |
| --- | --- | --- |
| Local dev | `appsettings.json` (committed) | `dotnet user-secrets` (outside the repo) |
| Docker | compose `environment` | `.env` next to compose (gitignored) or `-e` flags |
| Kubernetes | `ConfigMap` | `Secret`, created imperatively with `kubectl create secret` (never committed) |

The Spotify secret reference in the Deployment is **optional**, so the pods boot
and serve entirely self-contained (`Mode=Fake`). Creating the Secret and
restarting the Deployment switches the cluster to the real Spotify adapter
(`Mode=Api`).

> Kubernetes Secrets are only base64-encoded, not encrypted. For GitOps flows use
> SealedSecrets/ExternalSecrets or enable etcd encryption at rest. The same rule
> as everywhere: never commit real credentials.

## Frontend placement

The demo UI is static files inside the API image (`wwwroot`), so all three levels
serve the same single container with same-origin requests. A separate frontend
Deployment (nginx + Service + Ingress, often with CORS) is the conventional
pattern for a full SPA - deliberately out of scope for a throwaway demo UI.
