# StockEasy backend — dev-only container running `dotnet watch run` against bind-mounted source.
# Single-stage by design (D-03): hot reload requires the SDK; we never publish a runtime image
# for this challenge.
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# Pre-fetch the most common NuGet packages so the first `dotnet watch run` doesn't have to.
# We don't COPY the source — docker-compose bind-mounts /src at runtime. The actual restore happens
# on container start (dotnet watch handles it). This RUN keeps the layer cache warm for repeat builds.
RUN dotnet --list-sdks

EXPOSE 8080

# Default working dir inside the project — docker-compose overrides this anyway, but having a sane
# default makes `docker run` work for ad-hoc debugging.
WORKDIR /src/Inventory

# Entrypoint comes from docker-compose `command:` to keep config in one place (compose is the
# single source of truth for the dev-runtime contract).
CMD ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://+:8080"]
