# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["KuizServer/KuizServer.csproj", "KuizServer/"]
RUN dotnet restore "KuizServer/KuizServer.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/KuizServer"
RUN dotnet build "KuizServer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "KuizServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Set environment to Production (PORT will be set by Railway)
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "KuizServer.dll"]

