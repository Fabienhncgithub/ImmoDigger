FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ImmoDigger.Domain/ImmoDigger.Domain.csproj backend/ImmoDigger.Domain/
COPY backend/ImmoDigger.Application/ImmoDigger.Application.csproj backend/ImmoDigger.Application/
COPY backend/ImmoDigger.Infrastructure/ImmoDigger.Infrastructure.csproj backend/ImmoDigger.Infrastructure/
COPY backend/ImmoDigger.Api/ImmoDigger.Api.csproj backend/ImmoDigger.Api/
RUN dotnet restore backend/ImmoDigger.Api/ImmoDigger.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/ImmoDigger.Api/ImmoDigger.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ImmoDigger.Api.dll"]
