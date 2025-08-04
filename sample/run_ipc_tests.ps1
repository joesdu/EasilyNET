#!/usr/bin/env pwsh

Write-Host "===========================================" -ForegroundColor Green
Write-Host "   EasilyNET.Ipc 测试项目启动脚本" -ForegroundColor Green  
Write-Host "===========================================" -ForegroundColor Green
Write-Host ""

Write-Host "请选择要启动的项目:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 启动 IPC 服务端" -ForegroundColor Cyan
Write-Host "2. 启动 IPC 客户端（测试）" -ForegroundColor Cyan
Write-Host "3. 同时启动服务端和客户端" -ForegroundColor Cyan
Write-Host "4. 构建所有项目" -ForegroundColor Cyan
Write-Host "5. 清理构建文件" -ForegroundColor Cyan
Write-Host ""

$choice = Read-Host "请输入选择 (1-5)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "🔧 启动 IPC 服务端..." -ForegroundColor Yellow
        Write-Host ""
        Set-Location "EasilyNET.Ipc.Server.Sample"
        if ($IsWindows) {
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run"
        } else {
            # Linux/macOS - 在新终端中运行
            if (Get-Command gnome-terminal -ErrorAction SilentlyContinue) {
                gnome-terminal -- pwsh -NoExit -Command "dotnet run"
            } elseif (Get-Command xterm -ErrorAction SilentlyContinue) {
                xterm -e "pwsh -NoExit -Command 'dotnet run'" &
            } else {
                Write-Host "⚠️  无法检测到终端，请手动在新终端中运行: dotnet run" -ForegroundColor Yellow
            }
        }
        Write-Host "✅ 服务端已在新窗口中启动" -ForegroundColor Green
        Write-Host "💡 请等待服务端完全启动后再运行客户端测试" -ForegroundColor Blue
    }
    
    "2" {
        Write-Host ""
        Write-Host "🔧 启动 IPC 客户端测试..." -ForegroundColor Yellow
        Write-Host "💡 请确保服务端已经启动" -ForegroundColor Blue
        Write-Host ""
        Set-Location "EasilyNET.Ipc.Client.Sample"
        dotnet run
    }
    
    "3" {
        Write-Host ""
        Write-Host "🔧 同时启动服务端和客户端..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "启动服务端..." -ForegroundColor Cyan
        Set-Location "EasilyNET.Ipc.Server.Sample"
        
        if ($IsWindows) {
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run"
        } else {
            if (Get-Command gnome-terminal -ErrorAction SilentlyContinue) {
                gnome-terminal -- pwsh -NoExit -Command "dotnet run"
            } elseif (Get-Command xterm -ErrorAction SilentlyContinue) {
                xterm -e "pwsh -NoExit -Command 'dotnet run'" &
            }
        }
        
        Write-Host ""
        Write-Host "等待服务端启动... (5秒)" -ForegroundColor Blue
        Start-Sleep -Seconds 5
        Write-Host ""
        Write-Host "启动客户端测试..." -ForegroundColor Cyan
        Set-Location "../EasilyNET.Ipc.Client.Sample"
        dotnet run
    }
    
    "4" {
        Write-Host ""
        Write-Host "🔨 构建所有项目..." -ForegroundColor Yellow
        Write-Host ""
        
        Write-Host "构建服务端项目..." -ForegroundColor Cyan
        Set-Location "EasilyNET.Ipc.Server.Sample"
        $result = dotnet build
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ 服务端项目构建失败" -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ 服务端项目构建成功" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "构建客户端项目..." -ForegroundColor Cyan
        Set-Location "../EasilyNET.Ipc.Client.Sample"
        $result = dotnet build
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ 客户端项目构建失败" -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ 客户端项目构建成功" -ForegroundColor Green
        Write-Host ""
        Write-Host "🎯 所有项目构建完成！" -ForegroundColor Green
    }
    
    "5" {
        Write-Host ""
        Write-Host "🧹 清理构建文件..." -ForegroundColor Yellow
        Write-Host ""
        
        Write-Host "清理服务端..." -ForegroundColor Cyan
        Set-Location "EasilyNET.Ipc.Server.Sample"
        dotnet clean
        Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host ""
        
        Write-Host "清理客户端..." -ForegroundColor Cyan
        Set-Location "../EasilyNET.Ipc.Client.Sample"
        dotnet clean
        Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host ""
        Write-Host "✅ 清理完成！" -ForegroundColor Green
    }
    
    default {
        Write-Host "无效选择，退出..." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
