FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BuildingBlocks/FluxoCaixa.Contracts/FluxoCaixa.Contracts.csproj", "src/BuildingBlocks/FluxoCaixa.Contracts/"]
COPY ["src/Consolidado/FluxoCaixa.Consolidado.Domain/FluxoCaixa.Consolidado.Domain.csproj", "src/Consolidado/FluxoCaixa.Consolidado.Domain/"]
COPY ["src/Consolidado/FluxoCaixa.Consolidado.Application/FluxoCaixa.Consolidado.Application.csproj", "src/Consolidado/FluxoCaixa.Consolidado.Application/"]
COPY ["src/Consolidado/FluxoCaixa.Consolidado.Infrastructure/FluxoCaixa.Consolidado.Infrastructure.csproj", "src/Consolidado/FluxoCaixa.Consolidado.Infrastructure/"]
COPY ["src/Consolidado/FluxoCaixa.Consolidado.Api/FluxoCaixa.Consolidado.Api.csproj", "src/Consolidado/FluxoCaixa.Consolidado.Api/"]
RUN dotnet restore "src/Consolidado/FluxoCaixa.Consolidado.Api/FluxoCaixa.Consolidado.Api.csproj"

COPY src/BuildingBlocks/FluxoCaixa.Contracts/ src/BuildingBlocks/FluxoCaixa.Contracts/
COPY src/Consolidado/ src/Consolidado/
RUN dotnet publish "src/Consolidado/FluxoCaixa.Consolidado.Api/FluxoCaixa.Consolidado.Api.csproj" -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FluxoCaixa.Consolidado.Api.dll"]
