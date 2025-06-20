<#
.SYNOPSIS
    递归删除 'bin' 和 'obj' 文件夹。
.DESCRIPTION
    此 PowerShell 脚本查找并删除指定文件夹及其子文件夹中所有名为 'bin' 或 'obj' 的目录。
.PARAMETER TargetDirectory
    清理操作的起始目录。默认为当前目录。
.EXAMPLE
    .\clean.ps1 -TargetDirectory "C:\Projects\MySolution"
    删除 C:\Projects\MySolution 及其子目录下的所有 'bin' 和 'obj' 文件夹。
.EXAMPLE
    .\clean.ps1
    删除当前目录及其子目录下的所有 'bin' 和 'obj' 文件夹。
.EXAMPLE
    .\clean.ps1 -WhatIf
    显示将要被删除的文件夹，但不会实际执行删除操作。
#>
[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(Mandatory=$false, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
    [string]$TargetDirectory = "."
)

process {
    try {
        $fullPath = (Resolve-Path $TargetDirectory).Path
        Write-Verbose "在 '$fullPath' 中搜索 'bin' 和 'obj' 文件夹。"

        # 查找所有 'bin' 和 'obj' 目录
        $foldersToDelete = Get-ChildItem -Path $fullPath -Include "bin", "obj" -Recurse -Directory -ErrorAction SilentlyContinue

        if ($foldersToDelete) {
            foreach ($folder in $foldersToDelete) {
                $folderPath = $folder.FullName
                if ($PSCmdlet.ShouldProcess($folderPath, "删除文件夹")) {
                    Write-Host "正在删除文件夹: $folderPath"
                    Remove-Item -Path $folderPath -Recurse -Force
                }
            }
        } else {
            Write-Host "未找到 'bin' 或 'obj' 文件夹。"
        }

        Write-Host "清理脚本执行完毕。"
    }
    catch {
        Write-Error "发生错误: $_"
    }
}