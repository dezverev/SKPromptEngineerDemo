#!/bin/bash
# Run IntegrationTesterApp tests
# Usage: ./run-tests.sh [--verbose|-v]

VERBOSE=""
if [[ "$1" == "--verbose" || "$1" == "-v" ]]; then
    VERBOSE="--verbose"
fi

echo "Running IntegrationTesterApp tests..."
cd src/IntegrationTesterApp
dotnet run -- $VERBOSE
exit $?
