param(
    [string]$OutputZip = "ProjectSnapshot/AI_CONTEXT_EXPORT.zip",
    [switch]$RefreshGeneratedIndexes
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$snapshotDir = Join-Path $repoRoot "ProjectSnapshot"
New-Item -ItemType Directory -Force -Path $snapshotDir | Out-Null

if ($RefreshGeneratedIndexes) {
    $runtimeIndexPath = Join-Path $snapshotDir "RUNTIME_FILE_INDEX.md"
    $testIndexPath = Join-Path $snapshotDir "TEST_INDEX.md"

    $runtimeFiles = Get-ChildItem "Assets/Game/Runtime" -Recurse -File -Filter *.cs |
        Sort-Object FullName |
        ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1).Replace("\", "/") }

    @(
        "# Runtime File Index",
        "",
        $runtimeFiles
    ) | Set-Content -Encoding UTF8 $runtimeIndexPath

    $testFiles = Get-ChildItem "Assets/Game/Tests" -Recurse -File -Filter *.cs |
        Sort-Object FullName |
        ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1).Replace("\", "/") }

    @(
        "# Test File Index",
        "",
        $testFiles
    ) | Set-Content -Encoding UTF8 $testIndexPath

    Write-Host "Refreshed generated file indexes."
}
else {
    Write-Host "Skipping generated index refresh. Pass -RefreshGeneratedIndexes to update RUNTIME_FILE_INDEX.md and TEST_INDEX.md."
}

$exportScript = Join-Path $PSScriptRoot "GenerateChatReviewExport.ps1"
& $exportScript -OutputZip $OutputZip
