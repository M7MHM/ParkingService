# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Parking-Api/Parking.Api.csproj \
      -c Release \
      -o /app/publish \
      /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
COPY --from=build /app/publish/ ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Parking.Api.dll"]