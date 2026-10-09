FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5004
EXPOSE 50041

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects to restore dependencies
COPY ["services/Shared.Protos/Shared.Protos.csproj", "services/Shared.Protos/"]
COPY ["services/order-service/order-service.csproj", "services/order-service/"]

RUN dotnet restore "services/order-service/order-service.csproj"

# Copy source code and build
COPY ["services/Shared.Protos/", "services/Shared.Protos/"]
COPY ["services/order-service/", "services/order-service/"]

WORKDIR "/src/services/order-service"
RUN dotnet build "order-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "order-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "order-service.dll"]
