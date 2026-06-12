param(
    [string]$RepoName = "ApexTweaker",
    [ValidateSet("private", "public")]
    [string]$Visibility = "private",
    [string]$CommitMessage = "Initial ApexTweaker release"
)

$ErrorActionPreference = "Stop"

function Assert-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Comando '$Name' nao encontrado. Instale Git e GitHub CLI antes de continuar."
    }
}

Assert-Command git
Assert-Command gh

$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "GitHub CLI ainda nao esta autenticado. Abrindo login..."
    gh auth login
}

if (-not (Test-Path ".git")) {
    git init
}

git add .

$hasCommit = $true
git rev-parse --verify HEAD *> $null
if ($LASTEXITCODE -ne 0) {
    $hasCommit = $false
}

$hasChanges = $false
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    $hasChanges = $true
}

if (-not $hasCommit -or $hasChanges) {
    git commit -m $CommitMessage
}

$remote = git remote get-url origin 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($remote)) {
    $visibilityFlag = "--$Visibility"
    gh repo create $RepoName $visibilityFlag --source . --remote origin --push
}
else {
    git push -u origin HEAD
}

Write-Host "Repositorio GitHub pronto: $RepoName ($Visibility)"
