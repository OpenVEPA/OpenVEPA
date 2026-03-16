# Docker Deployment Guide

This guide covers deploying OpenVEPA with Docker, including configuration, security hardening, Ollama integration, and production best practices.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Configuration](#configuration)
3. [Deployment Options](#deployment-options)
4. [Security](#security)
5. [Running with Ollama](#running-with-ollama)
6. [GPU Support](#gpu-support)
7. [Volumes and Persistence](#volumes-and-persistence)
8. [SSH Access](#ssh-access)
9. [Reverse Proxy (NGINX / Traefik)](#reverse-proxy-nginx--traefik)
10. [Backup and Restore](#backup-and-restore)
11. [Troubleshooting](#troubleshooting)

---

## Quick Start

```bash
# Clone the repository
git clone https://github.com/OpenVEPA/OpenVEPA.git
cd OpenVEPA

# Start OpenVEPA (builds the image on first run)
docker compose up -d

# Open the setup wizard
# Navigate to http://localhost:8371
```

On first run with no configuration, the server automatically detects the missing setup and redirects you to the built-in setup wizard at `http://localhost:8371/init/setupwizard`. Complete the wizard to configure your LLM provider and model.

To skip the wizard and auto-configure via environment variables:

```bash
OPENVEPA_PROVIDER=ollama OPENVEPA_MODEL=llama3.2 docker compose --profile with-ollama up -d
```

---

## Configuration

### Environment Variables

| Variable | Default | Description |
|---|---|---|
| `OPENVEPA_PORT` | `8371` | HTTP port for the WebUI and API |
| `SSH_ENABLED` | `false` | Enable the SSH daemon inside the container |
| `SSH_PORT` | `2222` | Host port mapped to container port 22 (SSH) |
| `OPENVEPA_PROVIDER` | *(empty)* | AI provider: `ollama`, `openai`. When set, auto-generates config on first run. |
| `OPENVEPA_MODEL` | *(empty)* | Model identifier (e.g., `llama3.2`, `gpt-4o`) |
| `OPENVEPA_API_KEY` | *(empty)* | API key for the provider (required for OpenAI) |
| `OPENVEPA_OLLAMA_ENDPOINT` | `http://ollama:11434` | Ollama API endpoint. Uses the Docker service name by default. |
| `OLLAMA_PORT` | `11434` | Host port mapped to the Ollama container |

### Using a `.env` File

Docker Compose reads a `.env` file in the project root automatically. Create one to avoid passing variables on the command line:

```bash
# .env
OPENVEPA_PROVIDER=ollama
OPENVEPA_MODEL=llama3.2
OPENVEPA_PORT=8371
SSH_ENABLED=false
```

> **Security:** Add `.env` to your `.gitignore`. Never commit API keys.

### Overriding `appsettings.json`

To supply a custom configuration file, mount it into the data volume:

```bash
docker run -d \
  -p 8371:8371 \
  -v ./my-appsettings.json:/home/openvepa/.openvepa/appsettings.json:ro \
  -v openvepa-data:/home/openvepa/.openvepa \
  --cap-add=NET_ADMIN \
  --name openvepa \
  openvepa
```

The entrypoint checks for `/home/openvepa/.openvepa/appsettings.json` on startup. If it exists, the server starts normally. If not, the server starts and auto-redirects to the built-in setup wizard at `/init/setupwizard`.

### Development Overrides

For local development, copy the included example override file:

```bash
cp docker-compose.override.yml.example docker-compose.override.yml
docker compose --profile with-ollama up -d
```

Docker Compose merges the override file automatically, pre-configuring Ollama as the provider.

---

## Deployment Options

### Option 1: Interactive Deploy Script (Recommended)

The easiest way to get started. The script asks a few questions, generates a `.env` file, and launches everything.

```bash
./docker/deploy.sh

# Or for a one-command deploy with defaults (Ollama, port 8371, no SSH):
./docker/deploy.sh --quick
```

> **Tip:** A documented `.env` template is available at `docker/.env.example`. Copy it to `.env` in the project root and edit as needed.

### Option 2: Docker Compose with Browser Setup

Start the container and configure through the browser. On first run, you'll be automatically redirected to the setup wizard.

```bash
docker compose up -d
# Open http://localhost:8371 — auto-redirects to /init/setupwizard on first run
```

### Option 3: Docker Compose with Environment Variables

Pre-configure the provider to skip the setup wizard entirely.

```bash
# With OpenAI
OPENVEPA_PROVIDER=openai \
OPENVEPA_MODEL=gpt-4o \
OPENVEPA_API_KEY=sk-... \
  docker compose up -d

# With local Ollama
OPENVEPA_PROVIDER=ollama \
OPENVEPA_MODEL=llama3.2 \
  docker compose --profile with-ollama up -d
```

### Option 4: Manual `docker run`

Full control over container options.

```bash
# Build the image (or use the build script: ./docker/build.sh 1.0.0)
docker build -t openvepa:latest .

docker run -d \
  --name openvepa \
  --restart unless-stopped \
  --cap-add=NET_ADMIN \
  -p 8371:8371 \
  -v openvepa-data:/home/openvepa/.openvepa \
  -e OPENVEPA_PROVIDER=ollama \
  -e OPENVEPA_MODEL=llama3.2 \
  -e OPENVEPA_OLLAMA_ENDPOINT=http://host.docker.internal:11434 \
  openvepa:latest
```

> **Note:** When running Ollama on the host (not in Docker), use `http://host.docker.internal:11434` as the endpoint. The default `http://ollama:11434` only works when both containers are on the same Docker network.

### Option 4: Pre-built Image with Docker Compose

If a pre-built image is available on a registry, replace the `build` section in `docker-compose.yml`:

```yaml
services:
  openvepa:
    image: ghcr.io/openvepa/openvepa:latest  # or your registry
    # ... rest of configuration unchanged
```

---

## Security

OpenVEPA's Docker image includes multiple layers of security hardening.

### Non-Root User

The application runs as a dedicated `openvepa` user (not root). The container never runs application code as root.

### UFW Firewall

The image includes UFW (Uncomplicated Firewall) with a deny-all-incoming policy. Only ports 22 (SSH) and 8371 (WebUI/API) are allowed.

UFW requires the `NET_ADMIN` capability:

```yaml
# docker-compose.yml (already configured)
cap_add:
  - NET_ADMIN
```

Without `NET_ADMIN`, the container starts normally but logs a warning that the firewall could not be enabled. The application still works; you lose the in-container firewall layer.

### SSH Hardening

When SSH is enabled:

- Root login is disabled (`PermitRootLogin no`)
- Only the `openvepa` user can connect (`AllowUsers openvepa`)
- Password authentication is enabled by default (set a password or use keys)

### TLS Termination

The container serves plain HTTP on port 8371. For production, terminate TLS at a reverse proxy. See [Reverse Proxy](#reverse-proxy-nginx--traefik) below.

### Secrets Management

- Pass API keys via environment variables, never hardcode them
- Use Docker secrets or a `.env` file excluded from version control
- The `OPENVEPA_API_KEY` variable is written to `appsettings.json` inside the volume on first run

---

## Running with Ollama

The `docker-compose.yml` includes an optional Ollama sidecar activated with the `with-ollama` profile.

### Start with Ollama

```bash
docker compose --profile with-ollama up -d
```

This starts two containers on the `openvepa-net` bridge network:

| Container | Port | Purpose |
|---|---|---|
| `openvepa` | 8371 | OpenVEPA WebUI and API |
| `openvepa-ollama` | 11434 | Ollama LLM inference server |

### Pull a Model

After the containers start, pull a model into Ollama:

```bash
docker exec openvepa-ollama ollama pull llama3.2
```

Other popular models:

```bash
docker exec openvepa-ollama ollama pull mistral
docker exec openvepa-ollama ollama pull codellama
docker exec openvepa-ollama ollama pull phi3
```

### Verify Connectivity

Confirm OpenVEPA can reach Ollama:

```bash
docker exec openvepa curl -s http://ollama:11434/api/tags
```

You should see a JSON response listing available models.

### Using a Host-Installed Ollama

If Ollama is already running on the host (not in Docker), skip the profile and point to the host:

```bash
OPENVEPA_PROVIDER=ollama \
OPENVEPA_MODEL=llama3.2 \
OPENVEPA_OLLAMA_ENDPOINT=http://host.docker.internal:11434 \
  docker compose up -d
```

---

## GPU Support

### NVIDIA GPU

1. Install the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html).

2. Uncomment the GPU section in `docker-compose.yml` under the `ollama` service:

   ```yaml
   ollama:
     image: ollama/ollama:latest
     deploy:
       resources:
         reservations:
           devices:
             - driver: nvidia
               count: all
               capabilities: [gpu]
   ```

3. Start with the Ollama profile:

   ```bash
   docker compose --profile with-ollama up -d
   ```

4. Verify GPU detection:

   ```bash
   docker exec openvepa-ollama nvidia-smi
   ```

### AMD GPU (ROCm)

For AMD GPUs, use the ROCm variant of the Ollama image:

```yaml
ollama:
  image: ollama/ollama:rocm
  devices:
    - /dev/kfd
    - /dev/dri
```

> **Note:** ROCm support depends on your GPU model and driver version. Check [Ollama's documentation](https://ollama.ai/blog/amd-gpu-support) for compatibility.

### CPU-Only (Default)

No additional configuration is needed. Ollama runs on CPU by default, which works but is slower for large models.

---

## Volumes and Persistence

### Data Volume

The `openvepa-data` named volume persists all application state:

```
/home/openvepa/.openvepa/
├── appsettings.json       # Generated configuration
├── data/
│   ├── openvepa.db        # SQLite database (sessions, tokens, preferences)
│   └── documents/         # Uploaded documents for RAG
├── skills/                # Custom skill definitions
├── agents/                # Agent configurations
└── logs/                  # Application logs
```

### Ollama Volume

The `openvepa-ollama-data` volume stores downloaded models:

```
/root/.ollama/
└── models/                # Downloaded model files (several GB each)
```

### Inspecting Volume Contents

```bash
# List files in the OpenVEPA data volume
docker run --rm -v openvepa-data:/data alpine ls -la /data

# View the generated configuration
docker exec openvepa cat /home/openvepa/.openvepa/appsettings.json
```

---

## SSH Access

SSH is disabled by default. Enable it for remote debugging or administration.

### Enable SSH

```bash
SSH_ENABLED=true docker compose up -d
```

### Set a Password

The `openvepa` user has no password by default. Set one after the container starts:

```bash
docker exec -it openvepa passwd openvepa
```

### Connect

```bash
ssh openvepa@localhost -p 2222
```

The host port defaults to 2222 (configurable via `SSH_PORT` in `.env` or `docker-compose.yml`).

### SSH Key Authentication

Mount your public key into the container:

```bash
docker run -d \
  -p 8371:8371 \
  -p 2222:22 \
  -e SSH_ENABLED=true \
  -v ~/.ssh/id_rsa.pub:/home/openvepa/.ssh/authorized_keys:ro \
  -v openvepa-data:/home/openvepa/.openvepa \
  --cap-add=NET_ADMIN \
  --name openvepa \
  openvepa:latest
```

Then set correct permissions inside the container:

```bash
docker exec openvepa bash -c "chmod 700 /home/openvepa/.ssh && chmod 600 /home/openvepa/.ssh/authorized_keys && chown -R openvepa:openvepa /home/openvepa/.ssh"
```

> **Production:** Disable password authentication and use SSH keys only. Edit `/etc/ssh/sshd_config` inside the container or mount a custom config.

---

## Reverse Proxy (NGINX / Traefik)

For production deployments, terminate TLS at a reverse proxy in front of OpenVEPA.

### NGINX

OpenVEPA uses SignalR (WebSockets) for real-time communication. The NGINX configuration must include WebSocket upgrade headers.

```nginx
server {
    listen 443 ssl http2;
    server_name vepa.example.com;

    ssl_certificate     /etc/ssl/certs/vepa.example.com.pem;
    ssl_certificate_key /etc/ssl/private/vepa.example.com.key;

    location / {
        proxy_pass http://127.0.0.1:8371;
        proxy_http_version 1.1;

        # WebSocket support (required for SignalR)
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Disable buffering for streaming responses
        proxy_buffering off;

        # Long timeout for WebSocket connections
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name vepa.example.com;
    return 301 https://$host$request_uri;
}
```

### Traefik

Add labels to the `openvepa` service in `docker-compose.yml` or an override file:

```yaml
services:
  openvepa:
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.openvepa.rule=Host(`vepa.example.com`)"
      - "traefik.http.routers.openvepa.entrypoints=websecure"
      - "traefik.http.routers.openvepa.tls.certresolver=letsencrypt"
      - "traefik.http.services.openvepa.loadbalancer.server.port=8371"
```

Traefik handles WebSocket upgrades automatically when routing to HTTP backends.

### Cloudflare Tunnel

For zero-port-exposure deployments:

```bash
cloudflared tunnel --url http://localhost:8371
```

> **Important:** Ensure your reverse proxy or tunnel supports WebSocket connections. SignalR falls back to long-polling if WebSockets are unavailable, but performance degrades.

---

## Backup and Restore

### Backup the Database

```bash
# Copy the SQLite database from the volume
docker cp openvepa:/home/openvepa/.openvepa/data/openvepa.db ./backup-openvepa.db

# Or backup the entire data directory
docker cp openvepa:/home/openvepa/.openvepa ./backup-openvepa-data
```

### Volume Snapshot

```bash
# Create a tarball of the named volume
docker run --rm \
  -v openvepa-data:/data:ro \
  -v $(pwd):/backup \
  alpine tar czf /backup/openvepa-backup-$(date +%Y%m%d).tar.gz -C /data .
```

### Restore from Backup

```bash
# Stop the container first
docker compose down

# Restore the volume from tarball
docker run --rm \
  -v openvepa-data:/data \
  -v $(pwd):/backup \
  alpine sh -c "rm -rf /data/* && tar xzf /backup/openvepa-backup-20250101.tar.gz -C /data"

# Restart
docker compose up -d
```

### Backup Ollama Models

```bash
docker run --rm \
  -v openvepa-ollama-data:/data:ro \
  -v $(pwd):/backup \
  alpine tar czf /backup/ollama-models-$(date +%Y%m%d).tar.gz -C /data .
```

> **Tip:** Ollama models are large (2-8 GB each). It is often faster to re-pull models than to back them up.

---

## Troubleshooting

### Container won't start

**Check logs:**

```bash
docker compose logs openvepa
```

**Common causes:**

| Symptom | Cause | Fix |
|---|---|---|
| Port 8371 already in use | Another process on port 8371 | Change `OPENVEPA_PORT` or stop the other process |
| Permission denied on volume | Volume owned by different user | `docker exec openvepa chown -R openvepa:openvepa /home/openvepa/.openvepa` |

### Health check failing

```bash
# Check health status
docker inspect --format='{{.State.Health.Status}}' openvepa

# View health check logs
docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' openvepa

# Test the health endpoint manually
docker exec openvepa curl -sf http://localhost:8371/health
```

The health check probes `http://localhost:${OPENVEPA_PORT}/health` every 30 seconds with a 15-second start-up grace period.

### UFW warnings on startup

```
[OpenVEPA] WARNING: Could not enable UFW.
[OpenVEPA] Run with --cap-add=NET_ADMIN to enable firewall.
```

This is expected if you run without `--cap-add=NET_ADMIN`. The application works normally; the in-container firewall is skipped. If you need the firewall, add the capability:

```yaml
cap_add:
  - NET_ADMIN
```

### Cannot connect to Ollama

```bash
# Verify Ollama is running
docker compose --profile with-ollama ps

# Test connectivity from the OpenVEPA container
docker exec openvepa curl -s http://ollama:11434/api/tags

# Check if a model is pulled
docker exec openvepa-ollama ollama list
```

**Common causes:**

| Symptom | Cause | Fix |
|---|---|---|
| Connection refused | Ollama not started | `docker compose --profile with-ollama up -d` |
| No models listed | Model not pulled | `docker exec openvepa-ollama ollama pull llama3.2` |
| DNS resolution failed | Containers on different networks | Both must be on `openvepa-net` |

### Setup wizard not appearing

If you don't see the setup wizard on first run, check that the container is running and healthy:

```bash
# Check container status
docker compose ps

# Check if the config already exists (wizard only appears when no config is present)
docker exec openvepa cat /home/openvepa/.openvepa/appsettings.json

# To start fresh, remove the data volume
docker compose down
docker volume rm openvepa-data
docker compose up -d

# Or manually generate config via env vars
docker compose down
OPENVEPA_PROVIDER=ollama OPENVEPA_MODEL=llama3.2 docker compose up -d
```

### Viewing Application Logs

```bash
# Follow container logs
docker compose logs -f openvepa

# View logs inside the data volume
docker exec openvepa ls /home/openvepa/.openvepa/logs/
docker exec openvepa cat /home/openvepa/.openvepa/logs/openvepa.log
```

### Resetting to a Clean State

```bash
docker compose down
docker volume rm openvepa-data
docker compose up -d
# Open http://localhost:8371 — the server redirects to the setup wizard
```

---

## Architecture Reference

### Container Internals

```
┌─────────────────────────────────────────────┐
│  openvepa container                         │
│                                             │
│  tini (PID 1)                               │
│    └── entrypoint.sh                        │
│          ├── first_run_setup()              │
│          │     └── generate_config_from_env  │
│          │          (env-var auto-config)     │
│          ├── configure_firewall() (UFW)      │
│          ├── start_ssh() (if enabled)        │
│          └── start_app()                     │
│                └── OpenVEPA.Cli start        │
│                      ├── Kestrel :8371       │
│                      ├── SignalR /hub/*      │
│                      └── Hangfire scheduler  │
│                                             │
│  Ports: 8371 (HTTP), 22 (SSH)              │
│  Volume: /home/openvepa/.openvepa           │
└─────────────────────────────────────────────┘
```

### Docker Compose Network

```
┌──────────────────── openvepa-net (bridge) ────────────────────┐
│                                                                │
│  ┌─────────────┐          ┌──────────────────┐                │
│  │  openvepa    │  http:// │  openvepa-ollama  │                │
│  │  :8371       │──────────│  :11434           │                │
│  └─────────────┘  ollama:  └──────────────────┘                │
│                   11434     (profile: with-ollama)              │
└────────────────────────────────────────────────────────────────┘
     Host :8371                  Host :11434
```

### File Reference

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage build: SDK build, ASP.NET runtime, security hardening |
| `docker-compose.yml` | Service definitions for OpenVEPA and optional Ollama sidecar |
| `docker-compose.override.yml.example` | Development overrides template |
| `docker/entrypoint.sh` | First-run config, UFW, SSH, graceful shutdown |

---

## License

MIT. See [LICENSE](../LICENSE) for details.
