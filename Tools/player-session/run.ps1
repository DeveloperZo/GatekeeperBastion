# Gatekeeper's Protocol — same-machine two-player session (Windows).
#
#   Tools/player-session/one-click.cmd          # build + interactive windowed dual-box
#   Tools/player-session/run.ps1 -Build -Interactive
#   Tools/player-session/run.ps1 -Exe "..." -Interactive
#   Tools/player-session/run.ps1 -Exe "..."     # headless connect check (auto-quit)
#
# Avoid $host* names — PowerShell reserves $Host.

param(
    [string]$Exe = $env:GKB_PLAYER_EXE,
    [switch]$Build,
    [switch]$Interactive,
    [int]$QuitAfter = 18,
    [int]$JoinWait = 5,
    [int]$PollSecs = 30,
    [int]$Width = 960,
    [int]$Height = 540
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$Report = Join-Path $Root "gkb_player_build_report.json"
$Req = Join-Path $Root "gkb_player_build_request.txt"
$DefaultExe = Join-Path $Root "Logs\Builds\GatekeepersProtocol.exe"
$PersistCandidates = @(
    (Join-Path $env:USERPROFILE "AppData\LocalLow\Gatekeeper\Gatekeeper's Protocol\Logs"),
    (Join-Path $env:USERPROFILE "AppData\LocalLow\Gatekeeper\Gatekeeper Bastion\Logs"),
    (Join-Path $env:USERPROFILE "AppData\LocalLow\DefaultCompany\Gatekeeper's Protocol\Logs"),
    (Join-Path $env:USERPROFILE "AppData\LocalLow\DefaultCompany\Gatekeeper Bastion\Logs")
)
$OutDir = Join-Path $Root "Logs"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

if ($Build) {
    Remove-Item -Force -ErrorAction SilentlyContinue $Report
    Set-Content -Path $Req -Value ("build " + [DateTime]::UtcNow.ToString("o"))
    Write-Host "player-session: waiting for Unity player build..."
    $deadline = (Get-Date).AddMinutes(20)
    while ((Get-Date) -lt $deadline -and -not (Test-Path $Report)) {
        Start-Sleep -Seconds 1
    }
    if (-not (Test-Path $Report)) {
        Write-Error "player-session: timed out waiting for gkb_player_build_report.json"
        exit 2
    }
    $body = Get-Content -Raw $Report
    if ($body -notmatch '"status"\s*:\s*"ok"') {
        Write-Host $body
        Write-Error "player-session: player build failed"
        exit 2
    }
    $Exe = $DefaultExe
}

if ([string]::IsNullOrWhiteSpace($Exe)) {
    $exeFallbacks = @(
        $DefaultExe,
        (Join-Path $Root "Logs\build\Gatekeeper's Protocol.exe"),
        (Join-Path $Root "Logs\Builds\Gatekeeper Bastion.exe"),
        (Join-Path $Root "Logs\build\Gatekeeper Bastion.exe")
    )
    foreach ($cand in $exeFallbacks) {
        if (Test-Path -LiteralPath $cand) { $Exe = $cand; break }
    }
}

if ([string]::IsNullOrWhiteSpace($Exe) -or -not (Test-Path $Exe)) {
    Write-Error "player-session: no player exe. Pass -Exe, set GKB_PLAYER_EXE, or use -Build."
    exit 2
}

function New-PlayerArgs {
    param([string[]]$RoleArgs, [int]$PosX)
    $a = New-Object System.Collections.Generic.List[string]
    foreach ($r in $RoleArgs) { [void]$a.Add($r) }
    [void]$a.Add("-screen-fullscreen"); [void]$a.Add("0")
    [void]$a.Add("-screen-width"); [void]$a.Add("$Width")
    [void]$a.Add("-screen-height"); [void]$a.Add("$Height")
    [void]$a.Add("-screen-quality"); [void]$a.Add("Fastest")
    # Normal window chrome (title bar / min-max). -popupwindow stripped those and blocked eyes-on.
    if (-not $Interactive) {
        [void]$a.Add("-gkb-quit-after"); [void]$a.Add("$QuitAfter")
    }
    return ,$a.ToArray()
}

$cutoff = Get-Date
$serverArgs = New-PlayerArgs -RoleArgs @("-gkb-host") -PosX 0
$clientArgs = New-PlayerArgs -RoleArgs @("-gkb-join", "127.0.0.1") -PosX $Width

Write-Host ("player-session: host - " + ($serverArgs -join " "))
$serverProc = Start-Process -FilePath $Exe -ArgumentList $serverArgs -PassThru `
    -WorkingDirectory (Split-Path $Exe)
Start-Sleep -Seconds $JoinWait
Write-Host ("player-session: join - " + ($clientArgs -join " "))
$clientProc = Start-Process -FilePath $Exe -ArgumentList $clientArgs -PassThru `
    -WorkingDirectory (Split-Path $Exe)

if ($Interactive) {
    Write-Host ""
    Write-Host "Interactive dual-box: two windowed players. Drive both; Esc/quit each when done."
    Write-Host "Waiting for both processes to exit..."
    Wait-Process -Id $serverProc.Id, $clientProc.Id
} else {
    $waitUntil = (Get-Date).AddSeconds($PollSecs)
    while ((Get-Date) -lt $waitUntil) {
        if ($serverProc.HasExited -and $clientProc.HasExited) { break }
        Start-Sleep -Seconds 1
    }
    if (-not $serverProc.HasExited) { Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue }
    if (-not $clientProc.HasExited) { Stop-Process -Id $clientProc.Id -Force -ErrorAction SilentlyContinue }
}

# Player logs: companyName Gatekeeper, productName Gatekeeper's Protocol.
# Try current then legacy product, each under Gatekeeper then DefaultCompany.
$logs = foreach ($dir in $PersistCandidates) {
    if (-not (Test-Path -LiteralPath $dir)) { continue }
    Get-ChildItem -LiteralPath $dir -Filter "GatekeepersProtocol_*.log" -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath $dir -Filter "GatekeeperBastion_*.log" -ErrorAction SilentlyContinue
}
$logs = @($logs) | Where-Object { $_ -and $_.LastWriteTime -ge $cutoff.AddSeconds(-2) } |
    Sort-Object LastWriteTime -Descending

$serverLog = $null
$clientLog = $null
foreach ($f in $logs) {
    $text = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $text) { continue }
    if ($text -match '\[Net\] host started') { $serverLog = $f.FullName }
    if ($text -match 'role=client') { $clientLog = $f.FullName }
}

Write-Host ("player-session: host log  = " + $(if ($serverLog) { $serverLog } else { "NONE" }))
Write-Host ("player-session: client log = " + $(if ($clientLog) { $clientLog } else { "NONE" }))

$hasFpcWarn = $false
foreach ($f in $logs) {
    $text = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
    if ($text -match 'No FirstPersonController found') { $hasFpcWarn = $true }
}

if ($serverLog -and $clientLog -and -not $hasFpcWarn) {
    Write-Host "player-session: PASS - host started, client connected, local Player present"
    Select-String -Path $serverLog, $clientLog -Pattern '\[Net\] (host started|connected|client starting|join refused|avatar)|\[Launch\]' |
        ForEach-Object { $_.Line }
    exit 0
}

if ($serverLog -and $clientLog -and $hasFpcWarn) {
    Write-Host "player-session: FAIL - connected but No FirstPersonController (cannot drive either instance)" -ForegroundColor Red
    exit 1
}

Write-Host "player-session: FAIL - need host started + client role=client" -ForegroundColor Red
foreach ($f in $logs) {
    Write-Host ("---- " + $f.FullName + " ----")
    Select-String -Path $f.FullName -Pattern '\[Net\]|\[Launch\]|FirstPerson|Exception|OptionsView' | Select-Object -First 30 | ForEach-Object { $_.Line }
}
exit 1
