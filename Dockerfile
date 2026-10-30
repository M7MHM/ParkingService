
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all source and build configuration so restore sees every project setting.
COPY . .

RUN dotnet restore Parking-Api/Parking.Api.csproj

RUN dotnet publish Parking-Api/Parking.Api.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# SQLite creates the database under this writable directory.
RUN mkdir -p /app/data && chown "$APP_UID" /app/data

COPY --from=build /app/publish/ ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Parking.Api.dll"]