#!/bin/bash
# =============================================================================
# OpenVEPA Docker Entrypoint
# =============================================================================
# Handles first-run setup, firewall configuration, optional SSH, and
# application startup with graceful shutdown support.
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
OPENVEPA_HOME="${OPENVEPA_HOME:-/home/openvepa/.openvepa}"
PORT="${OPENVEPA_PORT:-8371}"
CONFIG_FILE="${OPENVEPA_HOME}/openvepa.conf"
APP_PATH="/app/OpenVEPA.Cli"

# ---------------------------------------------------------------------------
# Signal handling for graceful shutdown
# ---------------------------------------------------------------------------
shutdown() {
    echo "[OpenVEPA] Shutting down..."
    # Kill the app process if running
    if [ -n "${APP_PID:-}" ]; then
        kill -TERM "$APP_PID" 2>/dev/null || true
        wait "$APP_PID" 2>/dev/null || true
    fi
    # Stop SSH if running
    if [ "${SSH_ENABLED:-false}" = "true" ]; then
        service ssh stop 2>/dev/null || true
    fi
    echo "[OpenVEPA] Shutdown complete."
    exit 0
}
trap shutdown SIGTERM SIGINT

# ---------------------------------------------------------------------------
# 1. First-run configuration
# ---------------------------------------------------------------------------
first_run_setup() {
    if [ -f "$CONFIG_FILE" ]; then
        echo "[OpenVEPA] Configuration found at ${CONFIG_FILE}"
        return 0
    fi

    echo "[OpenVEPA] First run detected — no configuration found."

    # If provider is set via env vars, auto-generate config
    if [ -n "${OPENVEPA_PROVIDER:-}" ]; then
        echo "[OpenVEPA] Generating configuration from environment variables..."
        generate_config_from_env
        return 0
    fi

    # No config and no env vars — the server will serve the setup wizard
    echo "[OpenVEPA] No configuration found. Setup wizard will be available at http://localhost:${PORT}/init/setupwizard"
}

# ---------------------------------------------------------------------------
# Auto-generate appsettings.json from environment variables
# ---------------------------------------------------------------------------
generate_config_from_env() {
    local provider="${OPENVEPA_PROVIDER:-ollama}"
    local model="${OPENVEPA_MODEL:-llama3.2}"
    local api_key="${OPENVEPA_API_KEY:-}"
    local ollama_endpoint="${OPENVEPA_OLLAMA_ENDPOINT:-http://ollama:11434}"
    local home_path="${OPENVEPA_HOME}"

    mkdir -p "${home_path}/data/documents" \
             "${home_path}/skills" \
             "${home_path}/agents" \
             "${home_path}/logs"

    # Build provider-specific JSON
    local provider_json
    if [ "$provider" = "openai" ]; then
        provider_json=$(cat <<PJSON
    "DefaultProvider": "openai",
    "OpenAi": {
      "ApiKey": "${api_key}",
      "ModelId": "${model}",
      "Endpoint": "https://api.openai.com/v1"
    }
PJSON
)
    else
        provider_json=$(cat <<PJSON
    "DefaultProvider": "ollama",
    "Ollama": {
      "Endpoint": "${ollama_endpoint}",
      "ModelId": "${model}"
    }
PJSON
)
    fi

    cat > "$CONFIG_FILE" <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Providers": {
${provider_json}
  },
  "Skills": {
    "SkillsDirectory": "${home_path}/skills"
  },
  "OpenVEPA": {
    "Agents": {
      "AgentsDirectory": "${home_path}/agents"
    }
  },
  "Scheduler": {
    "Enabled": true,
    "DashboardEnabled": false,
    "Schedules": []
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}"
      }
    }
  }
}
EOF

    echo "[OpenVEPA] Configuration generated at ${CONFIG_FILE}"
    echo "[OpenVEPA]   Provider: ${provider}"
    echo "[OpenVEPA]   Model: ${model}"
    echo "[OpenVEPA]   Port: ${PORT}"

    # Create initial API token for WebUI access
    echo "[OpenVEPA] Creating initial API token..."
    TOKEN_OUTPUT=$("$APP_PATH" token create "docker-auto" 2>&1 || true)
    echo "$TOKEN_OUTPUT"
    echo "[OpenVEPA] Token created. Use 'docker exec openvepa openvepa token list' to view tokens."
}

# ---------------------------------------------------------------------------
# 2. Configure and enable UFW firewall
# ---------------------------------------------------------------------------
configure_firewall() {
    # UFW requires NET_ADMIN capability. If not available, skip gracefully.
    if ! command -v ufw &>/dev/null; then
        echo "[OpenVEPA] UFW not available, skipping firewall configuration."
        return 0
    fi

    echo "[OpenVEPA] Configuring firewall..."

    # Update the allowed port if non-default
    if [ "$PORT" != "8371" ]; then
        ufw allow "${PORT}/tcp" comment 'OpenVEPA WebUI/API' 2>/dev/null || true
    fi

    # Enable UFW (requires --cap-add=NET_ADMIN)
    if ufw --force enable 2>/dev/null; then
        echo "[OpenVEPA] Firewall enabled (allowing ports 22, ${PORT})"
    else
        echo "[OpenVEPA] WARNING: Could not enable UFW."
        echo "[OpenVEPA] Run with --cap-add=NET_ADMIN to enable firewall."
    fi
}

# ---------------------------------------------------------------------------
# 3. Start SSH daemon (optional)
# ---------------------------------------------------------------------------
start_ssh() {
    if [ "${SSH_ENABLED:-false}" != "true" ]; then
        return 0
    fi

    if ! command -v sshd &>/dev/null; then
        echo "[OpenVEPA] WARNING: SSH requested but sshd not found."
        return 0
    fi

    echo "[OpenVEPA] Starting SSH daemon..."

    # Generate host keys if missing (first run)
    ssh-keygen -A 2>/dev/null || true

    /usr/sbin/sshd
    echo "[OpenVEPA] SSH daemon started on port 22."
    echo "[OpenVEPA] Set password: docker exec -it openvepa passwd openvepa"
}

# ---------------------------------------------------------------------------
# 4. Start the application
# ---------------------------------------------------------------------------
start_app() {
    echo "[OpenVEPA] Starting OpenVEPA on port ${PORT}..."
    echo ""

    # Run in background so we can handle signals
    "$APP_PATH" start --urls="http://0.0.0.0:${PORT}" &
    APP_PID=$!

    # Wait for the process (will be interrupted by signal trap)
    wait "$APP_PID"
    local exit_code=$?

    echo "[OpenVEPA] Application exited with code ${exit_code}."
    exit "$exit_code"
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
echo "============================================="
echo "  OpenVEPA Personal AI Assistant"
echo "  Port: ${PORT}"
echo "  Home: ${OPENVEPA_HOME}"
echo "  SSH:  ${SSH_ENABLED:-false}"
echo "============================================="
echo ""

first_run_setup
configure_firewall
start_ssh
start_app
