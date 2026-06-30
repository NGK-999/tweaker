param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$projectFile = Join-Path $ProjectRoot 'ApexTweaker.csproj'
$releaseDir = Join-Path $ProjectRoot 'release-v2'
$stagingDir = Join-Path $ProjectRoot 'release-v2-staging'
$portableExe = Join-Path $releaseDir 'ApexTweaker.exe'

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Projeto nao encontrado: $projectFile"
}

$running = Get-Process ApexTweaker -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Encerrando instancia(s) de ApexTweaker em execucao..."
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

Write-Host "Publicando release portatil em: $releaseDir"
& dotnet publish $projectFile -c Release -r win-x64 --self-contained true -o $releaseDir
if ($LASTEXITCODE -ne 0) {
    throw @"
Falha ao publicar em release-v2.

Se o erro for 'Access denied' em ApexTweaker.exe, feche manualmente o app
(incluindo instancias elevadas) e execute novamente:

  powershell -ExecutionPolicy Bypass -File "$PSScriptRoot\Build-Release.ps1"
"@
}

if (Test-Path -LiteralPath $stagingDir) {
    Write-Host "Removendo pasta legada release-v2-staging..."
    try {
        Remove-Item -LiteralPath $stagingDir -Recurse -Force -ErrorAction Stop
    }
    catch {
        Write-Warning @"
Nao foi possivel remover release-v2-staging (arquivo em uso).
Feche instancias do ApexTweaker e apague manualmente a pasta, ou rode o script novamente.
"@
    }
}

$artifact = Get-Item -LiteralPath $portableExe
Write-Host ""
Write-Host "Release pronta para testes:"
Write-Host "  $($artifact.FullName)"
Write-Host "  $([math]::Round($artifact.Length / 1MB, 1)) MB | $($artifact.LastWriteTime)"
