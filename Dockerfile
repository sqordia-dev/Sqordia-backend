# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files (only what's needed for WebAPI, excluding test projects)
COPY ["src/Common/Sqordia.Contracts/Sqordia.Contracts.csproj", "src/Common/Sqordia.Contracts/"]
COPY ["src/Core/Sqordia.Domain/Sqordia.Domain.csproj", "src/Core/Sqordia.Domain/"]
COPY ["src/Core/Sqordia.Application/Sqordia.Application.csproj", "src/Core/Sqordia.Application/"]
COPY ["src/Infrastructure/Sqordia.Persistence/Sqordia.Persistence.csproj", "src/Infrastructure/Sqordia.Persistence/"]
COPY ["src/Infrastructure/Sqordia.Infrastructure/Sqordia.Infrastructure.csproj", "src/Infrastructure/Sqordia.Infrastructure/"]
COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]

# Restore dependencies for WebAPI project (will restore all dependencies automatically)
RUN dotnet restore "src/WebAPI/WebAPI.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables for Docker
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port 8080 (standard for containerized apps)
EXPOSE 8080

# Health check removed - curl is not available in .NET runtime image
# ECS Fargate will use application-level health checks via /health endpoint
# HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
#   CMD curl --fail http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "WebAPI.dll"]
