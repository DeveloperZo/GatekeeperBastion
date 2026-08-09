$ErrorActionPreference = 'Continue'
$proj = 'f:\Programming\Gatekeeper Bastion'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe'
$logDir = Join-Path $proj 'Logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

Write-Host 'Removing stale Library *-lock files if present...'
Remove-Item (Join-Path $proj 'Library\SourceAssetDB-lock') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $proj 'Library\ArtifactDB-lock') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $proj 'Library\EditorInstance.json') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $proj 'Temp\UnityLockfile') -Force -ErrorAction SilentlyContinue

# Named mutex leftovers: list any mutexes we can see via handles is hard; just retry open.
$jobs = @(
  @{ Name = 'Ship'; Method = 'GatekeeperBastion.EditorTools.ShipUiAssembler.AssembleFromBatch'; Log = 'm35_ship_assemble.log' },
  @{ Name = 'FrontEnd'; Method = 'GatekeeperBastion.EditorTools.ShiftFrontEndAssembler.BuildFromBatch'; Log = 'm35_frontend_assemble.log' },
  @{ Name = 'Bastion'; Method = 'GatekeeperBastion.EditorTools.BastionUiAssembler.AssembleFromBatch'; Log = 'm35_bastion_assemble.log' }
)

$exits = @{}
foreach ($j in $jobs) {
  $log = Join-Path $logDir $j.Log
  Remove-Item $log -Force -ErrorAction SilentlyContinue
  Write-Host ("=== {0} ===" -f $j.Name)
  $argLine = "-batchmode -nographics -quit -projectPath `"$proj`" -executeMethod $($j.Method) -logFile `"$log`""
  $p = Start-Process -FilePath $unity -ArgumentList $argLine -Wait -PassThru -NoNewWindow
  $exits[$j.Name] = $p.ExitCode
  Write-Host ("{0}_EXIT={1}" -f $j.Name, $p.ExitCode)
  if (Test-Path $log) {
    Select-String -Path $log -Pattern 'Aborting batchmode|Assembled|Built |error CS|status' |
      Select-Object -Last 20 |
      ForEach-Object { Write-Host $_.Line }
  }
}

$exits.GetEnumerator() | ForEach-Object { "{0}={1}" -f $_.Name, $_.Value } |
  Set-Content (Join-Path $logDir 'm35_assemble_exits.txt')

if (($exits.Values | Where-Object { $_ -ne 0 }).Count -gt 0) { exit 1 }
exit 0
