# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SendellVoice.sln", "."]
COPY ["src/SendellVoice.Domain/SendellVoice.Domain.csproj", "src/SendellVoice.Domain/"]
COPY ["src/SendellVoice.Application/SendellVoice.Application.csproj", "src/SendellVoice.Application/"]
COPY ["src/SendellVoice.Infrastructure/SendellVoice.Infrastructure.csproj", "src/SendellVoice.Infrastructure/"]
COPY ["src/SendellVoice.Web/SendellVoice.Web.csproj", "src/SendellVoice.Web/"]

# Restore dependencies
RUN dotnet restore "src/SendellVoice.Web/SendellVoice.Web.csproj"

# Copy source code
COPY src/ src/

# Build the application
WORKDIR "/src/src/SendellVoice.Web"
RUN dotnet build "SendellVoice.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SendellVoice.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create directories
RUN mkdir -p /app/models /app/logs

# Copy published files
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

# Run as non-root user
USER app

ENTRYPOINT ["dotnet", "SendellVoice.Web.dll"]
