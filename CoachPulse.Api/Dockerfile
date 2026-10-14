FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore CoachPulse.sln
RUN dotnet publish CoachPulse.Api/CoachPulse.Api.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/App_Data/uploads && chown -R $APP_UID:$APP_UID /app
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
ENTRYPOINT ["dotnet", "CoachPulse.Api.dll"]
