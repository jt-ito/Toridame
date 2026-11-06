# Builds Toridame (server + web client) into one image. The web client lives in a separate repo, so it is
# passed in as a named build context:
#   docker buildx build --build-context web=../jellyfin-web-12.4 -t jteaito/toridame:dev .
# or straight from GitHub (private repo needs a token):
#   docker buildx build --build-context web=https://github.com/jt-ito/Toridame-web.git#main -t jteaito/toridame:dev .

FROM node:24-slim AS webbuild
WORKDIR /src
COPY --from=web package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY --from=web . .
RUN npm run build:production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server
WORKDIR /src
COPY . .
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj -c Release -o /out --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl gnupg libfontconfig1 libfreetype6 libssl3 locales \
 && curl -fsSL https://repo.jellyfin.org/jellyfin_team.gpg.key | gpg --dearmor -o /usr/share/keyrings/jellyfin.gpg \
 && echo "deb [signed-by=/usr/share/keyrings/jellyfin.gpg] https://repo.jellyfin.org/ubuntu noble main" > /etc/apt/sources.list.d/jellyfin.list \
 && apt-get update \
 && apt-get install -y --no-install-recommends jellyfin-ffmpeg7 \
 && apt-get purge -y gnupg && apt-get autoremove -y \
 && rm -rf /var/lib/apt/lists/*

COPY --from=server /out /jellyfin
COPY --from=webbuild /src/dist /jellyfin/jellyfin-web

ENV JELLYFIN_DATA_DIR=/config \
    JELLYFIN_CACHE_DIR=/cache \
    JELLYFIN_WEB_DIR=/jellyfin/jellyfin-web \
    JELLYFIN_FFMPEG=/usr/lib/jellyfin-ffmpeg/ffmpeg \
    DOTNET_gcServer=0

VOLUME /config /cache
EXPOSE 8096
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s CMD curl -fs http://localhost:8096/health || exit 1
ENTRYPOINT ["/jellyfin/jellyfin"]
