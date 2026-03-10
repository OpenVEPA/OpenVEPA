#!/bin/bash
set -e
exec /app/OpenVEPA.Cli start --urls=http://0.0.0.0:${OPENVEPA_PORT:-5000}
