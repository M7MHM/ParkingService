# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .

RUN --mount=type=cache,id=parking-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore Parking-Api/Parking.Api.csproj \
    && dotnet publish Parking-Api/Parking.Api.csproj \
        --configuration $BUILD_CONFIGURATION \
        --output /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p /app/data && chown "$APP_UID" /app/data
COPY --from=build /app/publish/ ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Parking.Api.dll"]