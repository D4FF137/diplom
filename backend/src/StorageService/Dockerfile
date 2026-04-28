FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 5006
RUN apk add --no-cache curl icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/StorageService/StorageService.csproj", "src/StorageService/"]
COPY ["shared/Models/Models.csproj", "shared/Models/"]
COPY ["shared/Common/Common.csproj", "shared/Common/"]
COPY ["shared/Contracts/Contracts.csproj", "shared/Contracts/"]
RUN dotnet restore "src/StorageService/StorageService.csproj"
COPY . .
WORKDIR "/src/src/StorageService"
RUN dotnet build "StorageService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StorageService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/uploads
ENTRYPOINT ["dotnet", "StorageService.dll"]
