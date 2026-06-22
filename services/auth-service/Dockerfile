FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 50011

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects to restore dependencies
COPY ["services/Shared.Protos/Shared.Protos.csproj", "services/Shared.Protos/"]
COPY ["services/auth-service/auth-service.csproj", "services/auth-service/"]

RUN dotnet restore "services/auth-service/auth-service.csproj"

# Copy source code and build
COPY ["services/Shared.Protos/", "services/Shared.Protos/"]
COPY ["services/auth-service/", "services/auth-service/"]

WORKDIR "/src/services/auth-service"
RUN dotnet build "auth-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "auth-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "auth-service.dll"]
