# MusicAlbums on Kubernetes (Docker Desktop)

Deploys the API (frontend included) to the local Docker Desktop cluster.
Everything is self-contained: SQLite lives on a PersistentVolumeClaim and the
app boots without any credentials (Spotify stays in `Fake` mode).

## Manifests

| File | Contents |
| --- | --- |
| `namespace.yaml` | `musicalbums` namespace |
| `configmap.yaml` | Non-sensitive configuration (default provider, Spotify mode, SQLite path) |
| `secret.example.yaml` | Template for Spotify credentials and `Mode=Api`; not applied by default |
| `pvc.yaml` | 1Gi volume for `/data` (SQLite) |
| `deployment.yaml` | 1 replica (SQLite = single writer), probes, resources, non-root + fsGroup |
| `service.yaml` | NodePort `30080` -> container `8080` |

## Deploy

```powershell
# 1. Build the image the cluster will use (Docker Desktop shares its image store with Kubernetes)
docker build -t musicalbums-api:local .

# 2. Apply the manifests
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml -f deploy/k8s/pvc.yaml -f deploy/k8s/deployment.yaml -f deploy/k8s/service.yaml

# 3. Wait for the rollout
kubectl -n musicalbums rollout status deployment/musicalbums-api --timeout=180s
kubectl -n musicalbums get pods,svc
```

Open <http://localhost:30080> for the UI and
<http://localhost:30080/health/ready> for readiness.

## Verify

```powershell
.\scripts\smoke.ps1 -BaseUrl http://localhost:30080
```

## Optional: real Spotify credentials

```powershell
kubectl -n musicalbums create secret generic musicalbums-secrets `
  --from-literal=AlbumProviders__Spotify__Mode=Api `
  --from-literal=AlbumProviders__Spotify__ClientId=<id> `
  --from-literal=AlbumProviders__Spotify__ClientSecret=<secret>

kubectl -n musicalbums rollout restart deployment/musicalbums-api
```

The deployment references the secret as **optional**, so the app runs without it.
Spotify live access requires an active Premium subscription on the app owner's
account (Spotify's February 2026 Development Mode policy).

> Production note: Kubernetes Secrets are only base64-encoded. For GitOps use
> SealedSecrets/ExternalSecrets or enable etcd encryption at rest.

## Operations

```powershell
kubectl -n musicalbums logs -f deployment/musicalbums-api
kubectl -n musicalbums delete pod -l app=musicalbums-api   # reschedules; data survives on the PVC
kubectl delete namespace musicalbums                        # full teardown (deletes the PVC too)
```

## Why one replica?

SQLite allows a single writer. The Deployment intentionally runs one replica with
the `Recreate` strategy. Horizontal scaling would require moving to a
client/server database (the persistence layer is provider-swappable).
