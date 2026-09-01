# 将工作区根目录刚生成的 PNG 移到 assets/styles/{style}/
param(
  [Parameter(Mandatory=$true)][string]$Style,
  [Parameter(Mandatory=$true)][string[]]$Files
)
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$proj = Split-Path $root -Parent
$dest = Join-Path $PSScriptRoot "..\assets\styles\$Style"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
foreach ($f in $Files) {
  $src = Join-Path $proj $f
  if (-not (Test-Path $src)) { $src = Join-Path $root $f }
  if (-not (Test-Path $src)) { Write-Warning "missing $f"; continue }
  Copy-Item $src (Join-Path $dest $f) -Force
  Write-Host "-> styles/$Style/$f"
}
