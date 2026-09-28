# V5 Verification Record

Date: 2026-09-28 (UTC)
Environment: Docker Desktop 28.0.4 with Kubernetes enabled (context `docker-desktop`,
client v1.32.2, node Ready), image `musicalbums-api:local` (final revision including
the demo UI).

## Image visibility

Docker Desktop's Kubernetes shares the local image store; a throwaway pod with
`imagePullPolicy: Never` scheduled and started successfully, so the Deployment uses
the locally built image without any registry. (Fallback documented in 09-deployment.md:
a local registry would have been the alternative.)

## Kubernetes E2E

Raw log: [V5-kubernetes.txt](V5-kubernetes.txt)

```text
=== MusicAlbums V5 Kubernetes E2E ===
Date (UTC): 2026-09-28 08:56:26
Context: docker-desktop
namespace/musicalbums created
configmap/musicalbums-config created
persistentvolumeclaim/musicalbums-data created
deployment.apps/musicalbums-api created
service/musicalbums-api created
deployment "musicalbums-api" successfully rolled out
pod/musicalbums-api-... 1/1 Running
service/musicalbums-api NodePort 80:30080/TCP
persistentvolumeclaim/musicalbums-data Bound 1Gi RWO hostpath
NodePort health: HTTP 200 'Healthy'
frontend at NodePort: HTTP 200 (1448 bytes, contains app.js: True)

Smoke testing http://localhost:30080 (provider=deezer, album=302127)
[ok] providers: deezer, spotify
[ok] search 'daft punk' -> total=95, first='Discovery'
[ok] saved 'Discovery' with 14 tracks for smoke-65dcae9d...
[ok] library contains 'Discovery'
[ok] duplicate save -> 409
[ok] delete -> library empty
SMOKE TEST PASSED (http://localhost:30080)

=== pod restart persistence check ===
before restart: user=k8s-48e7e56c... title='Discovery' tracks=14
pod "musicalbums-api-b987496f-v7ttw" deleted
deployment "musicalbums-api" successfully rolled out
after pod restart: ready=True
after pod restart: library count=1 title='Discovery' externalUrl=https://www.deezer.com/album/302127
cleanup delete -> ok
RESULT: KUBERNETES E2E PASSED
```

What this proves:

- manifests apply cleanly on a stock Docker Desktop cluster;
- rollout, liveness and readiness probes (SQLite-backed `/health/ready`) pass;
- NodePort `30080` serves both the demo UI and the API;
- the live smoke test (real Deezer) passes through the cluster;
- the SQLite volume survives a pod restart (PVC persistence).

## Operations

```powershell
kubectl -n musicalbums get pods,svc,pvc
kubectl -n musicalbums logs -f deployment/musicalbums-api
kubectl -n musicalbums delete pod -l app=musicalbums-api   # reschedules, data survives
kubectl delete namespace musicalbums                        # full teardown (removes the PVC)
```

The cluster deployment was left running for demonstration at
<http://localhost:30080>.
