# The build output is static files, so it runs on the build machine's own architecture.
FROM --platform=$BUILDPLATFORM node:22-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run lint
RUN npm run build

FROM nginx:1.29-alpine AS runtime
# openssl hashes the passwords given in APP_USERS (see docker/40-access-control.sh).
RUN apk add --no-cache openssl
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY frontend/docker/40-access-control.sh /docker-entrypoint.d/40-access-control.sh
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
  CMD wget --quiet --tries=1 --spider http://127.0.0.1/healthz || exit 1
