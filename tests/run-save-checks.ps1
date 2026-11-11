param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$checkOutput = Join-Path ([System.IO.Path]::GetTempPath()) ('dungeon-save-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkOutput | Out-Null
$monoRuntime = Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe'
$compiler = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources = @(
    'tests/SaveSystemChecks.cs',
    'Assets/Scripts/Scripts_Item/Inventory.cs',
    'Assets/Scripts/Scripts_Item/ItemData.cs',
    'Assets/Scripts/Scripts_Item/DeathRules.cs',
    'Assets/Scripts/Scripts_Interface/PlayerWallet.cs',
    'Assets/Scripts/Scripts_Quests/QuestManager.cs',
    'Assets/Scripts/Scripts_Quests/QuestData.cs',
    'Assets/Scripts/Scripts_Quests/QuestCatalog.cs',
    'Assets/Scripts/Scripts_Equipment/EquipSlotType.cs',
    'Assets/Scripts/Scripts_Equipment/UpgradeGrade.cs'
) | ForEach-Object { Join-Path $repoRoot $_ }
$sources += (Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/Scripts/Scripts_Save') -Filter '*.cs').FullName
$exePath = Join-Path $checkOutput 'SaveSystemChecks.exe'
& $monoRuntime $compiler /nologo /r:System.Runtime.Serialization.dll "/out:$exePath" $sources
if ($LASTEXITCODE -ne 0) { throw 'Save test compilation failed.' }
& $monoRuntime $exePath $checkOutput
if ($LASTEXITCODE -ne 0) { throw 'Save regression checks failed.' }
