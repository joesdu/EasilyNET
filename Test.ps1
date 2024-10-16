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
function Exec {
  [CmdletBinding()]
  param(
    [Parameter(Position = 0, Mandatory = 1)][scriptblock]$cmd,
    [Parameter(Position = 1, Mandatory = 0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
  )
  & $cmd
  if ($lastexitcode -ne 0) {
    throw ("Exec: " + $errorMessage)
  }
}

# 尝试从环境变量中获取 SOLUTION，如果不存在则使用默认值
$SOLUTION = $env:SOLUTION
if (-not $SOLUTION) {
  $SOLUTION = "EasilyNET.slnx"
}

exec { & dotnet clean $SOLUTION -c Release }
exec { & dotnet build $SOLUTION -c Release }
exec { & dotnet test $SOLUTION -c Release --no-build -l trx --verbosity=normal }