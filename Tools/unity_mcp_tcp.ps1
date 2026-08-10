param(
    [Parameter(Mandatory = $true)][string]$Type,
    [string]$ParamsJson = '{}',
    [int]$Port = 6400,
    [int]$TimeoutMs = 60000
)

$ErrorActionPreference = 'Stop'

function Write-UInt64BE([UInt64]$value) {
    $b = New-Object byte[] 8
    for ($i = 7; $i -ge 0; $i--) {
        $b[$i] = [byte]($value -band 0xFF)
        $value = [UInt64]([Math]::Floor($value / 256))
    }
    return $b
}

function Read-Exact([System.Net.Sockets.NetworkStream]$stream, [int]$count) {
    $buf = New-Object byte[] $count
    $off = 0
    while ($off -lt $count) {
        $n = $stream.Read($buf, $off, $count - $off)
        if ($n -le 0) { throw "EOF reading $count bytes (got $off)" }
        $off += $n
    }
    return $buf
}

# Avoid ConvertFrom-Json eating nested objects oddly — build params via Newtonsoft-less string splice.
if ([string]::IsNullOrWhiteSpace($ParamsJson)) { $ParamsJson = '{}' }
$payload = "{`"type`":`"$Type`",`"params`":$ParamsJson}"

$client = New-Object System.Net.Sockets.TcpClient
$client.ReceiveTimeout = $TimeoutMs
$client.SendTimeout = $TimeoutMs
$client.Connect('127.0.0.1', $Port)
$stream = $client.GetStream()

$hsBuf = New-Object byte[] 128
$hsN = $stream.Read($hsBuf, 0, $hsBuf.Length)
$hs = [Text.Encoding]::UTF8.GetString($hsBuf, 0, $hsN).Trim()
if ($hs -notmatch 'WELCOME UNITY-MCP') { throw "Unexpected handshake: $hs" }

$bytes = [Text.Encoding]::UTF8.GetBytes($payload)
$header = Write-UInt64BE([UInt64]$bytes.Length)
$stream.Write($header, 0, 8)
$stream.Write($bytes, 0, $bytes.Length)

$respHeader = Read-Exact $stream 8
$len = [UInt64]0
for ($i = 0; $i -lt 8; $i++) { $len = ($len * 256) + [UInt64]$respHeader[$i] }
if ($len -gt 64MB) { throw "Response too large: $len" }
$respBytes = Read-Exact $stream ([int]$len)
Write-Output ([Text.Encoding]::UTF8.GetString($respBytes))
$client.Close()
