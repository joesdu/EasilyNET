#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Directory.Packages.props", "src/"]
COPY ["Directory.Build.props", "."]
COPY ["sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj", "sample/WebApi.Test.Unit/"]
COPY ["src/EasilyNET.AutoDependencyInjection/EasilyNET.AutoDependencyInjection.csproj", "src/EasilyNET.AutoDependencyInjection/"]
COPY ["src/EasilyNET.AutoDependencyInjection.Core/EasilyNET.AutoDependencyInjection.Core.csproj", "src/EasilyNET.AutoDependencyInjection.Core/"]
COPY ["src/EasilyNET.Core/EasilyNET.Core.csproj", "src/EasilyNET.Core/"]
COPY ["src/EasilyNET.Mongo.ConsoleDebug/EasilyNET.Mongo.ConsoleDebug.csproj", "src/EasilyNET.Mongo.ConsoleDebug/"]
COPY ["src/EasilyNET.MongoDistributedLock.AspNetCore/EasilyNET.MongoDistributedLock.AspNetCore.csproj", "src/EasilyNET.MongoDistributedLock.AspNetCore/"]
COPY ["src/EasilyNET.MongoDistributedLock/EasilyNET.MongoDistributedLock.csproj", "src/EasilyNET.MongoDistributedLock/"]
COPY ["src/EasilyNET.MongoSerializer.AspNetCore/EasilyNET.MongoSerializer.AspNetCore.csproj", "src/EasilyNET.MongoSerializer.AspNetCore/"]
COPY ["src/EasilyNET.MongoGridFS.AspNetCore/EasilyNET.MongoGridFS.AspNetCore.csproj", "src/EasilyNET.MongoGridFS.AspNetCore/"]
COPY ["src/EasilyNET.Mongo.AspNetCore/EasilyNET.Mongo.AspNetCore.csproj", "src/EasilyNET.Mongo.AspNetCore/"]
COPY ["src/EasilyNET.Mongo.Core/EasilyNET.Mongo.Core.csproj", "src/EasilyNET.Mongo.Core/"]
COPY ["src/EasilyNET.RabbitBus.AspNetCore/EasilyNET.RabbitBus.AspNetCore.csproj", "src/EasilyNET.RabbitBus.AspNetCore/"]
COPY ["src/EasilyNET.RabbitBus.Core/EasilyNET.RabbitBus.Core.csproj", "src/EasilyNET.RabbitBus.Core/"]
COPY ["src/EasilyNET.Security/EasilyNET.Security.csproj", "src/EasilyNET.Security/"]
COPY ["src/EasilyNET.WebCore.Swagger/EasilyNET.WebCore.Swagger.csproj", "src/EasilyNET.WebCore.Swagger/"]
COPY ["src/EasilyNET.WebCore/EasilyNET.WebCore.csproj", "src/EasilyNET.WebCore/"]
RUN dotnet restore "./sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj"
COPY . .
WORKDIR "/src/sample/WebApi.Test.Unit"
RUN dotnet build "./WebApi.Test.Unit.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebApi.Test.Unit.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.Test.Unit.dll"]