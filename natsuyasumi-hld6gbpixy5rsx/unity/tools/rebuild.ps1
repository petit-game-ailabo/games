# Rebuild the scene and the Windows player, headlessly.
#
# NOTE: keep this file ASCII-only. Windows PowerShell 5.1 reads a UTF-8 (no BOM)
# script as ANSI, which mangles non-ASCII bytes and breaks parsing in confusing
# ways (the reported line number is off by one).
#
#   .\rebuild.ps1                 # scene + windows player
#   .\rebuild.ps1 -Web            # also build the WebGL player into ../../unity-web
#   .\rebuild.ps1 -Only Paths     # run one -executeMethod and stop
param(
  [switch]$Web,
  [string]$Only = '',
  [string]$LogDir = ''
)
$ErrorActionPreference = 'Stop'

$PRJ = Split-Path -Parent $PSScriptRoot            # ...\unity
$U   = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
if (-not (Test-Path $U)) { throw "Unity not found: $U" }
if ($LogDir -eq '') { $LogDir = Join-Path $env:TEMP 'natsuyasumi' }
New-Item -ItemType Directory -Force $LogDir | Out-Null

# Unity keeps the project lock for a moment after "Exiting batchmode".
# Launching the next run too soon dies with "another Unity instance is running"
# and the whole script produces no output, which looks like a silent failure.
function WaitUnlock {
  $lock = Join-Path (Join-Path $PRJ 'Temp') 'UnityLockfile'
  for ($i = 0; $i -lt 90; $i++) {
    $busy = $null -ne (Get-Process -Name Unity -ErrorAction SilentlyContinue)
    if (-not $busy -and -not (Test-Path $lock)) { return }
    Start-Sleep -Seconds 2
  }
  Write-Output '== warning: project still locked after 180s =='
}

function RunUnity($method, $logName) {
  WaitUnlock
  $log = Join-Path $LogDir $logName
  $proc = Start-Process -FilePath $U -PassThru -Wait -NoNewWindow -ArgumentList @(
    '-batchmode','-quit','-nographics','-silent-crashes',
    '-projectPath', $PRJ, '-executeMethod', $method, '-logFile', $log)
  Write-Output "== $method exit=$($proc.ExitCode) =="
  Get-Content $log -Encoding UTF8 |
    Select-String -Pattern 'error CS|Shader error|Shader warning|BuildZashiki|BuildPlayerWin|BuildPlayerWeb|PixelSprite|Probe|Compilation failed|another Unity instance|Exception' |
    Select-Object -First 60 | ForEach-Object { $_.Line }
  if ($proc.ExitCode -ne 0) { throw "$method failed (log: $log)" }
}

if ($Only -ne '') {
  switch ($Only) {
    'Paths'  { RunUnity 'TerrainProbe.Paths' 'u_paths.log' }
    'Dump'   { RunUnity 'TerrainProbe.Dump'  'u_dump.log' }
    'Pixel'  { RunUnity 'SetupURP.FixPixelArt' 'u_fix.log' }
    'Scene'  { RunUnity 'BuildZashiki.Build' 'u_scene.log' }
    'Web'    { RunUnity 'BuildPlayerWeb.Build' 'u_web.log' }
    default  { RunUnity $Only 'u_only.log' }
  }
  Write-Output 'DONE'
  return
}

RunUnity 'BuildZashiki.Build'   'u_scene.log'
RunUnity 'BuildPlayerWin.Build' 'u_win.log'
if ($Web) { RunUnity 'BuildPlayerWeb.Build' 'u_web.log' }
Write-Output 'ALL OK'
