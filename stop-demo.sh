#!/usr/bin/env bash
# Stops the backend and the Cloudflare tunnel started by restart-demo.sh.
set -uo pipefail

echo "Stopping backend (dotnet.exe)..."
taskkill //F //IM dotnet.exe >/dev/null 2>&1 && echo "  stopped." || echo "  not running."

echo "Stopping tunnel (cloudflared.exe)..."
taskkill //F //IM cloudflared.exe >/dev/null 2>&1 && echo "  stopped." || echo "  not running."
