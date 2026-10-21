#!/usr/bin/env bash
# One-shot "bring the public demo back up" script for EHS_Project.
#
# The backend (HSEBackend, ASP.NET Core on port 5225) serves the built
# Angular app (EHS_Frontend) out of its own wwwroot, and a free Cloudflare
# Tunnel exposes that single port to the internet temporarily.
#
# Usage:
#   ./restart-demo.sh            # start backend + tunnel with the current build
#   ./restart-demo.sh --rebuild  # rebuild the Angular app first, then start
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT/HSEBackend"
FRONTEND_DIR="$ROOT/EHS_Frontend"
WWWROOT="$BACKEND_DIR/wwwroot"
PORT=5225

CLOUDFLARED="${CLOUDFLARED_PATH:-$HOME/.cloudflared/cloudflared.exe}"
BACKEND_LOG="$(mktemp -t ehs-backend-log.XXXXXX)"
TUNNEL_LOG="$(mktemp -t ehs-tunnel-log.XXXXXX)"

# --- Dependency check -------------------------------------------------------
if ! command -v "$CLOUDFLARED" >/dev/null 2>&1 && [ ! -f "$CLOUDFLARED" ]; then
  echo "cloudflared not found at: $CLOUDFLARED"
  echo "Download it with:"
  echo "  curl -L -o \"$CLOUDFLARED\" https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
  echo "(or set CLOUDFLARED_PATH to point at an existing install)"
  exit 1
fi

# --- Kill stale processes from a previous run -------------------------------
taskkill //F //IM dotnet.exe >/dev/null 2>&1 || true
taskkill //F //IM cloudflared.exe >/dev/null 2>&1 || true

# --- Optional: rebuild the Angular app and copy it into wwwroot ------------
if [[ "${1:-}" == "--rebuild" ]]; then
  echo "Rebuilding Angular app..."
  (cd "$FRONTEND_DIR" && npm run build)

  echo "Copying build output into $WWWROOT ..."
  # Keep uploaded/user content (avatars, uploads/...) in place; only replace
  # the Angular app's own files.
  find "$WWWROOT" -maxdepth 1 -mindepth 1 \
    ! -name "uploads" ! -name "test.txt" -exec rm -rf {} +
  cp -r "$FRONTEND_DIR/dist/ehs-frontend/browser/." "$WWWROOT/"
fi

# --- Start the backend (serves API + Angular app on $PORT) -----------------
echo "Starting backend on port $PORT (log: $BACKEND_LOG)..."
(cd "$BACKEND_DIR" && nohup dotnet run --urls "http://0.0.0.0:$PORT" >"$BACKEND_LOG" 2>&1 &)
sleep 12
echo "--- last lines of backend log ---"
tail -n 20 "$BACKEND_LOG"
echo "----------------------------------"

# --- Start the Cloudflare tunnel --------------------------------------------
echo "Starting cloudflared tunnel (log: $TUNNEL_LOG)..."
nohup "$CLOUDFLARED" tunnel --url "http://localhost:$PORT" >"$TUNNEL_LOG" 2>&1 &
sleep 6

PUBLIC_URL="$(grep -o 'https://[a-zA-Z0-9.-]*\.trycloudflare\.com' "$TUNNEL_LOG" | head -n 1)"

echo ""
echo "============================================"
if [[ -n "$PUBLIC_URL" ]]; then
  echo "Demo URL: $PUBLIC_URL"
else
  echo "Could not find the tunnel URL yet - check the log:"
  echo "  $TUNNEL_LOG"
fi
echo "============================================"
