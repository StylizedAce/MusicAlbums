FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/MusicAlbums.Core/MusicAlbums.Core.csproj src/MusicAlbums.Core/
COPY src/MusicAlbums.Infrastructure/MusicAlbums.Infrastructure.csproj src/MusicAlbums.Infrastructure/
COPY src/MusicAlbums.Api/MusicAlbums.Api.csproj src/MusicAlbums.Api/
RUN dotnet restore src/MusicAlbums.Api/MusicAlbums.Api.csproj

COPY src/ src/
RUN dotnet publish src/MusicAlbums.Api/MusicAlbums.Api.csproj --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data \
    && chown app:app /data

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__Library="Data Source=/data/musicalbums.db"

COPY --from=build /app/publish .

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "MusicAlbums.Api.dll"]
