FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5003
EXPOSE 50031

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects to restore dependencies
COPY ["services/Shared.Protos/Shared.Protos.csproj", "services/Shared.Protos/"]
COPY ["services/payment-service/payment-service.csproj", "services/payment-service/"]

RUN dotnet restore "services/payment-service/payment-service.csproj"

# Copy source code and build
COPY ["services/Shared.Protos/", "services/Shared.Protos/"]
COPY ["services/payment-service/", "services/payment-service/"]

WORKDIR "/src/services/payment-service"
RUN dotnet build "payment-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "payment-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "payment-service.dll"]
