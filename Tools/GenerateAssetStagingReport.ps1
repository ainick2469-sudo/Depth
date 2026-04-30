param(
    [string]$StagingProjectPath = "C:\Users\nickb\FrontierDepths_AssetStaging",
    [string]$OutputPath = "ProjectSnapshot/ASSET_STAGING_REPORT.md"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$assetsPath = Join-Path $StagingProjectPath "Assets"
if (!(Test-Path -LiteralPath $assetsPath -PathType Container)) {
    throw "Staging Assets folder was not found: $assetsPath"
}

function Format-Bytes {
    param([long]$Bytes)

    if ($Bytes -ge 1GB) {
        return ("{0:N2} GB" -f ($Bytes / 1GB))
    }

    if ($Bytes -ge 1MB) {
        return ("{0:N1} MB" -f ($Bytes / 1MB))
    }

    if ($Bytes -ge 1KB) {
        return ("{0:N1} KB" -f ($Bytes / 1KB))
    }

    return "$Bytes B"
}

function To-RelativeStagingPath {
    param([string]$FullName)
    return $FullName.Substring($StagingProjectPath.Length + 1).Replace("\", "/")
}

function Count-Extensions {
    param(
        [System.IO.FileInfo[]]$Files,
        [string[]]$Extensions
    )

    return @($Files | Where-Object { $Extensions -contains $_.Extension.ToLowerInvariant() }).Count
}

function Find-Candidates {
    param(
        [System.IO.FileInfo[]]$Files,
        [string]$Pattern,
        [int]$Limit = 30
    )

    return @($Files |
        Where-Object { $_.Extension.ToLowerInvariant() -in ".prefab", ".fbx", ".obj", ".blend", ".mat", ".unity", ".cs", ".shader", ".shadergraph" -and $_.FullName -match $Pattern } |
        Sort-Object FullName |
        Select-Object -First $Limit)
}

$allFiles = @(Get-ChildItem -LiteralPath $assetsPath -Recurse -File -ErrorAction SilentlyContinue)
$topFolders = @(Get-ChildItem -LiteralPath $assetsPath -Directory -ErrorAction SilentlyContinue | Sort-Object Name)
$scenes = @($allFiles | Where-Object { $_.Extension.ToLowerInvariant() -eq ".unity" } | Sort-Object FullName)
$scripts = @($allFiles | Where-Object { $_.Extension.ToLowerInvariant() -eq ".cs" } | Sort-Object FullName)
$shaders = @($allFiles | Where-Object { $_.Extension.ToLowerInvariant() -in ".shader", ".shadergraph" } | Sort-Object FullName)
$largest = @($allFiles | Sort-Object Length -Descending | Select-Object -First 25)

$folderRows = foreach ($folder in $topFolders) {
    $folderFiles = @(Get-ChildItem -LiteralPath $folder.FullName -Recurse -File -ErrorAction SilentlyContinue)
    [PSCustomObject]@{
        Folder = $folder.Name
        Files = $folderFiles.Count
        Prefabs = Count-Extensions $folderFiles @(".prefab")
        Models = Count-Extensions $folderFiles @(".fbx", ".obj", ".blend")
        Materials = Count-Extensions $folderFiles @(".mat")
        Textures = Count-Extensions $folderFiles @(".png", ".jpg", ".jpeg", ".tga", ".psd")
        Audio = Count-Extensions $folderFiles @(".wav", ".mp3", ".ogg")
        Scenes = Count-Extensions $folderFiles @(".unity")
        Scripts = Count-Extensions $folderFiles @(".cs")
        Shaders = Count-Extensions $folderFiles @(".shader", ".shadergraph")
        Size = (($folderFiles | Measure-Object Length -Sum).Sum)
    }
}

$candidateGroups = [ordered]@{
    "Town / Settlement" = "Smithy|Blacksmith|Forge|Tavern|Inn|Merchant|Shop|Bounty|Board|Sign|Road|Fence|Gate|House|Village|Stall|Lamp|Torch|Lantern|Barrel|Crate|Cart"
    "Dungeon / Labyrinth" = "Dungeon|Modular|Cave|Wall|Floor|Stair|Arch|Pillar|Door|Gate|Prison|Cage|Cell|Chest|Shrine|Trap|Boss|Torch|Lantern"
    "Overworld" = "Rock|Cliff|Cave|Tree|Grass|Road|Bridge|Camp|Ruin|Village|Terrain|Nature|Forest|Hill"
    "Weapons / Combat" = "Revolver|Rifle|Pistol|Gun|Knife|Dagger|Sword|Axe|Bow|Crossbow|Shield|Spear|Mace|Hammer|Weapon"
    "UI / HUD" = "Icon|Frame|Panel|Button|HUD|UI|Map|Minimap"
    "Risky / Deferred" = "Demo|Sample|Controller|Generation|Manager|Post|Lighting|HDRP|URP|Shader|Editor"
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Asset Staging Report") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- Generated: $(Get-Date -Format s)") | Out-Null
$lines.Add("- Staging project: $StagingProjectPath") | Out-Null
$lines.Add("- Assets scanned: $assetsPath") | Out-Null
$lines.Add("- Note: this report is read-only; it does not copy assets into FrontierDepths.") | Out-Null
$lines.Add("") | Out-Null

$lines.Add("## Summary Counts") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| Metric | Count |") | Out-Null
$lines.Add("| --- | ---: |") | Out-Null
$lines.Add("| Total files | $($allFiles.Count) |") | Out-Null
$lines.Add("| Prefabs | $(Count-Extensions $allFiles @(".prefab")) |") | Out-Null
$lines.Add("| Models (.fbx/.obj/.blend) | $(Count-Extensions $allFiles @(".fbx", ".obj", ".blend")) |") | Out-Null
$lines.Add("| Materials | $(Count-Extensions $allFiles @(".mat")) |") | Out-Null
$lines.Add("| Textures | $(Count-Extensions $allFiles @(".png", ".jpg", ".jpeg", ".tga", ".psd")) |") | Out-Null
$lines.Add("| Audio | $(Count-Extensions $allFiles @(".wav", ".mp3", ".ogg")) |") | Out-Null
$lines.Add("| Scenes | $($scenes.Count) |") | Out-Null
$lines.Add("| Scripts | $($scripts.Count) |") | Out-Null
$lines.Add("| Shaders / shader graphs | $($shaders.Count) |") | Out-Null
$lines.Add("") | Out-Null

$lines.Add("## Top-Level Asset Folders") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| Folder | Files | Prefabs | Models | Materials | Textures | Audio | Scenes | Scripts | Shaders | Size |") | Out-Null
$lines.Add("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |") | Out-Null
foreach ($row in $folderRows) {
    $lines.Add("| $($row.Folder) | $($row.Files) | $($row.Prefabs) | $($row.Models) | $($row.Materials) | $($row.Textures) | $($row.Audio) | $($row.Scenes) | $($row.Scripts) | $($row.Shaders) | $(Format-Bytes $row.Size) |") | Out-Null
}
$lines.Add("") | Out-Null

$lines.Add("## Largest Files") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| Path | Type | Size |") | Out-Null
$lines.Add("| --- | --- | ---: |") | Out-Null
foreach ($file in $largest) {
    $lines.Add("| $(To-RelativeStagingPath $file.FullName) | $($file.Extension) | $(Format-Bytes $file.Length) |") | Out-Null
}
$lines.Add("") | Out-Null

$lines.Add("## Demo Scenes Found") | Out-Null
$lines.Add("") | Out-Null
if ($scenes.Count -eq 0) {
    $lines.Add("- None found.") | Out-Null
} else {
    foreach ($scene in $scenes) {
        $lines.Add("- $(To-RelativeStagingPath $scene.FullName) ($(Format-Bytes $scene.Length))") | Out-Null
    }
}
$lines.Add("") | Out-Null

$lines.Add("## Runtime And Editor Scripts Found") | Out-Null
$lines.Add("") | Out-Null
if ($scripts.Count -eq 0) {
    $lines.Add("- None found.") | Out-Null
} else {
    foreach ($script in $scripts) {
        $kind = if ($script.FullName -match "\\Editor\\") { "Editor" } else { "Runtime" }
        $lines.Add("- ${kind}: $(To-RelativeStagingPath $script.FullName)") | Out-Null
    }
}
$lines.Add("") | Out-Null

$lines.Add("## Shader Dependencies Found") | Out-Null
$lines.Add("") | Out-Null
if ($shaders.Count -eq 0) {
    $lines.Add("- None found.") | Out-Null
} else {
    foreach ($shader in $shaders) {
        $lines.Add("- $(To-RelativeStagingPath $shader.FullName)") | Out-Null
    }
}
$lines.Add("") | Out-Null

$lines.Add("## Candidate Assets By Category") | Out-Null
foreach ($group in $candidateGroups.GetEnumerator()) {
    $lines.Add("") | Out-Null
    $lines.Add("### $($group.Key)") | Out-Null
    $matches = Find-Candidates $allFiles $group.Value 35
    if ($matches.Count -eq 0) {
        $lines.Add("- No obvious candidates found by keyword scan.") | Out-Null
    } else {
        foreach ($match in $matches) {
            $lines.Add("- $(To-RelativeStagingPath $match.FullName) ($($match.Extension), $(Format-Bytes $match.Length))") | Out-Null
        }
    }
}
$lines.Add("") | Out-Null

$lines.Add("## Obvious Risk Notes") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- `ADoorToGaming/Dungeon Generation` includes generator scripts and a sample scene; inspect as reference only and do not import into runtime blindly.") | Out-Null
$lines.Add("- `Caves and Dungeons` is audio-heavy; many files are 30-54 MB each and should not be committed without explicit audio selection/LFS policy.") | Out-Null
$lines.Add("- Demo scenes are useful for visual inspection in staging but should not be added to FrontierDepths Build Settings.") | Out-Null
$lines.Add("- Shader Graph/custom shader files may assume render-pipeline settings; wrap or replace materials during curated imports.") | Out-Null
$lines.Add("- Vendor prefabs/materials should remain isolated later under `Assets/ThirdParty/...`; game-owned prefabs should be wrappers or variants under `Assets/Game/Prefabs/...`.") | Out-Null

$resolvedOutput = Join-Path $repoRoot $OutputPath
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedOutput) | Out-Null
$lines | Set-Content -LiteralPath $resolvedOutput -Encoding UTF8

Write-Host "Wrote $OutputPath"
Write-Host "Staging project: $StagingProjectPath"
Write-Host "Top-level folders: $($topFolders.Count)"
Write-Host "Total files: $($allFiles.Count)"
Write-Host "Prefabs: $(Count-Extensions $allFiles @(".prefab"))"
Write-Host "Models: $(Count-Extensions $allFiles @(".fbx", ".obj", ".blend"))"
Write-Host "Materials: $(Count-Extensions $allFiles @(".mat"))"
Write-Host "Textures: $(Count-Extensions $allFiles @(".png", ".jpg", ".jpeg", ".tga", ".psd"))"
Write-Host "Audio: $(Count-Extensions $allFiles @(".wav", ".mp3", ".ogg"))"
Write-Host "Scenes: $($scenes.Count)"
Write-Host "Scripts: $($scripts.Count)"
Write-Host "Shaders: $($shaders.Count)"
