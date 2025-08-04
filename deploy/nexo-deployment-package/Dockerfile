# Nexo Framework Docker Image
# Multi-stage build for optimized container deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source code
COPY . .

# Restore dependencies
RUN dotnet restore

# Build the CLI application
RUN dotnet publish src/Nexo.CLI/Nexo.CLI.csproj -c Release -f net8.0 -o /app/publish --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install additional dependencies if needed
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    git \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app/publish .

# Create a non-root user
RUN groupadd -r nexo && useradd -r -g nexo nexo
RUN chown -R nexo:nexo /app
USER nexo

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose port for web interface (if needed)
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "Nexo.CLI.dll"]

# Default command
CMD ["--help"] 