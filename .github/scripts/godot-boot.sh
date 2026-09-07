#!/usr/bin/env bash
#
# Boots Godot and fails if it exits non-zero or logs an error.
#
# Godot exits 0 on most content errors - a .tres whose script no longer exists loads as null and the engine carries
# on - so the exit code alone proves almost nothing. Scanning the log is what makes this check worth running.
#
# Usage: godot-boot.sh <label> <command> [args...]

set -uo pipefail

label="$1"
shift

log="godot-${label}.log"

"$@" 2>&1 | tee "${log}"
status="${PIPESTATUS[0]}"

if [ "${status}" -ne 0 ]; then
    echo "::error::Godot ${label} boot exited with status ${status}"
    exit "${status}"
fi

# Known-benign lines, each with a reason. Keep this list short and justified: every entry is a real error message
# being deliberately ignored, and a stale entry hides a regression.
#
#   Path to node is invalid       Editor-only Godot bug against the SubViewport in the 3D sample scenes. The scenes
#                                 are correct; the editor reports it while building its own preview.
ignored='Path to node is invalid'

errors="$(grep -E 'ERROR:|SCRIPT ERROR:|Failed to load' "${log}" | grep -vF "${ignored}")"

if [ -n "${errors}" ]; then
    echo "::error::Godot ${label} boot logged errors"
    printf '%s\n' "${errors}"
    exit 1
fi

echo "Godot ${label} boot clean."
