Write-Host '=== processes mentioning Gatekeeper or Unity Editor ==='
Get-CimInstance Win32_Process |
  Where-Object {
    $_.Name -match 'Unity' -or
    ($_.CommandLine -and $_.CommandLine -match 'Gatekeeper|Unity\.exe')
  } |
  Select-Object ProcessId, Name, CommandLine |
  Format-List

Write-Host '=== Library lock files ==='
Get-ChildItem 'f:\Programming\Gatekeeper Bastion\Library' -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -like '*lock*' -or $_.Name -like '*Lock*' -or $_.Name -like '*Instance*' } |
  Format-Table Name, Length, LastWriteTime -AutoSize

Write-Host '=== try open SourceAssetDB-lock ==='
$p = 'f:\Programming\Gatekeeper Bastion\Library\SourceAssetDB-lock'
try {
  $fs = [System.IO.File]::Open($p, 'Open', 'ReadWrite', 'None')
  Write-Host 'SourceAssetDB-lock is NOT locked by another process'
  $fs.Close()
} catch {
  Write-Host ("SourceAssetDB-lock BUSY: " + $_.Exception.Message)
}

$p2 = 'f:\Programming\Gatekeeper Bastion\Library\ArtifactDB-lock'
try {
  $fs2 = [System.IO.File]::Open($p2, 'Open', 'ReadWrite', 'None')
  Write-Host 'ArtifactDB-lock is NOT locked by another process'
  $fs2.Close()
} catch {
  Write-Host ("ArtifactDB-lock BUSY: " + $_.Exception.Message)
}
