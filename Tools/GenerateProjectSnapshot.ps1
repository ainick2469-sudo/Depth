param(
    [string]$OutputZip = "ProjectSnapshot/AI_CONTEXT_EXPORT.zip"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$snapshotDir = Join-Path $repoRoot "ProjectSnapshot"
$tempDir = Join-Path $snapshotDir "_export_tmp"
New-Item -ItemType Directory -Force -Path $snapshotDir | Out-Null

$latestCommit = (git log -1 --oneline) 2>$null
$recentCommits = (git log --oneline -12) 2>$null

@"
# Frontier Depths AI Context Snapshot

Latest commit: $latestCommit

Frontier Depths is a Unity dungeon-crawler prototype centered on a Gunslinger loop: town hub -> dungeon floor -> combat/rewards -> return/deeper dive.

Current design decisions:
- Gunslinger uses Health, Stamina, Focus, and loaded weapon chambers.
- Mana remains future support for caster classes.
- Basic reserve ammo is inactive; reload/chamber timing remains.
- Dungeon generation overhaul and death recovery are deferred gates.
"@ | Set-Content -Encoding UTF8 (Join-Path $snapshotDir "README_FOR_CHATGPT.md")

@"
# Recent Changes

$($recentCommits -join "`n")
"@ | Set-Content -Encoding UTF8 (Join-Path $snapshotDir "RECENT_CHANGES.md")

$runtimeFiles = Get-ChildItem "Assets/Game/Runtime" -Recurse -File -Filter *.cs |
    Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1) }
$runtimeFiles | Set-Content -Encoding UTF8 (Join-Path $snapshotDir "RUNTIME_FILE_INDEX.md")

$testFiles = Get-ChildItem "Assets/Game/Tests" -Recurse -File -Filter *.cs |
    Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1) }
$testFiles | Set-Content -Encoding UTF8 (Join-Path $snapshotDir "TEST_INDEX.md")

Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

Copy-Item -Path (Join-Path $snapshotDir "*.md") -Destination $tempDir -Force

$codeRoot = Join-Path $tempDir "Code"
New-Item -ItemType Directory -Force -Path $codeRoot | Out-Null
$includeRoots = @("Assets/Game/Runtime", "Assets/Game/Tests/EditMode")
foreach ($root in $includeRoots) {
    if (!(Test-Path $root)) { continue }
    Get-ChildItem $root -Recurse -File -Include *.cs,*.asmdef |
        ForEach-Object {
            $relative = $_.FullName.Substring($repoRoot.Length + 1)
            $target = Join-Path $codeRoot $relative
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
            Copy-Item $_.FullName $target -Force
        }
}

$resolvedZip = Join-Path $repoRoot $OutputZip
Remove-Item -LiteralPath $resolvedZip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $resolvedZip -Force
Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Wrote $OutputZip"

