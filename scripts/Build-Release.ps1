param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$projectFile = Join-Path $ProjectRoot 'ApexTweaker.csproj'
$releaseDir = Join-Path $ProjectRoot 'release-v2'
$stagingDir = Join-Path $ProjectRoot 'release-v2-staging'
$portableExe = Join-Path $releaseDir 'ApexTweaker.exe'
$nativeDll = Join-Path $releaseDir 'ApexTweaker.Native.dll'

[xml]$projectXml = Get-Content -LiteralPath $projectFile
$versionGroup = $projectXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
$version = [string]$versionGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'A versao nao foi encontrada no ApexTweaker.csproj.'
}
$portableZip = Join-Path $releaseDir "ApexTweaker-Portable-v$version.zip"

$projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
$releaseDirFull = [System.IO.Path]::GetFullPath($releaseDir).TrimEnd('\')
$expectedPrefix = $projectRootFull + [System.IO.Path]::DirectorySeparatorChar
if (-not $releaseDirFull.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
    -not [System.String]::Equals(
        [System.IO.Path]::GetFileName($releaseDirFull),
        'release-v2',
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Pasta de release insegura ou inesperada: $releaseDirFull"
}

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Projeto nao encontrado: $projectFile"
}

$running = Get-Process ApexTweaker -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Encerrando instancia(s) de ApexTweaker em execucao..."
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

if (Test-Path -LiteralPath $releaseDirFull) {
    Write-Host "Limpando artefatos antigos da pasta de release validada..."
    Get-ChildItem -LiteralPath $releaseDirFull -Force | Remove-Item -Recurse -Force
}
else {
    New-Item -ItemType Directory -Path $releaseDirFull | Out-Null
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

if (-not (Test-Path -LiteralPath $nativeDll)) {
    throw "DLL nativa nao encontrada depois do publish: $nativeDll"
}

Compress-Archive -LiteralPath @($portableExe, $nativeDll) -DestinationPath $portableZip -CompressionLevel Optimal

$artifact = Get-Item -LiteralPath $portableExe
$zipArtifact = Get-Item -LiteralPath $portableZip
Write-Host ""
Write-Host "Release pronta para testes:"
Write-Host "  $($artifact.FullName)"
Write-Host "  $([math]::Round($artifact.Length / 1MB, 1)) MB | $($artifact.LastWriteTime)"
Write-Host "  $($zipArtifact.FullName)"
Write-Host "  $([math]::Round($zipArtifact.Length / 1MB, 1)) MB | $($zipArtifact.LastWriteTime)"
