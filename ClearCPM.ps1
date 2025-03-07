# 1. 解析 Directory.Packages.props 文件，获取 CPM 中定义的所有包
$propsFile = "src/Directory.Packages.props"
if (-not (Test-Path $propsFile)) {
  Write-Host "错误：未找到 Directory.Packages.props 文件于路径 $propsFile"
  exit
}
Write-Host "找到 Directory.Packages.props 文件于路径 $propsFile"

$propsXml = [xml](Get-Content $propsFile)
$cpPackages = $propsXml.Project.ItemGroup.PackageVersion | ForEach-Object { $_.Include }

# 2. 查找解决方案中的所有 .csproj 文件
$csprojFiles = Get-ChildItem -Path . -Recurse -Filter *.csproj

# 3. 解析每个 .csproj 文件，获取引用的包
$referencedPackages = @()
foreach ($csproj in $csprojFiles) {
  $csprojXml = [xml](Get-Content $csproj.FullName)
  $packages = $csprojXml.Project.ItemGroup.PackageReference | ForEach-Object { $_.Include }
  $referencedPackages += $packages
}
# 去重，确保每个包只出现一次
$referencedPackages = $referencedPackages | Sort-Object -Unique

# 4. 找出在 CPM 中存在但未被任何项目引用的包
$unusedPackages = $cpPackages | Where-Object { $_ -notin $referencedPackages }

# 5. 输出未使用的包
Write-Host "以下是在 CPM 中定义但未被任何项目引用的 NuGet 包："
$unusedPackages | ForEach-Object { Write-Host $_ }