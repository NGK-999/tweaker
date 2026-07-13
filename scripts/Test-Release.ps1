param(
    [string]$ExePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'release-v2\ApexTweaker.exe'),
    [string]$ExpectedVersion = '3.3.0'
)

$ErrorActionPreference = 'Stop'

$exe = (Resolve-Path -LiteralPath $ExePath).Path
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if ($version.FileVersion -ne "$ExpectedVersion.0" -or $version.ProductVersion -ne $ExpectedVersion) {
    throw "Versao inesperada: File=$($version.FileVersion), Product=$($version.ProductVersion)"
}

$kitsRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$mt = Get-ChildItem -LiteralPath $kitsRoot -Filter mt.exe -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -match '\\x64$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not $mt) {
    throw 'mt.exe x64 nao encontrado no Windows SDK.'
}

$testRoot = Join-Path $env:TEMP ('ApexTweaker-ReleaseTest-' + [Guid]::NewGuid().ToString('N'))
$testRootFull = [IO.Path]::GetFullPath($testRoot)
$tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
if (-not $testRootFull.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Raiz temporaria insegura: $testRootFull"
}

New-Item -ItemType Directory -Path $testRootFull | Out-Null
try {
    $manifestPath = Join-Path $testRootFull 'ApexTweaker.manifest.xml'
    & $mt.FullName -nologo "-inputresource:$exe;#1" "-out:$manifestPath"
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $manifestPath)) {
        throw 'Nao foi possivel extrair o manifesto do executavel.'
    }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath
    if ($manifest -notmatch 'requestedExecutionLevel\s+level="asInvoker"' -or
        $manifest -match 'requireAdministrator') {
        throw 'O executavel publicado nao usa privilegio minimo (asInvoker).'
    }

    $statusPath = Join-Path $testRootFull 'self-test.txt'
    $process = Start-Process -FilePath $exe `
        -ArgumentList @('--minecraft-self-test', '--status-file', ('"{0}"' -f $statusPath)) `
        -WindowStyle Hidden `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $statusPath)) {
        throw "Self-test do executavel falhou com codigo $($process.ExitCode)."
    }

    $status = Get-Content -Raw -LiteralPath $statusPath
    if ($status -notmatch 'SELF_TEST_OK') {
        throw 'Self-test do executavel nao confirmou SELF_TEST_OK.'
    }

    $hash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host "RELEASE_TEST_OK"
    Write-Host "VERSION=$ExpectedVersion"
    Write-Host "MANIFEST=asInvoker"
    Write-Host "SELF_TEST=OK"
    Write-Host "SHA256=$hash"
}
finally {
    if ([IO.Directory]::Exists($testRootFull)) {
        Remove-Item -LiteralPath $testRootFull -Recurse -Force
    }
}
