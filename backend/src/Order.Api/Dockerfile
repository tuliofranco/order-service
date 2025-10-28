
# 1. Build stage (compila e publica a API)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Order.Core/Order.Core.csproj ./Order.Core/
COPY src/Order.Infrastructure/Order.Infrastructure.csproj ./Order.Infrastructure/
COPY src/Order.Api/Order.Api.csproj ./Order.Api/

RUN dotnet restore ./Order.Api/Order.Api.csproj

COPY src/Order.Core ./Order.Core
COPY src/Order.Infrastructure ./Order.Infrastructure
COPY src/Order.Api ./Order.Api

RUN dotnet publish ./Order.Api/Order.Api.csproj -c Release -o /app/publish


# 2. Runtime stage (imagem final leve que vai rodar em produção)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copia só o resultado publicado (binários prontos)
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Order.Api.dll"]
