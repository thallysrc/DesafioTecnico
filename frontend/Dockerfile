# StockEasy frontend — dev-only container running `vite` against bind-mounted source.
# node:20-alpine + anonymous volume on /app/node_modules (D-04) so the host's node_modules
# (potentially missing or wrong-arch) never breaks the container.
FROM node:20-alpine

WORKDIR /app

# Copy package manifests first so `npm install` layer caches independently of source changes.
# Plan 03 creates package.json + package-lock.json. If they don't exist yet at build time,
# this COPY fails — that's intentional: the image is built AFTER frontend scaffold lands.
COPY package*.json ./

# Install deps inside the image. The anonymous volume in docker-compose (/app/node_modules)
# then shadows the host's empty node_modules with this image's populated one.
RUN npm install

EXPOSE 5173

# Entrypoint comes from docker-compose `command:` (consistent with backend).
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
