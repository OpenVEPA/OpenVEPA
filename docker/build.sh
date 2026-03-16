#!/bin/bash
# =============================================================================
# OpenVEPA Docker Build Script
# =============================================================================
# Builds the OpenVEPA Docker image with proper tagging.
#
# Usage:
#   ./docker/build.sh              # Build with tag openvepa:latest
#   ./docker/build.sh 1.0.0        # Build with tags openvepa:1.0.0 and openvepa:latest
#   ./docker/build.sh 1.0.0 --no-cache  # Build without cache
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
IMAGE_NAME="openvepa"
VERSION="${1:-latest}"
EXTRA_ARGS="${@:2}"

echo "============================================="
echo "  Building OpenVEPA Docker Image"
echo "  Version: ${VERSION}"
echo "============================================="
echo ""

cd "$PROJECT_ROOT"

# Build with version tag
docker build -t "${IMAGE_NAME}:${VERSION}" ${EXTRA_ARGS} .

# Also tag as latest if a version was specified
if [ "$VERSION" != "latest" ]; then
    docker tag "${IMAGE_NAME}:${VERSION}" "${IMAGE_NAME}:latest"
    echo ""
    echo "Tagged: ${IMAGE_NAME}:${VERSION}"
    echo "Tagged: ${IMAGE_NAME}:latest"
else
    echo ""
    echo "Tagged: ${IMAGE_NAME}:latest"
fi

echo ""
echo "Build complete!"
echo ""
echo "Run with:"
echo "  docker compose up -d"
echo "  # or"
echo "  docker run -d -p 8371:8371 --name openvepa ${IMAGE_NAME}:${VERSION}"
