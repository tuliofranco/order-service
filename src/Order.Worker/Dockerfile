# 1. Build stage (compila e publica o Worker)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Order.Core/Order.Core.csproj ./Order.Core/
COPY src/Order.Infrastructure/Order.Infrastructure.csproj ./Order.Infrastructure/
COPY src/Order.Worker/Order.Worker.csproj ./Order.Worker/

COPY src/Order.Api/Order.Api.csproj ./Order.Api/

RUN dotnet restore ./Order.Worker/Order.Worker.csproj

COPY src/Order.Core ./Order.Core
COPY src/Order.Infrastructure ./Order.Infrastructure
COPY src/Order.Worker ./Order.Worker
COPY src/Order.Api ./Order.Api

RUN dotnet publish ./Order.Worker/Order.Worker.csproj -c Release -o /app/publish



# 2. Runtime stage (imagem final leve para rodar o Worker)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Order.Worker.dll"]
