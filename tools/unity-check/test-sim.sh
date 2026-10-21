#!/usr/bin/env bash
# Headless C# gameplay-logic checks (no Unity runtime needed).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT/scripts/unity-sim-tests"
dotnet run --project tests.csproj
