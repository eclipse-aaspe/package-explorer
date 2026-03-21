# Docker: AASX Package Explorer (Blazor)

Repository: **[eclipse-aaspe/package-explorer](https://github.com/eclipse-aaspe/package-explorer)**  
Gleiches Muster wie beim AAS-Server (`Dockerfile-AasxServerBlazor`): Image aus **Git-URL + Branch** bauen, `linux/amd64`, Tag `develop`, Push zu Docker Hub.

## Image bauen (Remote — Branch **AnyUiAO**)

```bash
docker build --platform linux/amd64 \
  -t adminshellio/aasx-package-explorer-blazor-for-demo:develop \
  -f src/docker/Dockerfile-AasxPackageExplorerBlazor \
  "https://github.com/eclipse-aaspe/package-explorer.git#AnyUiAO"
```

Anderen Branch: Fragment `#BRANCHNAME` anpassen (z. B. `#main`).

## Lokal aus dem geklonten Repo

```bash
cd /pfad/zum/package-explorer
git checkout AnyUiAO

docker build --platform linux/amd64 \
  -t adminshellio/aasx-package-explorer-blazor-for-demo:develop \
  -f src/docker/Dockerfile-AasxPackageExplorerBlazor .
```

## Publish & Aufräumen (wie beim Server)

```bash
docker login docker.io
docker push adminshellio/aasx-package-explorer-blazor-for-demo:develop
docker builder prune -a
```

## Container starten

```bash
docker run --rm -p 8080:8080 adminshellio/aasx-package-explorer-blazor-for-demo:develop
```

→ **http://localhost:8080**

## Hinweise

- **`.dockerignore`** liegt im **Repository-Root** (wichtig für Remote-Build).
- **Release**-Publish → `BlazorExplorer.options-for-release.json` (siehe `.csproj`).
- Optional: `BlazorExplorer.options.json` per Volume mounten.
