param(
    [string]$OutputZip = "ProjectSnapshot/AI_CONTEXT_EXPORT.zip",
    [int]$MaxTextAssetKB = 512
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$snapshotDir = Join-Path $repoRoot "ProjectSnapshot"
$tempDir = Join-Path $snapshotDir "_ai_context_export_tmp"
$resolvedZip = Join-Path $repoRoot $OutputZip
$maxTextAssetBytes = $MaxTextAssetKB * 1024
$included = New-Object System.Collections.Generic.List[string]
$skippedLarge = New-Object System.Collections.Generic.List[string]
$timestamp = Get-Date -Format s

function Copy-ExportFile {
    param([System.IO.FileInfo]$File)

    if ($null -eq $File -or !$File.Exists) {
        return
    }

    $relative = $File.FullName.Substring($repoRoot.Length + 1)
    $target = Join-Path $tempDir $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $File.FullName -Destination $target -Force
    $included.Add($relative.Replace("\", "/")) | Out-Null
}

function Include-IfExists {
    param([string]$Path)

    $fullPath = Join-Path $repoRoot $Path
    if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
        Copy-ExportFile (Get-Item -LiteralPath $fullPath)
    }
}

New-Item -ItemType Directory -Force -Path $snapshotDir | Out-Null
Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
Remove-Item -LiteralPath $resolvedZip -Force -ErrorAction SilentlyContinue

$codeExtensions = @(".cs", ".asmdef", ".asmref")
$smallTextExtensions = @(".asset", ".prefab", ".json", ".txt", ".md", ".uxml", ".uss", ".inputactions")
$excludedExtensions = @(".fbx", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".wav", ".mp3", ".ogg", ".mp4", ".mov", ".zip", ".dll", ".exe")

if (Test-Path "Assets/Game") {
    Get-ChildItem "Assets/Game" -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $extension = $_.Extension.ToLowerInvariant()
            if ($excludedExtensions -contains $extension) {
                return
            }

            if ($codeExtensions -contains $extension) {
                Copy-ExportFile $_
                return
            }

            if (($smallTextExtensions -contains $extension) -and $_.Length -le $maxTextAssetBytes) {
                Copy-ExportFile $_
                return
            }

            if (($smallTextExtensions -contains $extension) -and $_.Length -gt $maxTextAssetBytes) {
                $skippedLarge.Add($_.FullName.Substring($repoRoot.Length + 1).Replace("\", "/")) | Out-Null
            }
        }
}

if (Test-Path "ProjectSnapshot") {
    Get-ChildItem "ProjectSnapshot" -Recurse -File -Filter *.md |
        Sort-Object FullName |
        ForEach-Object { Copy-ExportFile $_ }
}

Include-IfExists "README.md"
Include-IfExists ".gitignore"
Include-IfExists "Packages/manifest.json"
Include-IfExists "Packages/packages-lock.json"
Include-IfExists "ProjectSettings/ProjectVersion.txt"
Include-IfExists "ProjectSettings/TagManager.asset"
Include-IfExists "ProjectSettings/InputManager.asset"

$manifestPath = Join-Path $tempDir "EXPORT_MANIFEST.txt"
$manifestLines = @(
    "Frontier Depths AI Context Export",
    "Generated: $timestamp",
    "Output: $OutputZip",
    "Included files: $($included.Count)",
    "",
    "Included allowlist:",
    "- Assets/Game code, asmdefs, asmrefs, and small text gameplay data",
    "- ProjectSnapshot markdown docs",
    "- Packages manifest/lock",
    "- Minimal ProjectSettings version/tag/input files",
    "- README.md and .gitignore when present",
    "",
    "Excluded by design:",
    "- Library, Logs, Temp, Obj, UserSettings, Builds, .git",
    "- generated zip exports and temp export folders",
    "- imported models, images, audio, video, binaries, and heavy third-party payloads",
    "",
    "Included paths:",
    $included
)

if ($skippedLarge.Count -gt 0) {
    $manifestLines += ""
    $manifestLines += "Skipped large text assets over ${MaxTextAssetKB}KB:"
    $manifestLines += $skippedLarge
}

$manifestLines | Set-Content -Encoding UTF8 $manifestPath

Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $resolvedZip -Force
$zipInfo = Get-Item -LiteralPath $resolvedZip
Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Wrote $OutputZip"
Write-Host "Timestamp: $timestamp"
Write-Host "Included files: $($included.Count)"
Write-Host ("Zip size: {0:N1} KB" -f ($zipInfo.Length / 1KB))
Write-Host "Excluded heavy folders: Library, Logs, Temp, Obj, UserSettings, Builds, .git"
if ($skippedLarge.Count -gt 0) {
    Write-Host "Skipped large text assets: $($skippedLarge.Count)"
}
