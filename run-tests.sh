#!/usr/bin/env bash
# Wrapper around TestSuiteRunner so you can run the consolidated Unity tests
# from a normal terminal (e.g. the VS Code integrated terminal) with one command.
#
# Usage:
#   ./run-tests.sh              runs every suite (TestSuiteRunner.RunAll)
#   ./run-tests.sh ccas-economy runs one suite (TestSuiteRunner.RunSelected)
#   ./run-tests.sh ccas,progression   runs a comma-separated list of suites

set -uo pipefail

PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION=$(grep 'm_EditorVersion:' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt" | awk '{print $2}')
UNITY_BIN="/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"

if [ ! -x "$UNITY_BIN" ]; then
  echo "Couldn't find Unity $VERSION at $UNITY_BIN"
  echo "Check what's installed with: ls /Applications/Unity/Hub/Editor/"
  echo "then edit the UNITY_BIN line in run-tests.sh to match."
  exit 1
fi

mkdir -p "$PROJECT_PATH/Logs"
LOG="$PROJECT_PATH/Logs/test-run.log"

if [ -n "${1:-}" ]; then
  echo "Running suite(s): $1"
  MGI_TESTS="$1" "$UNITY_BIN" -batchmode -projectPath "$PROJECT_PATH" -executeMethod TestSuiteRunner.RunSelected -quit -logFile "$LOG"
else
  echo "Running all suites"
  "$UNITY_BIN" -batchmode -projectPath "$PROJECT_PATH" -executeMethod TestSuiteRunner.RunAll -quit -logFile "$LOG"
fi
STATUS=$?

echo ""
echo "Unity exited with status $STATUS (0 = pass, 1 = fail)"
echo "Report: $PROJECT_PATH/TestReports/full-test-report.md"
echo "Full log: $LOG"
exit $STATUS
