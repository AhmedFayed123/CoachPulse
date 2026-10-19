FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
ENV NUGET_PACKAGES=/tmp/nuget-packages NUGET_HTTP_CACHE_PATH=/tmp/nuget-http-cache
COPY . .
RUN dotnet restore CoachPulse.Api/CoachPulse.Api.csproj
RUN dotnet publish CoachPulse.Api/CoachPulse.Api.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/App_Data/uploads /app/App_Data/data-protection /home/app \
    && chown -R $APP_UID:$APP_UID /app /home/app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
ENV HOME=/home/app
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=45s --retries=10 CMD curl --fail --silent http://127.0.0.1:8080/healthz || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "CoachPulse.Api.dll"]
