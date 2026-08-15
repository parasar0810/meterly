# =============================================================
# Meterly.Client — Multi-Stage Dockerfile (Blazor WebAssembly + Nginx)
#
# Stage 1 (build):  Uses the .NET SDK to publish the Blazor WASM app.
#                   Output is a folder of static files (HTML, JS, WASM, CSS).
# Stage 2 (runtime): Uses a tiny Nginx Alpine image to serve those static files.
#                    No .NET runtime needed — WASM runs in the browser!
#
# Build context: the repo root (meterly/)
#   docker build -f src/Meterly.Client/Dockerfile -t meterly-client .
# =============================================================

# ── Stage 1: Build ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore only the client project — it has no server-side project references.
COPY ["src/Meterly.Client/Meterly.Client.csproj", "Meterly.Client/"]
RUN dotnet restore "Meterly.Client/Meterly.Client.csproj"

COPY src/Meterly.Client/ Meterly.Client/

RUN dotnet publish "Meterly.Client/Meterly.Client.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime (Nginx) ────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Remove the default Nginx site config.
RUN rm /etc/nginx/conf.d/default.conf

# Copy our custom Nginx config (handles SPA routing + WASM MIME types).
COPY src/Meterly.Client/nginx.conf /etc/nginx/nginx.conf

# Copy the published Blazor WASM static files.
# Blazor publishes to wwwroot inside the publish output folder.
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
