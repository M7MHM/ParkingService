#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/opt/parking-app/backups}"
VOLUME="parking-app_parking-data"
STAMP="$(date +%Y%m%d-%H%M%S)"

mkdir -p "$BACKUP_DIR"

if docker run --rm -v "${VOLUME}:/data:ro" alpine:3 sh -c 'test -f /data/ParkingBooking.db'; then
  : 
else
  CHECK_STATUS=$?
  if [ "$CHECK_STATUS" -eq 1 ]; then
    echo "No existing database; skipping pre-deployment backup."
    exit 0
  fi
  echo "Could not inspect the database volume (docker exit ${CHECK_STATUS})." >&2
  exit "$CHECK_STATUS"
fi

docker run --rm \
  -e STAMP="$STAMP" -e OWNER_IDS="$(id -u):$(id -g)" \
  -v "${VOLUME}:/data" -v "${BACKUP_DIR}:/backup" \
  alpine:3 sh -c '
    apk add --no-cache sqlite >/dev/null &&
    sqlite3 /data/ParkingBooking.db ".backup /backup/parking-${STAMP}.db" &&
    gzip /backup/parking-${STAMP}.db &&
    chown "$OWNER_IDS" /backup/parking-${STAMP}.db.gz
  '

find "$BACKUP_DIR" -name 'parking-*.db.gz' -mtime +14 -delete

echo "Backup written: ${BACKUP_DIR}/parking-${STAMP}.db.gz"