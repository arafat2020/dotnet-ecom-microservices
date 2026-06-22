FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5205
EXPOSE 52051

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects to restore dependencies
COPY ["services/Shared.Protos/Shared.Protos.csproj", "services/Shared.Protos/"]
COPY ["services/Image-service/Image-service.csproj", "services/Image-service/"]

RUN dotnet restore "services/Image-service/Image-service.csproj"

# Copy source code and build
COPY ["services/Shared.Protos/", "services/Shared.Protos/"]
COPY ["services/Image-service/", "services/Image-service/"]

WORKDIR "/src/services/Image-service"
RUN dotnet build "Image-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Image-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Image-service.dll"]
