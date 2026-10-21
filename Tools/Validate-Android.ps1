param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.0.49f1/Editor')
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

function Assert($condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Read-Scene([string]$path) {
    $objects = @{}
    $content = [IO.File]::ReadAllText((Join-Path $pwd $path))
    foreach ($match in [regex]::Matches($content, '(?ms)^--- !u!(\d+) &(-?\d+)\r?\n(.*?)(?=^--- !u!|\z)')) {
        $id = $match.Groups[2].Value
        Assert (-not $objects.ContainsKey($id)) "Duplicate fileID $id in $path"
        $objects[$id] = @{ Type = [int]$match.Groups[1].Value; Text = $match.Groups[3].Value }
    }
    foreach ($obj in $objects.Values) {
        foreach ($reference in [regex]::Matches($obj.Text, '\{fileID: (-?\d+)\}')) {
            $id = $reference.Groups[1].Value
            Assert ($id -eq '0' -or $objects.ContainsKey($id)) "Missing local reference $id in $path"
        }
    }
    Write-Output -NoEnumerate $objects
}

$main = Read-Scene 'Assets/Scenes/MainMenu.unity'
$city = Read-Scene 'Assets/Scenes/CityScene.unity'
Assert ($city['519420031'].Type -eq 20) 'Camera reference must point to a Camera'
Assert ($city['825425783'].Text -match 'gameObjectScript: \{fileID: 825425781\}') 'Transformation manager reference'
Assert ($city['825425781'].Text -match 'cb3c8abd16163a24d8105447183d58e6') 'Vehicle manager reference type'
Assert ($city['497691516'].Text -match 'resultCanvas: \{fileID: 1451266239\}') 'Result canvas reference'
Assert ($city['1451266240'].Text -match '1329865010') 'WinPanel must belong to the referenced result canvas'
$actions = @($city.Values | Where-Object { $_.Text -match '(?m)^  action: [0-6]\r?$' })
Assert ($actions.Count -eq 7) 'Expected all seven existing transformation controls'
$dragGuid = [regex]::Match([IO.File]::ReadAllText((Join-Path $pwd 'Assets/Scripts/DragAndDropScript.cs.meta')), 'guid: (\w+)').Groups[1].Value
$dropGuid = [regex]::Match([IO.File]::ReadAllText((Join-Path $pwd 'Assets/Scripts/DropPlaceScript.cs.meta')), 'guid: (\w+)').Groups[1].Value
Assert (@($city.Values | Where-Object { $_.Text -match $dragGuid }).Count -eq 12) 'Expected 12 draggable vehicles'
Assert (@($city.Values | Where-Object { $_.Text -match $dropGuid }).Count -eq 12) 'Expected 12 drop zones'

# Validate persistent callbacks against their actual script source, not only names.
$scripts = @{}
Get-ChildItem Assets -Recurse -Filter '*.cs.meta' | ForEach-Object {
    $guid = [regex]::Match([IO.File]::ReadAllText($_.FullName), 'guid: (\w+)').Groups[1].Value
    $scripts[$guid] = [IO.File]::ReadAllText($_.FullName.Substring(0, $_.FullName.Length - 5))
}
foreach ($scene in @($main, $city)) {
    foreach ($obj in $scene.Values) {
        foreach ($call in [regex]::Matches($obj.Text, '(?s)m_Target: \{fileID: (\d+)\}\s+m_TargetAssemblyTypeName: ([^\r\n]+)\s+m_MethodName: (\w+)')) {
            $target = $scene[$call.Groups[1].Value]
            Assert ($null -ne $target) 'Missing callback target'
            $guid = [regex]::Match($target.Text, 'm_Script: \{fileID: 11500000, guid: (\w+)').Groups[1].Value
            if ($scripts.ContainsKey($guid)) {
                $method = $call.Groups[3].Value
                Assert ($scripts[$guid] -match ('\b' + $method + '\s*\(')) "Missing callback method $method"
            }
        }
    }
}
Assert ($city['727290739'].Text -match 'fileID: 21300002, guid: 333005c8837b85f4a8049ab8c99836e2') 'Timer must use the board-only sprite'
Assert ($city['727290739'].Text -match 'm_PreserveAspect: 1') 'Timer must preserve aspect'
Assert ($city['1024653707'].Text -match 'm_TextWrappingMode: 0') 'Timer must remain on one line'
$timerMeta = [IO.File]::ReadAllText((Join-Path $pwd 'Assets/Sprites/Izkartne.png.meta'))
$timerSprite = [regex]::Match($timerMeta, '(?ms)^    - serializedVersion: 2\r?\n      name: TimerPanel_0.*?(?=^    - |^    outline:)').Value
Assert ($timerSprite -match 'internalID: 21300002' -and $timerSprite -match 'height: 373') 'Board sprite import entry missing'
Assert ($timerMeta -match 'internalID: 8135142445426999207') 'Original billboard sprite must remain available'
$landscapePositions = @{
    '401480987' = '96, y: 96'; '2129851457' = '252, y: 96';
    '65039167' = '-96, y: 408'; '923455407' = '-408, y: 252';
    '497119398' = '-252, y: 252'; '1639621303' = '-96, y: 252'; '814247982' = '-408, y: 96'
}
foreach ($id in $landscapePositions.Keys) {
    Assert ($city[$id].Text.Contains('m_AnchoredPosition: {x: ' + $landscapePositions[$id] + '}')) "Unexpected landscape position for $id"
}
$uiSource = [IO.File]::ReadAllText((Join-Path $pwd 'Assets/Scripts/AdaptiveOrientationUI.cs'))
Assert ($uiSource -notmatch 'ZoomPlus|ZoomMinus|ZoomIn|ZoomOut') 'UI must not create zoom controls'
foreach ($obj in $city.Values) { Assert ($obj.Text -notmatch 'm_Name: Zoom(Plus|Minus)') 'Scene must not contain zoom buttons' }
Write-Host 'PASS: scene references, timer sprite, no zoom buttons, 12 cars, 12 zones, 7 actions and callbacks'

# Geometric checks only. These do not execute Unity layout or render frames.
foreach ($resolution in @(@(1080,2400), @(2400,1080), @(720,1600), @(1600,720), @(1080,1920), @(1920,1080))) {
    $w = $resolution[0]; $h = $resolution[1]; $portrait = $h -gt $w
    foreach ($inset in @(0, 96)) {
        $scale = if ($portrait) { $w / 1080.0 } else { $h / 1080.0 }
        $sw = ($w - $inset) / $scale; $sh = ($h - $inset) / $scale
        $size = if ($portrait) { [Math]::Min(156, [Math]::Min(($sw - 96) / 4, ($sh - 608) / 5)) } else {
            [Math]::Min(144, [Math]::Min(($sh - 230) / 3, ($sw - 96) / 5))
        }
        if ($portrait) {
            $centerY = ($sh - 480 + 24) / 2
            $rightHeight = 5 * $size + 48
            Assert ($centerY - $rightHeight / 2 -ge 24) 'Right column below Safe Area'
            Assert ($centerY + $rightHeight / 2 -lt $sh - 464) 'Right column overlaps HUD/ads'
            Assert (2 * ($size + 24) -lt $sw) 'Side columns overlap'
        }
        else {
            Assert (3 * $size + 48 -lt $sh - 276) 'Controls overlap HUD/ads'
            Assert (5 * $size + 36 + 48 -lt $sw) 'Left and right controls overlap'
            Assert (345 -lt $sw / 2 - 162.5) 'Menu overlaps timer'
            Assert ($sw / 2 + 162.5 -lt $sw - 345) 'Timer overlaps lives'
        }
        $timerWidth = [Math]::Min($(if ($portrait) {380} else {325}), $sw * .44)
        $timerHeight = $timerWidth * 373 / 844
        $timerY = if ($portrait) {254} else {90}
        Assert ($timerY - $timerHeight / 2 -ge 0) 'Timer outside Safe Area'
        if ($portrait) {
            Assert ($timerY - $timerHeight / 2 -gt 154) 'Timer overlaps top HUD row'
            Assert ($timerY + $timerHeight / 2 -lt 352) 'Timer overlaps ads row'
        }
        Assert ($size * $scale -ge 90) 'Transformation touch target smaller than 90 physical pixels'
        $windowWidth = if ($portrait) { 825 } else { 900 }
        $windowHeight = if ($portrait) { 1040 } else { 630 }
        $fit = [Math]::Min(1, [Math]::Min(($sw - 48) / $windowWidth, ($sh - 48) / $windowHeight))
        Assert ($windowWidth * $fit -le $sw -and $windowHeight * $fit -le $sh) 'Result window outside safe area'
    }
    Write-Host "PASS: calculated bounds ${w}x${h}, full screen and synthetic 96px insets"
}

$project = [xml](Get-Content Assembly-CSharp.csproj)
$references = @($project.Project.ItemGroup.Reference.HintPath | Where-Object { $_ })
foreach ($reference in $references) { Assert (Test-Path $reference) "Missing compilation reference: $reference" }
New-Item -ItemType Directory -Force Temp | Out-Null
foreach ($mode in @('Editor', 'Player')) {
    $defines = if ($mode -eq 'Editor') { 'UNITY_EDITOR,UNITY_ANDROID,UNITY_6000_0_OR_NEWER' } else { 'UNITY_ANDROID,UNITY_6000_0_OR_NEWER' }
    $response = @('/nologo', '/target:library', '/langversion:9.0', '/nostdlib+', "/out:Temp/AndroidAudit$mode.dll", "/define:$defines")
    $response += $references | ForEach-Object { '/reference:"' + $_ + '"' }
    $response += Get-ChildItem Assets -Recurse -Filter '*.cs' | Where-Object { $mode -eq 'Editor' -or $_.FullName -notmatch '[\\/]Editor[\\/]' } | ForEach-Object { '"' + $_.FullName + '"' }
    $responsePath = Join-Path $pwd "Temp/android-audit-$mode.rsp"
    [IO.File]::WriteAllLines($responsePath, $response)
    & "$UnityEditor/Data/MonoBleedingEdge/bin/mono.exe" "$UnityEditor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe" "@$responsePath"
    Assert ($LASTEXITCODE -eq 0) "$mode compilation failed"
    Write-Host "PASS: $mode conditional C# compilation against installed Unity assemblies"
}
