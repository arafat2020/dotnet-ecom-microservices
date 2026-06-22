FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5002
EXPOSE 50021

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects to restore dependencies
COPY ["services/Shared.Protos/Shared.Protos.csproj", "services/Shared.Protos/"]
COPY ["services/Product-service/Product-service.csproj", "services/Product-service/"]

RUN dotnet restore "services/Product-service/Product-service.csproj"

# Copy source code and build
COPY ["services/Shared.Protos/", "services/Shared.Protos/"]
COPY ["services/Product-service/", "services/Product-service/"]

WORKDIR "/src/services/Product-service"
RUN dotnet build "Product-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Product-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Product-service.dll"]
