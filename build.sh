#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore
dotnet run --project build -- "$@"
