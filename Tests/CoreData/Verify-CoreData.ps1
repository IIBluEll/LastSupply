# PowerShell 7에서 실행합니다. Unity를 실행하거나 프로젝트 파일을 변경하지 않습니다.
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$coreFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\01_Main\02_Scripts\Core') -Recurse -Filter *.cs -File | Select-Object -ExpandProperty FullName)
Add-Type -Path $coreFiles
$script:checkCount = 0

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
    $script:checkCount++
}

function Assert-Rejected([scriptblock]$action, [type]$exceptionType) {
    try { & $action | Out-Null } catch {
        if ($_.Exception.GetBaseException() -is $exceptionType) {
            $script:checkCount++
            return
        }
        throw
    }
    throw ('Expected exception: ' + $exceptionType.Name)
}

function New-TestRules([hashtable]$changes = @{}) {
    $values = @{
        MaxStage = $game.Rules.MaxStage
        MaxNoise = $game.Rules.MaxNoise
        MaxDays = $game.Rules.MaxDays
        InitialResources = $game.Rules.InitialResources
        TargetParts = $game.Rules.TargetParts
        DailyFoodConsumption = $game.Rules.DailyFoodConsumption
        StarvationDamage = $game.Rules.StarvationDamage
        HealingMedicineCost = $game.Rules.HealingMedicineCost
        HealingAmount = $game.Rules.HealingAmount
        Rewards = [LastSupply.Core.SearchRewardDefinition[]]@($game.Rules.DefaultSearchRewards)
        Chances = [LastSupply.Core.StageEventChanceDefinition[]]@($game.Rules.StageEventChances)
        Risks = [LastSupply.Core.ReturnRiskDefinition[]]@($game.Rules.ReturnRisks)
        ReturnEvent = $game.Rules.ReturnEvent
    }
    foreach ($key in $changes.Keys) { $values[$key] = $changes[$key] }
    return [LastSupply.Core.GameRulesDefinition]::new(
        $values.MaxStage, $values.MaxNoise, $values.MaxDays, $values.InitialResources,
        $values.TargetParts, $values.DailyFoodConsumption, $values.StarvationDamage,
        $values.HealingMedicineCost, $values.HealingAmount, $values.Rewards,
        $values.Chances, $values.Risks, $values.ReturnEvent)
}

function Assert-Effect($effect, [int]$hp, [int]$noise, [int]$mainReward) {
    Assert-True ($effect.HpChange -eq $hp -and $effect.NoiseIncrease -eq $noise) 'Effect HP/noise differs from planning'
    if ($mainReward -eq 0) {
        Assert-True ($effect.Rewards.Count -eq 0) 'Unexpected resource reward'
    } else {
        Assert-True ($effect.Rewards.Count -eq 1 -and $effect.Rewards[0].Amount -eq $mainReward -and $null -eq $effect.Rewards[0].ResourceType) 'Main-resource reward differs from planning'
    }
}

$game = [LastSupply.Core.DefaultGameData]::CreateDefault()
$prototype = [LastSupply.Core.DefaultGameData]::CreatePrototype()
Assert-True ($game.Survivors.Count -eq 2 -and $game.Regions.Count -eq 3 -and $game.Events.Count -eq 5) 'Default content counts differ'
Assert-True ($prototype.Survivors.Count -eq 1 -and $prototype.Regions.Count -eq 1 -and $prototype.Events.Count -eq 3) 'Prototype content counts differ'
Assert-True ($prototype.Survivors[0].ID -eq 'SURVIVOR_MARCUS' -and $prototype.Regions[0].Id -eq 'REGION_WAREHOUSE') 'Prototype selection differs'
Assert-True ($game.Rules.MaxStage -eq 5 -and $game.Rules.MaxNoise -eq 6 -and $game.Rules.MaxDays -eq 7) 'Game limits differ'
Assert-True ($game.Rules.TargetParts -eq 8 -and $game.Rules.DailyFoodConsumption -eq 2 -and $game.Rules.StarvationDamage -eq 2) 'Survival settings differ'
Assert-True ($game.Rules.HealingMedicineCost -eq 1 -and $game.Rules.HealingAmount -eq 3) 'Healing settings differ'
Assert-True ($game.Rules.InitialResources.Food -eq 6 -and $game.Rules.InitialResources.Medicine -eq 1 -and $game.Rules.InitialResources.Parts -eq 0) 'Initial resources differ'
Assert-True ($game.Survivors[0].MaxHP -eq 10 -and $game.Survivors[0].CombatModifier -eq 4 -and $game.Survivors[0].TechModifier -eq 0) 'Marcus stats differ'
Assert-True ($game.Survivors[1].MaxHP -eq 8 -and $game.Survivors[1].CombatModifier -eq 1 -and $game.Survivors[1].TechModifier -eq 4) 'Sara stats differ'

$chanceRows = @(@(65,20,15), @(55,20,25), @(45,20,35), @(35,20,45), @(25,20,55))
for ($i = 0; $i -lt 5; $i++) {
    $actual = $game.Rules.StageEventChances[$i]
    Assert-True ($actual.Stage -eq $i + 1 -and $actual.NoEventChance -eq $chanceRows[$i][0] -and $actual.OpportunityChance -eq $chanceRows[$i][1] -and $actual.DangerChance -eq $chanceRows[$i][2]) 'Stage probability differs'
}

$riskChances = @(0,0,0,30,50,75,100)
$riskDamages = @(0,0,0,2,2,3,3)
for ($noise = 0; $noise -le 6; $noise++) {
    $matches = @($game.Rules.ReturnRisks | Where-Object { $_.MinNoise -le $noise -and $_.MaxNoise -ge $noise })
    Assert-True ($matches.Count -eq 1 -and $matches[0].EventChance -eq $riskChances[$noise] -and $matches[0].FailureDamage -eq $riskDamages[$noise]) 'Return risk differs'
}
Assert-True ($game.Rules.ReturnEvent.Id -eq 'RETURN_001' -and $game.Rules.ReturnEvent.StatType -eq [LastSupply.Core.STAT_TYPE]::COMBAT -and $game.Rules.ReturnEvent.Difficulty -eq 12) 'Return action differs'
Assert-True (@($game.Events | Where-Object Id -eq 'RETURN_001').Count -eq 0) 'Return event appears in normal events'

foreach ($region in $game.Regions) {
    Assert-True ($null -eq $region.SearchRewards) 'Default content unexpectedly overrides search table'
    for ($stage = 1; $stage -le 5; $stage++) {
        for ($dice = 1; $dice -le 20; $dice++) {
            $expected = if ($dice -le 6) {0} elseif ($dice -le 15) {1} elseif ($dice -le 19) {if ($stage -le 2) {2} elseif ($stage -le 4) {3} else {4}} else {if ($stage -le 2) {3} elseif ($stage -le 4) {4} else {5}}
            $actual = [LastSupply.Core.ExplorationRules]::CalculateSearchReward($stage, $dice, $region.MainResource, $game.Rules.DefaultSearchRewards, $region.SearchRewards)
            $food = if ($region.MainResource -eq [LastSupply.Core.RESOURCE_TYPE]::FOOD) {$expected} else {0}
            $medicine = if ($region.MainResource -eq [LastSupply.Core.RESOURCE_TYPE]::MEDICINE) {$expected} else {0}
            $parts = if ($region.MainResource -eq [LastSupply.Core.RESOURCE_TYPE]::PARTS) {$expected} else {0}
            Assert-True ($actual.Food -eq $food -and $actual.Medicine -eq $medicine -and $actual.Parts -eq $parts) 'Search table differs from planning'
        }
    }
}
Assert-True (($game.Regions[0].EventIds -join ',') -eq 'EVENT_001,EVENT_002,EVENT_003,EVENT_005') 'Mart events differ'
Assert-True (($game.Regions[1].EventIds -join ',') -eq 'EVENT_001,EVENT_002,EVENT_003,EVENT_004,EVENT_005') 'Hospital events differ'
Assert-True (($game.Regions[2].EventIds -join ',') -eq 'EVENT_001,EVENT_002,EVENT_003,EVENT_004,EVENT_005') 'Warehouse events differ'

for ($i = 0; $i -lt 5; $i++) {
    Assert-True ($game.Events[$i].Id -eq ('EVENT_00' + ($i + 1)) -and $game.Events[$i].MinStage -eq 1 -and $game.Events[$i].MaxStage -eq 5) 'Event ID/stage differs'
}
$unlock = $game.Events[0].Actions[0]
$force = $game.Events[0].Actions[1]
$pass = $game.Events[0].Actions[2]
Assert-True ($game.Events[0].Category -eq [LastSupply.Core.EVENT_CATEGORY]::OPPORTUNITY -and $game.Events[0].Mode -eq [LastSupply.Core.EVENT_MODE]::CHOICE -and $game.Events[0].Actions.Count -eq 3) 'Locker event differs'
Assert-True ($unlock.StatType -eq [LastSupply.Core.STAT_TYPE]::TECH -and $unlock.Difficulty -eq 14) 'Unlock check differs'
Assert-Effect $unlock.Effect 0 0 2
Assert-Effect $unlock.FailureEffect 0 1 0
Assert-True ($force.StatType -eq [LastSupply.Core.STAT_TYPE]::COMBAT -and $force.Difficulty -eq 11) 'Force-open check differs'
Assert-Effect $force.Effect 0 2 2
Assert-Effect $force.FailureEffect -2 2 0
Assert-True (-not $pass.RequiresCheck -and $null -eq $pass.FailureEffect) 'Pass action unexpectedly rolls'
Assert-Effect $pass.Effect 0 0 0
$zombie = $game.Events[1].Actions[0]
Assert-True ($game.Events[1].Category -eq [LastSupply.Core.EVENT_CATEGORY]::DANGER -and $game.Events[1].Mode -eq [LastSupply.Core.EVENT_MODE]::CHECK -and $game.Events[1].Actions.Count -eq 1 -and $zombie.StatType -eq [LastSupply.Core.STAT_TYPE]::COMBAT -and $zombie.Difficulty -eq 12) 'Zombie event differs'
Assert-Effect $zombie.Effect 0 1 0
Assert-Effect $zombie.FailureEffect -3 2 0
Assert-True ($game.Events[2].Mode -eq [LastSupply.Core.EVENT_MODE]::INSTANT -and $game.Events[2].Actions.Count -eq 0) 'Supplies event differs'
Assert-Effect $game.Events[2].InstantEffect 0 0 1
$gate = $game.Events[3].Actions[0]
Assert-True ($game.Events[3].Category -eq [LastSupply.Core.EVENT_CATEGORY]::DANGER -and $gate.StatType -eq [LastSupply.Core.STAT_TYPE]::TECH -and $gate.Difficulty -eq 12) 'Gate event differs'
Assert-Effect $gate.Effect 0 0 0
Assert-Effect $gate.FailureEffect -2 1 0
$shelf = $game.Events[4].Actions[0]
Assert-True ($game.Events[4].Mode -eq [LastSupply.Core.EVENT_MODE]::CHOICE -and $game.Events[4].Actions.Count -eq 2 -and $shelf.RequiresCheck -and $shelf.StatType -eq [LastSupply.Core.STAT_TYPE]::NONE -and $shelf.Difficulty -eq 13) 'Shelf event differs'
Assert-Effect $shelf.Effect 0 0 3
Assert-Effect $shelf.FailureEffect -1 2 0

Assert-Rejected { [LastSupply.Core.StageEventChanceDefinition]::new(1, 65, 20, 14) } ([ArgumentException])
Assert-Rejected { [LastSupply.Core.StageEventChanceDefinition]::new(1, -1, 20, 81) } ([ArgumentOutOfRangeException])
Assert-Rejected { [LastSupply.Core.ReturnRiskDefinition]::new(0, 2, 'Risk', 101, 2) } ([ArgumentOutOfRangeException])
Assert-Rejected { [LastSupply.Core.ReturnRiskDefinition]::new(0, 2, 'Risk', 50, -1) } ([ArgumentOutOfRangeException])
Assert-Rejected { New-TestRules @{ MaxDays = 0 } } ([ArgumentOutOfRangeException])
Assert-Rejected { New-TestRules @{ TargetParts = 0 } } ([ArgumentOutOfRangeException])
Assert-Rejected { New-TestRules @{ HealingMedicineCost = 0 } } ([ArgumentOutOfRangeException])
Assert-Rejected { New-TestRules @{ Rewards = [LastSupply.Core.SearchRewardDefinition[]]@() } } ([ArgumentException])
Assert-Rejected { New-TestRules @{ Rewards = [LastSupply.Core.SearchRewardDefinition[]]@($game.Rules.DefaultSearchRewards) + @($game.Rules.DefaultSearchRewards[0]) } } ([ArgumentException])
Assert-Rejected { New-TestRules @{ Chances = [LastSupply.Core.StageEventChanceDefinition[]]@($game.Rules.StageEventChances[0..3]) } } ([ArgumentException])
Assert-Rejected { New-TestRules @{ Chances = [LastSupply.Core.StageEventChanceDefinition[]]@($game.Rules.StageEventChances) + @($game.Rules.StageEventChances[0]) } } ([ArgumentException])
Assert-Rejected { New-TestRules @{ Risks = [LastSupply.Core.ReturnRiskDefinition[]]@($game.Rules.ReturnRisks[0..3]) } } ([ArgumentException])
Assert-Rejected { New-TestRules @{ Risks = [LastSupply.Core.ReturnRiskDefinition[]]@($game.Rules.ReturnRisks) + @($game.Rules.ReturnRisks[0]) } } ([ArgumentException])
Assert-Rejected { New-TestRules @{ ReturnEvent = $null } } ([ArgumentNullException])
$survivors = [LastSupply.Core.SurvivorDefinition[]]@($prototype.Survivors)
$regions = [LastSupply.Core.RegionDefinition[]]@($prototype.Regions)
$events = [LastSupply.Core.EventDefinition[]]@($prototype.Events)
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors + $survivors, $regions, $events) } ([ArgumentException])
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, $regions + $regions, $events) } ([ArgumentException])
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, $regions, $events + @($events[0])) } ([ArgumentException])
$badRegion = [LastSupply.Core.RegionDefinition]::new('BAD', '잘못된 참조', [LastSupply.Core.RESOURCE_TYPE]::PARTS, [string[]]@('MISSING'))
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, [LastSupply.Core.RegionDefinition[]]@($badRegion), $events) } ([ArgumentException])
$noDanger = [LastSupply.Core.RegionDefinition]::new('NO_DANGER', '위험 후보 없음', [LastSupply.Core.RESOURCE_TYPE]::PARTS, [string[]]@('EVENT_001', 'EVENT_003'))
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, [LastSupply.Core.RegionDefinition[]]@($noDanger), $events) } ([ArgumentException])
$incomplete = [LastSupply.Core.RegionDefinition]::new('EMPTY_TABLE', '빈 지역표', [LastSupply.Core.RESOURCE_TYPE]::PARTS, [string[]]@('EVENT_001', 'EVENT_002', 'EVENT_003'), [LastSupply.Core.SearchRewardDefinition[]]@())
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, [LastSupply.Core.RegionDefinition[]]@($incomplete), $events) } ([ArgumentException])
$returnAsNormal = [LastSupply.Core.EventDefinition]::new('RETURN_001', '잘못된 귀환 사건', '설명', [LastSupply.Core.EVENT_CATEGORY]::OPPORTUNITY, [LastSupply.Core.EVENT_MODE]::INSTANT, 1, 5, $game.Events[2].InstantEffect, [LastSupply.Core.EventActionDefinition[]]@())
Assert-Rejected { [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, $regions, $events + @($returnAsNormal)) } ([ArgumentException])

# 입력 배열을 바꿔도 현재 판의 정의가 바뀌지 않아야 합니다.
$copy = [LastSupply.Core.GameDefinition]::new($prototype.Rules, $survivors, $regions, $events)
$survivors[0] = $null
$regions[0] = $null
$events[0] = $null
Assert-True ($null -ne $copy.Survivors[0] -and $null -ne $copy.Regions[0] -and $null -ne $copy.Events[0]) 'Catalog arrays were not copied'
Assert-Rejected { ([Collections.IList]$copy.Survivors).set_Item(0, $null) } ([NotSupportedException])
$sourceChances = [LastSupply.Core.StageEventChanceDefinition[]]@($game.Rules.StageEventChances)
$sourceRisks = [LastSupply.Core.ReturnRiskDefinition[]]@($game.Rules.ReturnRisks)
$sourceRewards = [LastSupply.Core.SearchRewardDefinition[]]@($game.Rules.DefaultSearchRewards)
$copiedRules = New-TestRules @{ Chances = $sourceChances; Risks = $sourceRisks; Rewards = $sourceRewards }
$sourceChances[0] = $null
$sourceRisks[0] = $null
$sourceRewards[0] = $null
Assert-True ($null -ne $copiedRules.StageEventChances[0] -and $null -ne $copiedRules.ReturnRisks[0] -and $null -ne $copiedRules.DefaultSearchRewards[0]) 'Rules arrays were not copied'

# 개발용 혼합 보상은 지역표 전체 교체와 같은 자원 합산을 확인하는 데만 사용합니다.
$mixedRow = [LastSupply.Core.SearchRewardDefinition]::new(1, 5, 1, 20, [LastSupply.Core.SearchRewardItemDefinition[]]@(
    [LastSupply.Core.SearchRewardItemDefinition]::new(1),
    [LastSupply.Core.SearchRewardItemDefinition]::new(1, [LastSupply.Core.RESOURCE_TYPE]::FOOD),
    [LastSupply.Core.SearchRewardItemDefinition]::new(1, [LastSupply.Core.RESOURCE_TYPE]::MEDICINE)))
$mixedRegion = [LastSupply.Core.RegionDefinition]::new('MIXED', '검사용 혼합 보상', [LastSupply.Core.RESOURCE_TYPE]::FOOD, [string[]]@('EVENT_001', 'EVENT_002', 'EVENT_003'), [LastSupply.Core.SearchRewardDefinition[]]@($mixedRow))
$mixedGame = [LastSupply.Core.GameDefinition]::new($prototype.Rules, [LastSupply.Core.SurvivorDefinition[]]@($prototype.Survivors), [LastSupply.Core.RegionDefinition[]]@($mixedRegion), [LastSupply.Core.EventDefinition[]]@($prototype.Events))
$mixedReward = [LastSupply.Core.ExplorationRules]::CalculateSearchReward(1, 1, $mixedRegion.MainResource, $mixedGame.Rules.DefaultSearchRewards, $mixedRegion.SearchRewards)
Assert-True ($mixedReward.Food -eq 2 -and $mixedReward.Medicine -eq 1 -and $mixedReward.Parts -eq 0) 'Mixed reward merge/replacement failed'
$fresh = [LastSupply.Core.DefaultGameData]::CreateDefault()
Assert-True (-not [object]::ReferenceEquals($fresh, $game) -and -not [object]::ReferenceEquals($fresh.Rules, $game.Rules)) 'Data factory unexpectedly shares a mutable catalog lifetime'

Write-Output ('Core compilation passed: ' + $coreFiles.Count + ' files')
Write-Output ('Core data checks passed: ' + $script:checkCount + ' assertions (including 300 search combinations)')
