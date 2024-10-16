$SCRIPTNAME = $MyInvocation.MyCommand.Name
$ARTIFACTS = $env:ARTIFACTS
if (-not $ARTIFACTS) {
  $ARTIFACTS = ".\artifacts"
}

if ([string]::IsNullOrEmpty($Env:NUGET_API_KEY)) {
    Write-Host "${SCRIPTNAME}: NUGET_API_KEY is empty or not set. Skipped pushing package(s)."
}
else {
    Get-ChildItem $ARTIFACTS -Filter "*.nupkg" | ForEach-Object {
        Write-Host "$($SCRIPTNAME): Pushing $($_.Name)"
        dotnet nuget push $_ --source $Env:NUGET_URL --api-key $Env:NUGET_API_KEY
        if ($lastexitcode -ne 0) {
            throw ("Exec: " + $errorMessage)
        }
    }
}
