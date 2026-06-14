#!/usr/bin/env bash
# Convenience wrapper so the common loops are one command.
#   ./scripts/dev.sh test     # build + run all tests
#   ./scripts/dev.sh build     # build the whole solution
#   ./scripts/dev.sh server    # run the shared-world server (http://localhost:5173)
#   ./scripts/dev.sh demo      # run the single-NPC console demo
set -euo pipefail
cd "$(dirname "$0")/.."

cmd="${1:-test}"
case "$cmd" in
  build)  dotnet build VoxelAgentNexus.slnx ;;
  test)   dotnet test  VoxelAgentNexus.slnx ;;
  server) dotnet run --project src/VoxelAgentNexus.Server ;;
  demo)   dotnet run --project src/VoxelAgentNexus.App ;;
  *) echo "usage: $0 [build|test|server|demo]"; exit 1 ;;
esac
