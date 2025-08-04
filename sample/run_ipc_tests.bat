@echo off
echo ===========================================
echo    EasilyNET.Ipc 测试项目启动脚本
echo ===========================================
echo.

echo 请选择要启动的项目:
echo.
echo 1. 启动 IPC 服务端
echo 2. 启动 IPC 客户端（测试）
echo 3. 同时启动服务端和客户端
echo 4. 构建所有项目
echo 5. 清理构建文件
echo.

set /p choice=请输入选择 (1-5): 

if "%choice%"=="1" goto server
if "%choice%"=="2" goto client
if "%choice%"=="3" goto both
if "%choice%"=="4" goto build
if "%choice%"=="5" goto clean

echo 无效选择，退出...
goto end

:server
echo.
echo 🔧 启动 IPC 服务端...
echo.
cd EasilyNET.Ipc.Server.Sample
start "IPC Server" cmd /k "dotnet run"
echo ✅ 服务端已在新窗口中启动
echo 💡 请等待服务端完全启动后再运行客户端测试
goto end

:client
echo.
echo 🔧 启动 IPC 客户端测试...
echo 💡 请确保服务端已经启动
echo.
cd EasilyNET.Ipc.Client.Sample
dotnet run
goto end

:both
echo.
echo 🔧 同时启动服务端和客户端...
echo.
echo 启动服务端...
cd EasilyNET.Ipc.Server.Sample
start "IPC Server" cmd /k "dotnet run"
echo.
echo 等待服务端启动... (5秒)
timeout /t 5 /nobreak > nul
echo.
echo 启动客户端测试...
cd ..\EasilyNET.Ipc.Client.Sample
dotnet run
goto end

:build
echo.
echo 🔨 构建所有项目...
echo.
echo 构建服务端项目...
cd EasilyNET.Ipc.Server.Sample
dotnet build
if errorlevel 1 (
    echo ❌ 服务端项目构建失败
    goto end
)
echo ✅ 服务端项目构建成功
echo.

echo 构建客户端项目...
cd ..\EasilyNET.Ipc.Client.Sample
dotnet build
if errorlevel 1 (
    echo ❌ 客户端项目构建失败
    goto end
)
echo ✅ 客户端项目构建成功
echo.
echo 🎯 所有项目构建完成！
goto end

:clean
echo.
echo 🧹 清理构建文件...
echo.
echo 清理服务端...
cd EasilyNET.Ipc.Server.Sample
dotnet clean
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul
echo.

echo 清理客户端...
cd ..\EasilyNET.Ipc.Client.Sample
dotnet clean
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul
echo.
echo ✅ 清理完成！
goto end

:end
echo.
echo 按任意键退出...
pause > nul
