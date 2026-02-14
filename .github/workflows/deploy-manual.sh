#!/bin/bash
# Manual deployment script for API
# Usage: ./deploy-manual.sh

set -e  # Exit on error

echo "🚀 Starting manual API deployment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Navigate to API directory
cd /srv/docker/api/AskAMuslimBackend

echo -e "${YELLOW}📥 Pulling latest code from production branch...${NC}"
git fetch origin
git reset --hard origin/production

# Navigate to docker directory
cd /srv/docker

echo -e "${YELLOW}🔨 Rebuilding API Docker image...${NC}"
docker compose build api

echo -e "${YELLOW}♻️ Restarting API container...${NC}"
docker compose up -d api

# Wait for health check
echo -e "${YELLOW}⏳ Waiting for API to become healthy...${NC}"
for i in {1..30}; do
  if docker compose ps api | grep -q "healthy"; then
    echo -e "${GREEN}✅ API is healthy and running!${NC}"
    break
  fi
  echo "Waiting... ($i/30)"
  sleep 2
done

# Show final status
echo -e "${YELLOW}📊 Deployment Status:${NC}"
docker compose ps api

# Check if healthy
if docker compose ps api | grep -q "healthy"; then
  echo -e "${GREEN}✨ Deployment completed successfully!${NC}"
  exit 0
else
  echo -e "${RED}❌ Deployment may have issues. Check logs:${NC}"
  echo "docker compose logs api --tail 100"
  exit 1
fi
