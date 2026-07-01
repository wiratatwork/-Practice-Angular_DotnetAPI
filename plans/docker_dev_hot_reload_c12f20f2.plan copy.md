---
name: docker dev hot reload
overview: Add a Docker-based development workflow with hot reload for both backend and frontend, while preserving the existing production workflow unchanged.
todos:
  - id: dev-backend-dockerfile
    content: Create `backend/Dockerfile.dev` for SDK-based `dotnet watch` development runtime.
    status: completed
  - id: dev-frontend-dockerfile
    content: Create `frontend/Dockerfile.dev` for Angular dev server with host binding and polling.
    status: completed
  - id: dev-compose-override
    content: Add `docker-compose.dev.yml` to override `api` and `frontend` with bind mounts, dev commands, and dev-friendly volumes.
    status: completed
  - id: docker-proxy-config
    content: Add `frontend/proxy.conf.docker.json` so frontend-in-container proxies `/api` to `http://api:5001`.
    status: completed
  - id: validate-both-modes
    content: Verify the dev overlay hot reload path and confirm the original production compose path still works unchanged.
    status: in_progress
isProject: false
---

# Docker Dev Hot Reload Plan

## Goal

Create a separate dev overlay so that:

- `docker compose -f docker-compose.yml -f docker-compose.dev.yml up` runs a hot-reload development stack
- `docker compose up -d --build` keeps the current production-style behavior

## Current Baseline

- Production compose already defines `postgres`, `api`, `frontend`, and `pgadmin` in [docker-compose.yml](docker-compose.yml).
- Backend is currently a publish-and-run image in [backend/Dockerfile](backend/Dockerfile), so code changes require rebuild.
- Frontend is currently a build-to-Nginx image in [frontend/Dockerfile](frontend/Dockerfile), so code changes also require rebuild.
- Angular dev proxy currently targets `localhost:5001` in [frontend/proxy.conf.json](frontend/proxy.conf.json), which works on the host but not from inside a container.

## Planned Changes

### 1. Add dev-only Dockerfiles

Create separate dev images so production files stay focused on release builds:

- [backend/Dockerfile.dev](backend/Dockerfile.dev)
- [frontend/Dockerfile.dev](frontend/Dockerfile.dev)

Backend dev image will:

- use `mcr.microsoft.com/dotnet/sdk:10.0`
- restore dependencies once during image build
- run `dotnet watch run --project backend.csproj --urls http://+:5001`

Frontend dev image will:

- use `node:22-alpine`
- install dependencies once during image build
- run Angular dev server on `0.0.0.0:4200`
- enable polling so host edits are detected reliably from Docker on Windows

## 2. Add a compose override for dev behavior

Create [docker-compose.dev.yml](docker-compose.dev.yml) that overrides only `api` and `frontend`.

For `api`:

- switch build to `backend/Dockerfile.dev`
- mount source code from `./backend`
- add container-only volumes for build outputs such as `/src/bin` and `/src/obj`
- enable polling with `DOTNET_USE_POLLING_FILE_WATCHER=true`
- keep port `5001:5001`
- relax or extend healthcheck timing so `dotnet watch` startup and EF migration time do not cause false unhealthy states

For `frontend`:

- switch build to `frontend/Dockerfile.dev`
- change port mapping from `4200:80` to `4200:4200`
- mount source code from `./frontend`
- protect container dependencies with a separate `/app/node_modules` volume
- run `ng serve --host 0.0.0.0 --poll=2000`
- keep dependency on `api`

This keeps service names the same, so the base compose remains the production definition and the dev file simply overrides runtime behavior.

## 3. Add a Dodcker-specific Angular proxy

Create [frontend/proxy.conf.docker.json](frontend/proxy.conf.docker.json) that points `/api` to `http://api:5001` instead of `http://localhost:5001`.

Reason:

- `localhost` inside the frontend container refers to the frontend container itself
- the backend is reachable by Compose service name `api`

The existing [frontend/proxy.conf.json](frontend/proxy.conf.json) stays unchanged for host-based local development.

## 4. Keep production flow unchanged

Do not change the behavior of:

- [docker-compose.yml](docker-compose.yml)
- [backend/Dockerfile](backend/Dockerfile)
- [frontend/Dockerfile](frontend/Dockerfile)
- [frontend/nginx.conf](frontend/nginx.conf)

That preserves the current production command:

- `docker compose up -d --build`

## 5. Validation plan

After implementation, verify both paths:

Development path:

- `docker compose -f docker-compose.yml -f docker-compose.dev.yml up`
- edit a backend `.cs` file and confirm `dotnet watch` rebuilds/restarts
- edit a frontend Angular file and confirm `ng serve` recompiles and browser reloads
- verify `/api` requests still work through the Docker proxy

Production path:

- `docker compose up -d --build`
- confirm frontend is still served by Nginx on `localhost:4200`
- confirm API healthcheck and service startup order still work as before

## Implementation Notes

A representative dev override shape will look like this:

```yaml
services:
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile.dev
    volumes:
      - ./backend:/src
      - api_bin:/src/bin
      - api_obj:/src/obj
    environment:
      DOTNET_USE_POLLING_FILE_WATCHER: "true"

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.dev
    ports:
      - "4200:4200"
    volumes:
      - ./frontend:/app
      - frontend_node_modules:/app/node_modules
```

This structure is the safest fit for the current repo because it introduces hot reload without changing the production container layout already defined in the existing files.