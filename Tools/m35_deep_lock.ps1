Write-Host '=== all processes with Gatekeeper in cmdline ==='
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -and $_.CommandLine -match 'Gatekeeper' } |
  Select-Object ProcessId, Name, CommandLine |
  Format-List

Write-Host '=== executables under Unity Hub Editor folders running? ==='
Get-CimInstance Win32_Process |
  Where-Object { $_.ExecutablePath -and $_.ExecutablePath -match 'Unity\\Hub\\Editor' } |
  Select-Object ProcessId, Name, ExecutablePath, CommandLine |
  Format-List

Write-Host '=== crash handlers / helpers ==='
Get-Process |
  Where-Object { $_.ProcessName -match 'Crash|BugSplat|Unity|mono|bee' } |
  Format-Table Id, ProcessName -AutoSize

Write-Host '=== open handles on project (via openfiles if admin) ==='
# fallback: list recently written Library files
Get-ChildItem 'f:\Programming\Gatekeeper Bastion\Library' -File -ErrorAction SilentlyContinue |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 15 Name, LastWriteTime |
  Format-Table -AutoSize

Write-Host '=== Temp after failed launch ==='
Get-ChildItem 'f:\Programming\Gatekeeper Bastion\Temp' -ErrorAction SilentlyContinue |
  Format-Table Name, Length, LastWriteTime -AutoSize

Write-Host '=== ProjectVersion last write ==='
Get-Item 'f:\Programming\Gatekeeper Bastion\ProjectSettings\ProjectVersion.txt' |
  Format-List FullName, LastWriteTime, Attributes

# Check for socket/pipe
Write-Host '=== named pipes mentioning Unity ==='
[System.IO.Directory]::GetFiles('\\.\pipe\') |
  Where-Object { $_ -match 'Unity|unity' } |
  Select-Object -First 40
