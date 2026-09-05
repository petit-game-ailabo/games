# Bake the 3D Marisa into an 8x10 sprite sheet, then copy the result out for review.
#
# NOTE: keep this file ASCII-only (see rebuild.ps1 for why).
#
# This is a WORKBENCH. It writes to unity/ArtSource/marisa3d/ and does NOT touch the
# garden scene or Assets/Art/Sprites. Nothing changes in the game until we copy it in.
#
# It runs Unity WITHOUT -nographics: the bake needs a GPU to render into a RenderTexture.
param([string]$LogDir = '')
$ErrorActionPreference = 'Stop'

$PRJ = Split-Path -Parent $PSScriptRoot
$U   = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
if (-not (Test-Path $U)) { throw "Unity not found: $U" }
if ($LogDir -eq '') { $LogDir = Join-Path $env:TEMP 'natsuyasumi' }
New-Item -ItemType Directory -Force $LogDir | Out-Null

$lock = Join-Path (Join-Path $PRJ 'Temp') 'UnityLockfile'
for ($i = 0; $i -lt 90; $i++) {
  $busy = $null -ne (Get-Process -Name Unity -ErrorAction SilentlyContinue)
  if (-not $busy -and -not (Test-Path $lock)) { break }
  Start-Sleep -Seconds 2
}

$log = Join-Path $LogDir 'u_marisa.log'
$proc = Start-Process -FilePath $U -PassThru -Wait -NoNewWindow -ArgumentList @(
  '-batchmode','-quit','-silent-crashes',
  '-projectPath', $PRJ, '-executeMethod', 'MarisaYaku.Yaku', '-logFile', $log)
Write-Output "== MarisaYaku exit=$($proc.ExitCode) =="
Get-Content $log -Encoding UTF8 |
  Select-String -Pattern 'error CS|Probe|Exception|Compilation failed' |
  Select-Object -First 30 | ForEach-Object { $_.Line }

$out = Join-Path $env:TEMP 'natsuyasumi\shots'
New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem (Join-Path $PRJ 'ArtSource\marisa3d') -Filter *.png -ErrorAction SilentlyContinue |
  ForEach-Object { Copy-Item $_.FullName (Join-Path $out $_.Name) -Force; Write-Output $_.Name }
