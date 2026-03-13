param(
  [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

function Ensure-Dir([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Path $Path | Out-Null
  }
}

function Copy-FileIfExists([string]$Source, [string]$Dest) {
  if (Test-Path -LiteralPath $Source) {
    $destDir = Split-Path -Parent $Dest
    Ensure-Dir $destDir
    if ($WhatIf) {
      Write-Host "[WhatIf] Copy $Source -> $Dest"
    } else {
      Copy-Item -LiteralPath $Source -Destination $Dest -Force
      Write-Host "Copied $Source -> $Dest"
    }
  } else {
    Write-Host "Skip (missing) $Source"
  }
}

function Copy-DirIfExists([string]$SourceDir, [string]$DestDir) {
  if (Test-Path -LiteralPath $SourceDir) {
    Ensure-Dir $DestDir
    if ($WhatIf) {
      Write-Host "[WhatIf] CopyDir $SourceDir -> $DestDir"
    } else {
      Copy-Item -LiteralPath (Join-Path $SourceDir '*') -Destination $DestDir -Recurse -Force
      Write-Host "Copied dir $SourceDir -> $DestDir"
    }
  } else {
    Write-Host "Skip (missing) $SourceDir"
  }
}

$insidersUser = Join-Path $env:APPDATA 'Code - Insiders\User'
$cursorUser   = Join-Path $env:APPDATA 'Cursor\User'

Write-Host "Insiders user dir: $insidersUser"
Write-Host "Cursor user dir:   $cursorUser"

Ensure-Dir $cursorUser

# Settings / keybindings / snippets
Copy-FileIfExists (Join-Path $insidersUser 'settings.json')    (Join-Path $cursorUser 'settings.json')
Copy-FileIfExists (Join-Path $insidersUser 'keybindings.json') (Join-Path $cursorUser 'keybindings.json')
Copy-DirIfExists  (Join-Path $insidersUser 'snippets')         (Join-Path $cursorUser 'snippets')

# Extension list: prefer CLI (matches what you see in the UI)
$extIds = @()
try {
  if (Get-Command code-insiders -ErrorAction SilentlyContinue) {
    $extIds = (code-insiders --list-extensions) | Where-Object { $_ -and $_.Trim().Length -gt 0 }
  } else {
    Write-Host "code-insiders not found on PATH; cannot list extensions via CLI."
  }
} catch {
  Write-Host "Failed to query code-insiders extensions: $($_.Exception.Message)"
}

if ($extIds.Count -gt 0) {
  Write-Host ("Found {0} Insiders extensions." -f $extIds.Count)
  if (-not (Get-Command cursor -ErrorAction SilentlyContinue)) {
    throw "cursor not found on PATH; cannot install extensions."
  }

  foreach ($id in $extIds) {
    if ($WhatIf) {
      Write-Host "[WhatIf] cursor --install-extension $id"
    } else {
      Write-Host "Installing $id"
      cursor --install-extension $id | Out-Null
    }
  }
} else {
  Write-Host "No extension ids found to install."
}

Write-Host "Done."

