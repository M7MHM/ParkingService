#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

TAG="${1:-}"
if [[ ! "$TAG" =~ ^sha-[a-f0-9]{7,40}$ ]]; then
  echo "invalid image tag: expected sha- followed by 7-40 lowercase hex characters" >&2
  exit 1
fi

if grep -q '^IMAGE_TAG=' .env; then
  sed -i "s/^IMAGE_TAG=.*/IMAGE_TAG=${TAG}/" .env
else
  echo "IMAGE_TAG=${TAG}" >> .env
fi

compose() { docker compose -f docker-compose.prod.yml "$@"; }

compose pull
./backup.sh
compose up -d --remove-orphans --wait --wait-timeout 180
compose ps
docker image prune -f

echo "Deployed ${TAG}"