
# ==========================================
# Build stage - SDK image for building
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

# Set working directory
WORKDIR /src

# Copy solution file first for better layer caching
COPY ["TestAwing.sln", "./"]

# Copy project files - this layer will be cached unless project files change
COPY ["TestAwing/TestAwing.csproj", "TestAwing/"]
COPY ["TestAwing.Tests/TestAwing.Tests.csproj", "TestAwing.Tests/"]

# Restore dependencies - this layer will be cached unless project files change
RUN dotnet restore "TestAwing/TestAwing.csproj" --verbosity minimal

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/TestAwing"
RUN dotnet build "./TestAwing.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore \
    --verbosity minimal

# ==========================================
# Test stage (optional) - uncomment if you want to run tests during build
# ==========================================
# FROM build AS test
# WORKDIR /src
# RUN dotnet test "TestAwing.Tests/TestAwing.Tests.csproj" \
#     --no-build \
#     --verbosity normal \
#     --configuration $BUILD_CONFIGURATION

# ==========================================
# Publish stage - create the published output
# ==========================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/TestAwing"
RUN dotnet publish "./TestAwing.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --verbosity minimal \
    /p:UseAppHost=false

# ==========================================
# Runtime stage - final slim image
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install curl for health checks and tzdata for timezone support
RUN apt-get update && apt-get install -y \
    curl \
    tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean

# Create application directory
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Change ownership of the app directory to appuser
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Define entry point
ENTRYPOINT ["dotnet", "TestAwing.dll"]
