#!/bin/bash
set -e

echo "[PurrVet] Waiting for SQL Server to be ready..."

# Wait for SQL Server to accept connections (up to 60 seconds)
MAX_RETRIES=30
RETRY_COUNT=0
while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if timeout 2 bash -c "echo > /dev/tcp/db/1433" 2>/dev/null; then
        echo "[PurrVet] SQL Server is accepting connections."
        break
    fi
    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "[PurrVet] SQL Server not ready yet (attempt $RETRY_COUNT/$MAX_RETRIES)..."
    sleep 2
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "[PurrVet] ERROR: SQL Server did not become ready in time."
    exit 1
fi

# Give SQL Server a few extra seconds to fully initialize
sleep 3

echo "[PurrVet] Starting application..."
exec dotnet PetCloud.dll
