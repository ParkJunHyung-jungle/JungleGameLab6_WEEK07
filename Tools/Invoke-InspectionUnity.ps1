param(
    [string]$Endpoint = 'api/editor_status',
    [string]$Command,
    [string]$ParametersJson = '{}'
)

$ErrorActionPreference = 'Stop'
$inspectionRoot = Split-Path $PSScriptRoot -Parent
$inspectionDescriptorPath = Join-Path $inspectionRoot 'Library/Pipeline/.unity-pipeline-port'
$inspectionDescriptor = Get-Content -Raw -Force -LiteralPath $inspectionDescriptorPath | ConvertFrom-Json
$inspectionHeaders = @{ Authorization = 'Bearer ' + $inspectionDescriptor.evalToken }
$inspectionUri = 'http://127.0.0.1:' + $inspectionDescriptor.port + '/' + $Endpoint

if ($Command) {
    $inspectionBody = @{ command = $Command; parameters = ($ParametersJson | ConvertFrom-Json) } | ConvertTo-Json -Depth 30
    $inspectionUri = 'http://127.0.0.1:' + $inspectionDescriptor.port + '/api/exec'
    $inspectionResponse = Invoke-RestMethod -Uri $inspectionUri -Headers $inspectionHeaders -Method Post -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($inspectionBody)) -TimeoutSec 45
} else {
    $inspectionResponse = Invoke-RestMethod -Uri $inspectionUri -Headers $inspectionHeaders -TimeoutSec 45
}

$inspectionResponse | ConvertTo-Json -Depth 30
