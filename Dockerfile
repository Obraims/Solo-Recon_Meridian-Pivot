# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY backend/StockSync.csproj backend/
RUN dotnet restore backend/StockSync.csproj

# Copy source code and publish
COPY backend/ backend/
COPY frontend/ frontend/
RUN dotnet publish backend/StockSync.csproj -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY frontend/ /frontend/

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "StockSync.dll"]
