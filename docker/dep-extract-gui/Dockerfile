# syntax=docker/dockerfile:1
# DepExtract GUI — extract (docker dep-extract) + adapt (local Ollama).
# Build from the Nexo repo root:
#   docker build -f docker/dep-extract-gui/Dockerfile -t nexo-dep-extract-gui .
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy build system + only the projects the GUI graph needs.
COPY global.json Directory.Packages.props Directory.Build.props Directory.Build.targets ./
COPY build ./build
COPY src/Directory.Build.props ./src/Directory.Build.props

COPY src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj src/Nexo.Brick.Contracts/
COPY src/Nexo.Certification.Contracts/Nexo.Certification.Contracts.csproj src/Nexo.Certification.Contracts/
COPY src/Nexo.Certification.Physical/Nexo.Certification.Physical.csproj src/Nexo.Certification.Physical/
COPY src/Nexo.Abstractions/Nexo.Abstractions.csproj src/Nexo.Abstractions/
COPY src/Nexo.Core/Nexo.Core.csproj src/Nexo.Core/
COPY src/Nexo.Core.Domain/Nexo.Core.Domain.csproj src/Nexo.Core.Domain/
COPY src/Nexo.Core.Application/Nexo.Core.Application.csproj src/Nexo.Core.Application/
COPY src/Nexo.Policies/Nexo.Policies.csproj src/Nexo.Policies/
COPY src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj src/Nexo.Transport.Grpc/
COPY src/Nexo.Runtime/Nexo.Runtime.csproj src/Nexo.Runtime/
COPY src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj src/Nexo.Tools.Dev/
COPY src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj src/Nexo.Tools.Assembly/
COPY src/Nexo.Infrastructure/Nexo.Infrastructure.csproj src/Nexo.Infrastructure/
COPY src/Nexo.Bricks.DepExtract/Nexo.Bricks.DepExtract.csproj src/Nexo.Bricks.DepExtract/
COPY src/Nexo.Bricks.DepExtract.Gui/Nexo.Bricks.DepExtract.Gui.csproj src/Nexo.Bricks.DepExtract.Gui/

RUN dotnet restore src/Nexo.Bricks.DepExtract.Gui/Nexo.Bricks.DepExtract.Gui.csproj

COPY src/Nexo.Brick.Contracts ./src/Nexo.Brick.Contracts
COPY src/Nexo.Certification.Contracts ./src/Nexo.Certification.Contracts
COPY src/Nexo.Certification.Physical ./src/Nexo.Certification.Physical
COPY src/Nexo.Abstractions ./src/Nexo.Abstractions
COPY src/Nexo.Core ./src/Nexo.Core
COPY src/Nexo.Core.Domain ./src/Nexo.Core.Domain
COPY src/Nexo.Core.Application ./src/Nexo.Core.Application
COPY src/Nexo.Policies ./src/Nexo.Policies
COPY src/Nexo.Transport.Grpc ./src/Nexo.Transport.Grpc
COPY src/Nexo.Runtime ./src/Nexo.Runtime
COPY src/Nexo.Tools.Dev ./src/Nexo.Tools.Dev
COPY src/Nexo.Tools.Assembly ./src/Nexo.Tools.Assembly
COPY src/Nexo.Infrastructure ./src/Nexo.Infrastructure
COPY src/Nexo.Bricks.DepExtract ./src/Nexo.Bricks.DepExtract
COPY src/Nexo.Bricks.DepExtract.Gui ./src/Nexo.Bricks.DepExtract.Gui

RUN dotnet publish src/Nexo.Bricks.DepExtract.Gui/Nexo.Bricks.DepExtract.Gui.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime: aspnet only + docker CLI (no dockerd). Image talks to host engine via sock.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# docker CLI + Compose v2 plugin (needed for /api/ports/recreate and EVTX_IMAGE switch).
# Prefer vendored binary (scripts/vendor-docker-compose.sh); fall back to GitHub download.
ARG COMPOSE_VERSION=v2.36.2
COPY docker/dep-extract-gui/vendor/ /tmp/compose-vendor/
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl ca-certificates docker.io \
    && mkdir -p /usr/local/lib/docker/cli-plugins \
    && if [ -f /tmp/compose-vendor/docker-compose-linux-x86_64 ]; then \
         cp /tmp/compose-vendor/docker-compose-linux-x86_64 /usr/local/lib/docker/cli-plugins/docker-compose; \
       else \
         curl -fsSL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-linux-x86_64" \
           -o /usr/local/lib/docker/cli-plugins/docker-compose; \
       fi \
    && chmod +x /usr/local/lib/docker/cli-plugins/docker-compose \
    && ln -sf /usr/local/lib/docker/cli-plugins/docker-compose /usr/local/bin/docker-compose \
    && rm -rf /var/lib/apt/lists/* /tmp/compose-vendor \
    && docker compose version

WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://0.0.0.0:5237 \
    OLLAMA_BASE_URL=http://ollama:11434 \
    OLLAMA_MODEL=qwen2.5-coder:7b \
    DOTNET_EnableDiagnostics=0

EXPOSE 5237
HEALTHCHECK --interval=10s --timeout=3s --retries=5 \
    CMD curl -fsS http://127.0.0.1:5237/api/status || exit 1

ENTRYPOINT ["dotnet", "Nexo.Bricks.DepExtract.Gui.dll"]
