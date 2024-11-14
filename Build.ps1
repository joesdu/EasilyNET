# Taken from psake https://github.com/psake/psake

<#
.SYNOPSIS
  This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
  to see if an error occcured. If an error is detected then an exception is thrown.
  This function allows you to run command-line programs without having to
  explicitly check the $lastexitcode variable.
.EXAMPLE
  exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
#>
function Exec
{
  [CmdletBinding()]
  param(
    [Parameter(Position = 0, Mandatory = 1)][scriptblock]$cmd,
    [Parameter(Position = 1, Mandatory = 0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
  )
  & $cmd
  if ($lastexitcode -ne 0)
  {
    throw ("Exec: " + $errorMessage)
  }
}

# 尝试从环境变量中获取 SOLUTION，如果不存在则使用默认值
$SOLUTION = $env:SOLUTION
if (-not $SOLUTION)
{
  $SOLUTION = "EasilyNET.sln"
}
$ARTIFACTS = $env:ARTIFACTS
if (-not $ARTIFACTS)
{
  $ARTIFACTS = ".\artifacts"
}

if (Test-Path $ARTIFACTS)
{
  Remove-Item $ARTIFACTS -Force -Recurse
}

exec { & dotnet clean $SOLUTION -c Release }
exec { & dotnet build $SOLUTION -c Release }
exec { & dotnet test $SOLUTION -c Release --no-build -l trx --verbosity=normal }

# Core
exec { & dotnet pack .\src\EasilyNET.Core\EasilyNET.Core.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
# exec { & dotnet pack .\src\EasilyNET.Core.SourceGenerator\EasilyNET.Core.SourceGenerator.csproj -c Release -o $ARTIFACTS --no-build }
exec { & dotnet pack .\src\EasilyNET.WebCore\EasilyNET.WebCore.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.WebCore.Swagger\EasilyNET.WebCore.Swagger.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }

# Framework
exec { & dotnet pack .\src\EasilyNET.AutoDependencyInjection\EasilyNET.AutoDependencyInjection.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.AutoDependencyInjection.Core\EasilyNET.AutoDependencyInjection.Core.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.ExpressMapper\EasilyNET.ExpressMapper.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.RabbitBus.AspNetCore\EasilyNET.RabbitBus.AspNetCore.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.RabbitBus.Core\EasilyNET.RabbitBus.Core.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.Security\EasilyNET.Security.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }

# Mongo
exec { & dotnet pack .\src\EasilyNET.Mongo.AspNetCore\EasilyNET.Mongo.AspNetCore.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.Mongo.ConsoleDebug\EasilyNET.Mongo.ConsoleDebug.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
exec { & dotnet pack .\src\EasilyNET.Mongo.Core\EasilyNET.Mongo.Core.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
# exec { & dotnet pack .\src\EasilyNET.MongoDistributedLock\EasilyNET.MongoDistributedLock.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }
# exec { & dotnet pack .\src\EasilyNET.MongoDistributedLock.AspNetCore\EasilyNET.MongoDistributedLock.AspNetCore.csproj -c Release -o $ARTIFACTS --include-symbols -p:SymbolPackageFormat=snupkg --no-build }