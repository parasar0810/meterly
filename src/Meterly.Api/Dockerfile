# =============================================================
# Meterly.Api — Multi-Stage Dockerfile
#
# Stage 1 (build):  Uses the full .NET SDK to restore, compile, and publish.
# Stage 2 (runtime): Uses only the lightweight ASP.NET runtime image.
#                    The final image contains NO SDK tooling — smaller and safer.
#
# Build context: the repo root (meterly/)
#   docker build -f src/Meterly.Api/Dockerfile -t meterly-api .
# =============================================================

# ── Stage 1: Build ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy every .csproj first and restore — this layer is cached as long as
# the project files don't change, so subsequent builds are much faster.
COPY ["src/Meterly.Api/Meterly.Api.csproj",                         "Meterly.Api/"]
COPY ["src/Meterly.Application/Meterly.Application.csproj",         "Meterly.Application/"]
COPY ["src/Meterly.Domain/Meterly.Domain.csproj",                   "Meterly.Domain/"]
COPY ["src/Meterly.Infrastructure/Meterly.Infrastructure.csproj",   "Meterly.Infrastructure/"]

RUN dotnet restore "Meterly.Api/Meterly.Api.csproj"

# Now copy the full source and publish in Release mode.
COPY src/ .
RUN dotnet publish "Meterly.Api/Meterly.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security — never run production containers as root.
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

# ASP.NET Core uses port 8080 by default in .NET 8 non-root containers.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "Meterly.Api.dll"]
