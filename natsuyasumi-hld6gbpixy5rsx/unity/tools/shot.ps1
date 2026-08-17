# Run the built Windows player with test arguments and capture screenshots.
#
# NOTE: keep this file ASCII-only (see rebuild.ps1 for why).
#
# The player build is the ONLY trustworthy way to check how this game looks.
# Editor-side cam.Render() in batchmode lies (post FX and billboards differ).
#
# Examples:
#   .\shot.ps1 -tod hiru -tag a
#   .\shot.ps1 -clock 18.5 -tag yugata            # blended time of day
#   .\shot.ps1 -at '-30,-17' -tag lookout         # teleport (x,z or x,z,y)
#   .\shot.ps1 -at '0,-2,0.1' -cam '10,24,180'    # indoors (3rd value = height)
#   .\shot.ps1 -play tsuri -playwait 0 -shots 3   # capture DURING an activity
#   .\shot.ps1 -face 4 -pose 2 -tag back-run      # pin sprite direction/pose
#   .\shot.ps1 -walk '1,0' -walkhold -run -shots 6 -gap 0.07   # run cycle
param(
  [string]$tod = 'asa', [string]$tag = 'a', [int]$shots = 1, [double]$gap = 1.5,
  [string]$weather = 'hare', [string]$cam = '', [string]$walk = '', [int]$bugs = 0,
  [switch]$book, [int]$sumo = 0, [string]$diary = '', [int]$hyohon = 0, [int]$neru = 0, [switch]$tosi, [int]$shina = 0,
  [string]$at = '', [string]$clock = '', [string]$play = '', [string]$playwait = '1',
  [int]$frames = 150, [string]$face = '', [string]$pose = '',
  [switch]$run, [switch]$walkhold, [string]$walksec = '2.0',
  [string]$OutDir = '', [string]$day = ''
)
$ErrorActionPreference = 'Stop'

$UNITY = Split-Path -Parent $PSScriptRoot                     # ...\unity
$EXE   = Join-Path $UNITY 'Builds\win\natsuyasumi.exe'
if (-not (Test-Path $EXE)) { throw "player not built: $EXE  (run rebuild.ps1 first)" }
if ($OutDir -eq '') { $OutDir = Join-Path $env:TEMP 'natsuyasumi\shots' }
New-Item -ItemType Directory -Force $OutDir | Out-Null

$target = Join-Path $OutDir "$tag.png"
Get-ChildItem $OutDir -Filter "$tag*.png" -ErrorAction SilentlyContinue | Remove-Item -Force

$a = @('-screen-width','1280','-screen-height','720','-screen-fullscreen','0',
       '-weather', $weather,
       '-shot', $target, '-shotframes', "$frames", '-shots', "$shots", '-shotgap', "$gap",
       '-logFile', (Join-Path $OutDir "$tag.log"))
if ($cam  -ne '') { $a += @('-cam', $cam) }
if ($day  -ne '') { $a += @('-day', $day) }
if ($walk -ne '') { $a += @('-walk', $walk, '-walksec', $walksec) }
if ($run)         { $a += @('-run','1') }
if ($walkhold)    { $a += @('-walkhold','1') }
if ($bugs -gt 0)  { $a += @('-bugs', "$bugs") }
if ($book)        { $a += @('-book','1') }
if ($diary -ne '') { $a += @('-diary', $diary) }
if ($hyohon -gt 0) { $a += @('-hyohon', "$hyohon") }
if ($neru -gt 0) { $a += @('-neru', "$neru") }
if ($tosi) { $a += @('-tosi','1') }
if ($shina -gt 0) { $a += @('-shina', "$shina") }
if ($sumo -gt 0)  { $a += @('-sumo', "$sumo") }
if ($at    -ne '') { $a += @('-at', $at) }
# When -clock is used we must NOT pass -tod: a later -tod pins the discrete preset again.
if ($clock -ne '') { $a += @('-clock', $clock) } else { $a += @('-tod', $tod) }
if ($play  -ne '') { $a += @('-play', $play, '-playwait', $playwait) }
if ($face  -ne '') { $a += @('-face', $face) }
if ($pose  -ne '') { $a += @('-pose', $pose) }

$pr = Start-Process -FilePath $EXE -PassThru -Wait -ArgumentList $a
# NOTE: exit=-1073741819 (0xC0000005) on shutdown is normal here; the log ends cleanly.
Write-Output "exit=$($pr.ExitCode)"
Get-ChildItem $OutDir -Filter "$tag*.png" | ForEach-Object { "$($_.Name)  $($_.Length) bytes" }
$log = Join-Path $OutDir "$tag.log"
if (Test-Path $log) {
  Get-Content $log -Encoding UTF8 |
    Select-String -Pattern 'AutoShot|BugSpawner|BugCage|PlayHost|Exception|NullReference|error' |
    Select-Object -First 30 | ForEach-Object { $_.Line }
}
Write-Output "out: $OutDir"
