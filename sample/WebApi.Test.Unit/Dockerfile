# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Directory.Packages.props", "src/"]
COPY ["Directory.Build.props", "."]
COPY ["sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj", "sample/WebApi.Test.Unit/"]
COPY ["src/EasilyNET.AutoDependencyInjection/EasilyNET.AutoDependencyInjection.csproj", "src/EasilyNET.AutoDependencyInjection/"]
COPY ["src/EasilyNET.AutoDependencyInjection.Core/EasilyNET.AutoDependencyInjection.Core.csproj", "src/EasilyNET.AutoDependencyInjection.Core/"]
COPY ["src/EasilyNET.Core/EasilyNET.Core.csproj", "src/EasilyNET.Core/"]
COPY ["src/EasilyNET.Mongo.ConsoleDebug/EasilyNET.Mongo.ConsoleDebug.csproj", "src/EasilyNET.Mongo.ConsoleDebug/"]
COPY ["src/EasilyNET.Mongo.AspNetCore/EasilyNET.Mongo.AspNetCore.csproj", "src/EasilyNET.Mongo.AspNetCore/"]
COPY ["src/EasilyNET.Mongo.Core/EasilyNET.Mongo.Core.csproj", "src/EasilyNET.Mongo.Core/"]
COPY ["src/EasilyNET.RabbitBus.AspNetCore/EasilyNET.RabbitBus.AspNetCore.csproj", "src/EasilyNET.RabbitBus.AspNetCore/"]
COPY ["src/EasilyNET.RabbitBus.Core/EasilyNET.RabbitBus.Core.csproj", "src/EasilyNET.RabbitBus.Core/"]
COPY ["src/EasilyNET.Security/EasilyNET.Security.csproj", "src/EasilyNET.Security/"]
COPY ["src/EasilyNET.WebCore/EasilyNET.WebCore.csproj", "src/EasilyNET.WebCore/"]
RUN dotnet restore "./sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj"
COPY . .
WORKDIR "/src/sample/WebApi.Test.Unit"
RUN dotnet build "./WebApi.Test.Unit.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebApi.Test.Unit.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.Test.Unit.dll"]