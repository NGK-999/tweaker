param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$installerScript = Join-Path $ProjectRoot 'installer\ApexTweaker.iss'
$outputDir = Join-Path $ProjectRoot 'release-installer'

if (-not (Test-Path -LiteralPath $installerScript)) {
    throw "Script do instalador nao encontrado: $installerScript"
}

$releaseCandidates = @(
    @{ Name = 'release-v2'; Dir = '..\release-v2' },
    @{ Name = 'release-v2-staging'; Dir = '..\release-v2-staging' }
)

$releaseSource = $releaseCandidates |
    ForEach-Object {
        $candidateExe = Join-Path $ProjectRoot ($_.Name + '\ApexTweaker.exe')
        if (Test-Path -LiteralPath $candidateExe) {
            [PSCustomObject]@{
                Name = $_.Name
                Dir = $_.Dir
                LastWrite = (Get-Item -LiteralPath $candidateExe).LastWriteTimeUtc
            }
        }
    } |
    Sort-Object LastWrite -Descending |
    Select-Object -First 1

if (-not $releaseSource) {
    throw "Binario portatil nao encontrado. Gere release-v2\ApexTweaker.exe (ou release-v2-staging) antes."
}

Write-Host "Usando pasta de release: $($releaseSource.Name)"

function Resolve-InnoSetupCompiler {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidatePaths = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 5\ISCC.exe',
        'C:\Program Files\Inno Setup 5\ISCC.exe',
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 5\ISCC.exe')
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -LiteralPath $candidatePath) {
            return $candidatePath
        }
    }

    $registryLocations = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )

    foreach ($registryLocation in $registryLocations) {
        $match = Get-ItemProperty -Path $registryLocation -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -like 'Inno Setup*' } |
            Select-Object -First 1

        if (-not $match) {
            continue
        }

        foreach ($basePath in @($match.InstallLocation, $match.DisplayIcon, $match.UninstallString)) {
            if ([string]::IsNullOrWhiteSpace($basePath)) {
                continue
            }

            $normalizedBasePath = $basePath.Trim('"')
            if ($normalizedBasePath.Contains(',')) {
                $normalizedBasePath = $normalizedBasePath.Split(',')[0].Trim()
            }

            if ($normalizedBasePath.EndsWith('ISCC.exe', [System.StringComparison]::OrdinalIgnoreCase) -and
                (Test-Path -LiteralPath $normalizedBasePath)) {
                return $normalizedBasePath
            }

            if (Test-Path -LiteralPath $normalizedBasePath -PathType Container) {
                $joinedPath = Join-Path $normalizedBasePath 'ISCC.exe'
                if (Test-Path -LiteralPath $joinedPath) {
                    return $joinedPath
                }
            }
            elseif (Test-Path -LiteralPath $normalizedBasePath -PathType Leaf) {
                $siblingCompiler = Join-Path (Split-Path -Parent $normalizedBasePath) 'ISCC.exe'
                if (Test-Path -LiteralPath $siblingCompiler) {
                    return $siblingCompiler
                }
            }
        }
    }

    return $null
}

$iscc = Resolve-InnoSetupCompiler
if (-not $iscc) {
    throw @"
Inno Setup nao foi encontrado nesta maquina.

Instale o compilador do Inno Setup e execute novamente.

Opcao via winget:
  winget install JRSoftware.InnoSetup

Depois rode:
  powershell -ExecutionPolicy Bypass -File "C:\Apextweaker\scripts\Build-Installer.ps1"
"@
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

& $iscc "/DReleaseDir=$($releaseSource.Dir)" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao compilar o instalador. Codigo de saida: $LASTEXITCODE"
}

Write-Host "Instalador gerado em: $outputDir"

