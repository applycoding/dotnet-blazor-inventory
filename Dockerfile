FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json nuget.config ./
COPY src/Stockroom.Domain/Stockroom.Domain.csproj src/Stockroom.Domain/
COPY src/Stockroom.Contracts/Stockroom.Contracts.csproj src/Stockroom.Contracts/
COPY src/Stockroom.Application/Stockroom.Application.csproj src/Stockroom.Application/
COPY src/Stockroom.Infrastructure/Stockroom.Infrastructure.csproj src/Stockroom.Infrastructure/
COPY src/Stockroom.Web/Stockroom.Web.csproj src/Stockroom.Web/
COPY src/Stockroom.Api/Stockroom.Api.csproj src/Stockroom.Api/
RUN dotnet restore src/Stockroom.Api/Stockroom.Api.csproj

COPY src/ src/
RUN dotnet publish src/Stockroom.Api/Stockroom.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

USER root
RUN mkdir -p /app/data && chown -R "$APP_UID:$APP_UID" /app/data
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DatabaseProvider=Sqlite \
    ConnectionStrings__Sqlite="Data Source=/app/data/stockroom.db"

EXPOSE 8080
ENTRYPOINT ["dotnet", "Stockroom.Api.dll"]
