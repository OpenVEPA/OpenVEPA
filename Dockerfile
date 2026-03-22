# =============================================================================
# OpenVEPA Multi-Stage Dockerfile
# =============================================================================
# Builds the OpenVEPA .NET 10 application with security hardening.
#
# Build:
#   docker build -t openvepa:latest .
#
# Run (minimal):
#   docker run -d -p 8371:8371 --name openvepa openvepa:latest
#
# Run (with SSH and UFW, requires NET_ADMIN for iptables):
#   docker run -d -p 8371:8371 -p 2222:22 \
#     --cap-add=NET_ADMIN \
#     -e SSH_ENABLED=true \
#     -v openvepa-data:/home/openvepa/.openvepa \
#     --name openvepa openvepa:latest
#
# Environment variables:
#   OPENVEPA_PORT             - Server port (default: 8371)
#   SSH_ENABLED               - Enable SSH server (default: false)
#   OPENVEPA_PROVIDER         - AI provider name (empty = setup wizard)
#   OPENVEPA_MODEL            - Model identifier
#   OPENVEPA_API_KEY          - Provider API key
#   OPENVEPA_OLLAMA_ENDPOINT  - Ollama endpoint for local models
# =============================================================================

# ---------------------------------------------------------------------------
# Stage 1: Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and build props first (rarely change, good cache layer)
COPY OpenVEPA.slnx Directory.Build.props ./

# Copy each project file individually for optimal restore caching.
# Changing source code does not bust the restore cache as long as
# the .csproj files remain unchanged.
COPY src/OpenVEPA.Core/OpenVEPA.Core.csproj src/OpenVEPA.Core/
COPY src/OpenVEPA.Storage/OpenVEPA.Storage.csproj src/OpenVEPA.Storage/
COPY src/OpenVEPA.Providers/OpenVEPA.Providers.csproj src/OpenVEPA.Providers/
COPY src/OpenVEPA.Skills.Runtime/OpenVEPA.Skills.Runtime.csproj src/OpenVEPA.Skills.Runtime/
COPY src/OpenVEPA.Agents.Runtime/OpenVEPA.Agents.Runtime.csproj src/OpenVEPA.Agents.Runtime/
COPY src/OpenVEPA.Server/OpenVEPA.Server.csproj src/OpenVEPA.Server/
COPY src/OpenVEPA.Scheduler/OpenVEPA.Scheduler.csproj src/OpenVEPA.Scheduler/
COPY src/OpenVEPA.Cli/OpenVEPA.Cli.csproj src/OpenVEPA.Cli/
COPY src/OpenVEPA.Channels.Telegram/OpenVEPA.Channels.Telegram.csproj src/OpenVEPA.Channels.Telegram/
COPY src/OpenVEPA.Channels.Discord/OpenVEPA.Channels.Discord.csproj src/OpenVEPA.Channels.Discord/
COPY src/OpenVEPA.Channels.Email/OpenVEPA.Channels.Email.csproj src/OpenVEPA.Channels.Email/
COPY src/OpenVEPA.Hub.Client/OpenVEPA.Hub.Client.csproj src/OpenVEPA.Hub.Client/

# Restore NuGet packages (cached unless .csproj files change).
# Restore the CLI project (not the full solution) because test projects
# are excluded from the Docker build context via .dockerignore.
RUN dotnet restore src/OpenVEPA.Cli/OpenVEPA.Cli.csproj

# Copy all source code
COPY src/ src/

# Publish framework-dependent build (uses aspnet base image runtime).
# No --self-contained flag: the runtime stage provides the .NET runtime.
RUN dotnet publish src/OpenVEPA.Cli/OpenVEPA.Cli.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------------------------------------------------------------------------
# Stage 2: Runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Security: install minimal required packages.
# - tini: PID 1 init process for proper signal handling (SIGTERM, SIGINT).
# - curl: required for HEALTHCHECK endpoint probing.
# - openssh-server: optional SSH access (controlled by SSH_ENABLED env var).
# - ufw: firewall rules (requires --cap-add=NET_ADMIN at runtime since
#   UFW uses iptables which needs kernel-level network capabilities.
#   UFW is enabled in entrypoint.sh, not here, because iptables
#   requires runtime permissions that are unavailable during build).
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        tini \
        ufw \
        openssh-server && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir -p /run/sshd

# Security: create dedicated non-root user.
# The application runs as this user, reducing blast radius if compromised.
RUN groupadd -r openvepa && \
    useradd -r -g openvepa -m -s /bin/bash openvepa

# Security: configure UFW defaults.
# NOTE: UFW enable requires --cap-add=NET_ADMIN at docker run.
# These rules are pre-configured; entrypoint.sh calls "ufw --force enable"
# at container start when the capability is available.
RUN ufw default deny incoming && \
    ufw default allow outgoing && \
    ufw allow 22/tcp comment 'SSH' && \
    ufw allow 8371/tcp comment 'OpenVEPA WebUI/API'

# Security: harden SSH configuration.
# - Disable root login to prevent privilege escalation.
# - Restrict access to the openvepa user only.
# - SSH is off by default; enable with SSH_ENABLED=true.
RUN sed -i 's/#PermitRootLogin.*/PermitRootLogin no/' /etc/ssh/sshd_config && \
    sed -i 's/#PasswordAuthentication.*/PasswordAuthentication yes/' /etc/ssh/sshd_config && \
    echo "AllowUsers openvepa" >> /etc/ssh/sshd_config

# Copy published application from build stage
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R openvepa:openvepa /app

# Copy entrypoint script
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# ---------------------------------------------------------------------------
# Environment
# ---------------------------------------------------------------------------
ENV OPENVEPA_HOME=/home/openvepa/.openvepa \
    DOTNET_RUNNING_IN_CONTAINER=true \
    OPENVEPA_PORT=8371 \
    OPENVEPA_LOG_LEVEL=Information \
    SSH_ENABLED=false

# Provider configuration (empty = use defaults or setup wizard)
ENV OPENVEPA_PROVIDER="" \
    OPENVEPA_MODEL="" \
    OPENVEPA_API_KEY="" \
    OPENVEPA_OLLAMA_ENDPOINT="http://ollama:11434"

# Create data directories with correct ownership.
# These paths match the conventions used by the application:
#   data/          - SQLite database
#   data/documents - uploaded documents for RAG
#   skills/        - custom skill definitions
#   agents/        - agent configurations
#   logs/          - application logs
RUN mkdir -p /home/openvepa/.openvepa/data/documents \
             /home/openvepa/.openvepa/skills \
             /home/openvepa/.openvepa/agents \
             /home/openvepa/.openvepa/logs && \
    chown -R openvepa:openvepa /home/openvepa

# Persist OpenVEPA home directory across container restarts.
# Mount a named volume here to retain database, documents, and config.
VOLUME ["/home/openvepa/.openvepa"]

# Health check probes the /health endpoint registered by the server.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -sf http://localhost:${OPENVEPA_PORT}/health || exit 1

# Expose application port and SSH port
EXPOSE 8371 22

# Use tini as PID 1 for proper signal forwarding and zombie reaping.
# Without tini, the .NET process runs as PID 1 and may not handle
# SIGTERM correctly, causing slow container stops (10s Docker timeout).
ENTRYPOINT ["tini", "--", "/entrypoint.sh"]
