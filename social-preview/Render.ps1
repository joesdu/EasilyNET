<#
.SYNOPSIS
  将 social-preview.html 渲染为 GitHub 仓库社交预览图(Social preview)。

.DESCRIPTION
  使用本机已安装的 Chromium 内核浏览器(Microsoft Edge / Google Chrome)以无头模式截图。
  画布固定为 1280x640(GitHub 推荐比例 2:1),默认以 1.5 倍设备像素比输出 1920x960,
  兼顾清晰度与 GitHub 1MB 的体积上限。

.PARAMETER Scale
  设备像素比。1 => 1280x640,1.5 => 1920x960(默认),2 => 2560x1280(接近 1MB,慎用)。

.PARAMETER OutFile
  输出的 PNG 路径,默认为脚本同目录下的 social-preview.png。

.PARAMETER BrowserPath
  手动指定浏览器可执行文件路径。未指定时自动探测。

.EXAMPLE
  ./Render.ps1

.EXAMPLE
  ./Render.ps1 -Scale 2 -OutFile ./preview@2x.png

.NOTES
  渲染完成后需手动上传:仓库 Settings -> General -> Social preview -> Upload an image。
#>
[CmdletBinding()]
param(
  [ValidateRange(1, 3)]
  [double]$Scale = 1.5,

  [string]$OutFile,

  [string]$BrowserPath
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSCommandPath
$html = Join-Path $root 'social-preview.html'
if (-not (Test-Path $html)) {
  throw "找不到源文件: $html"
}
if (-not $OutFile) {
  $OutFile = Join-Path $root 'social-preview.png'
}

function Resolve-Browser {
  param([string]$Explicit)

  if ($Explicit) {
    if (-not (Test-Path $Explicit)) { throw "指定的浏览器不存在: $Explicit" }
    return $Explicit
  }

  # PATH 中的常见命令
  foreach ($name in @('msedge', 'chrome', 'chromium', 'google-chrome', 'chromium-browser')) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
  }

  # 各平台默认安装位置
  $candidates = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
    "${env:ProgramFiles}\Microsoft\Edge\Application\msedge.exe"
    "${env:ProgramFiles}\Google\Chrome\Application\chrome.exe"
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
    '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge'
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
    '/usr/bin/microsoft-edge'
    '/usr/bin/google-chrome'
    '/usr/bin/chromium'
  )
  foreach ($path in $candidates) {
    if ($path -and (Test-Path $path)) { return $path }
  }

  throw '未找到 Microsoft Edge 或 Google Chrome,请通过 -BrowserPath 指定浏览器路径。'
}

$browser = Resolve-Browser -Explicit $BrowserPath
$uri = ([uri](Resolve-Path $html).ProviderPath).AbsoluteUri

if (Test-Path $OutFile) {
  Remove-Item $OutFile -Force
}

Write-Host "Render.ps1: 使用 $browser"
& $browser `
  --headless=new `
  --disable-gpu `
  --hide-scrollbars `
  --force-device-scale-factor=$Scale `
  --window-size=1280,640 `
  "--screenshot=$OutFile" `
  $uri | Out-Null

# 无头截图为异步落盘,等待文件生成
$deadline = (Get-Date).AddSeconds(30)
while (-not (Test-Path $OutFile) -and (Get-Date) -lt $deadline) {
  Start-Sleep -Milliseconds 200
}
if (-not (Test-Path $OutFile)) {
  throw "渲染失败,未生成: $OutFile"
}

$size = (Get-Item $OutFile).Length
Write-Host ("Render.ps1: 已生成 {0} ({1:N0} KB, DPR {2})" -f $OutFile, ($size / 1KB), $Scale)
if ($size -gt 1MB) {
  Write-Warning 'GitHub 社交预览图上限为 1MB,请降低 -Scale 后重新生成。'
}
