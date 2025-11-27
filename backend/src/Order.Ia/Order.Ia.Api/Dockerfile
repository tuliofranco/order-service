# 1. Build stage (compila e publica a IA API)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia apenas os .csproj (melhora cache de restore)
COPY src/Order.Ia/Order.Ia.Application/Order.Ia.Application.csproj ./Order.Ia.Application/
COPY src/Order.Ia/Order.Ia.Api/Order.Ia.Api.csproj ./Order.Ia.Api/

RUN dotnet restore ./Order.Ia.Api/Order.Ia.Api.csproj

# Copia TODO o código da feature IA
COPY src/Order.Ia/Order.Ia.Application ./Order.Ia.Application
COPY src/Order.Ia/Order.Ia.Api ./Order.Ia.Api

# Publica o projeto da IA API
RUN dotnet publish ./Order.Ia.Api/Order.Ia.Api.csproj -c Release -o /app/publish

# 2. Runtime stage (imagem final leve que vai rodar em produção)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copia só o resultado publicado (binários prontos)
COPY --from=build /app/publish .

# Copia a pasta de prompts para dentro da imagem final
COPY src/Order.Ia/Order.Ia.Api/Prompts ./Prompts

ENTRYPOINT ["dotnet", "Order.Ia.Api.dll"]
