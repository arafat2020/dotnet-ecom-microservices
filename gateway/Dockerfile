FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5999

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects to restore dependencies
COPY ["services/Shared.Protos/Shared.Protos.csproj", "services/Shared.Protos/"]
COPY ["gateway/gateway.csproj", "gateway/"]

RUN dotnet restore "gateway/gateway.csproj"

# Copy source code and build
COPY ["services/Shared.Protos/", "services/Shared.Protos/"]
COPY ["gateway/", "gateway/"]

WORKDIR "/src/gateway"
RUN dotnet build "gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "gateway.dll"]
