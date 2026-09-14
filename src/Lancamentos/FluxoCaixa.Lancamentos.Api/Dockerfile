FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BuildingBlocks/FluxoCaixa.Contracts/FluxoCaixa.Contracts.csproj", "src/BuildingBlocks/FluxoCaixa.Contracts/"]
COPY ["src/Lancamentos/FluxoCaixa.Lancamentos.Domain/FluxoCaixa.Lancamentos.Domain.csproj", "src/Lancamentos/FluxoCaixa.Lancamentos.Domain/"]
COPY ["src/Lancamentos/FluxoCaixa.Lancamentos.Application/FluxoCaixa.Lancamentos.Application.csproj", "src/Lancamentos/FluxoCaixa.Lancamentos.Application/"]
COPY ["src/Lancamentos/FluxoCaixa.Lancamentos.Infrastructure/FluxoCaixa.Lancamentos.Infrastructure.csproj", "src/Lancamentos/FluxoCaixa.Lancamentos.Infrastructure/"]
COPY ["src/Lancamentos/FluxoCaixa.Lancamentos.Api/FluxoCaixa.Lancamentos.Api.csproj", "src/Lancamentos/FluxoCaixa.Lancamentos.Api/"]
RUN dotnet restore "src/Lancamentos/FluxoCaixa.Lancamentos.Api/FluxoCaixa.Lancamentos.Api.csproj"

COPY src/BuildingBlocks/FluxoCaixa.Contracts/ src/BuildingBlocks/FluxoCaixa.Contracts/
COPY src/Lancamentos/ src/Lancamentos/
RUN dotnet publish "src/Lancamentos/FluxoCaixa.Lancamentos.Api/FluxoCaixa.Lancamentos.Api.csproj" -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FluxoCaixa.Lancamentos.Api.dll"]
