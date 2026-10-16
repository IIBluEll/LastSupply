# LAST SUPPLY Core 스크립트 설명서

> **2026-10-16 추가:** 아래는 기존 Core 23개 파일의 설명이다. 이후 추가한 GameSession·Snapshot·정산 규칙과 기록·종료 사유의 최신 API 및 검사는 [GameSession 사용 안내](LAST_SUPPLY_GameSession_사용_안내.md)를 따른다.

> 작성일: 2026-10-15
>
> 기준: `Assets/01_Main/02_Scripts/Core`에 있는 실제 C# 스크립트 23개.
>
> 목적: 각 파일이 왜 필요한지, 무엇을 보관하거나 계산하는지, 다음 로직에서 어떻게 연결할지 설명한다.

## 1. 먼저 알아둘 구분

| 구분 | 의미 | 예시 |
|---|---|---|
| 정의(Definition) | 현재 판에서 사용할 고정 설정 | 생존자의 최대 HP, 사건의 DC |
| 상태(State) | 플레이하면서 달라지는 현재 값 | 현재 HP, 탐색 단계, 임시 전리품 |
| 규칙(Rules) | 주어진 입력으로 결과를 계산 | D20에 따른 수색 보상 |
| 난수(Random) | 판정에 사용할 정수를 제공 | D20의 1~20 |
| 기본 데이터(Data) | 정의에 실제 기획 수치를 넣어 구성 | 마커스, 창고, EVENT_001 |

Core 전체의 네임스페이스는 `LastSupply.Core`다. 현재 UnityEngine·UniTask·HM CodeBase 의존 없이 구성되어 있으며, MonoBehaviour나 Scene 배치가 필요한 클래스는 없다.

현재 구현된 것은 **정의 데이터, 기본 상태, 수색 보상·행동 성공 계산, 난수 경계**다. 이들을 한 번의 플레이로 연결하는 GameSession은 아직 없다.

## 2. 폴더와 파일 찾기

```text
Core/
├─ Data/
│  └─ DefaultGameData.cs
├─ Definitions/
│  ├─ GameDefinition.cs
│  ├─ GameRulesDefinition.cs
│  ├─ SurvivorDefinition.cs
│  ├─ RegionDefinition.cs
│  ├─ EventDefinition.cs
│  ├─ EventActionDefinition.cs
│  ├─ EventEffectDefinition.cs
│  ├─ SearchRewardDefinition.cs
│  ├─ SearchRewardItemDefinition.cs
│  ├─ StageEventChanceDefinition.cs
│  ├─ ReturnRiskDefinition.cs
│  └─ ReturnEventDefinition.cs
├─ Enums/
│  ├─ RESOURCE_TYPE.cs
│  ├─ STAT_TYPE.cs
│  ├─ EVENT_CATEGORY.cs
│  └─ EVENT_MODE.cs
├─ States/
│  ├─ SurvivorState.cs
│  └─ ExpeditionState.cs
├─ Resources/
│  └─ ResourceAmount.cs
├─ Rules/
│  └─ ExplorationRules.cs
└─ Random/
   ├─ IRandomSource.cs
   └─ GameRandom.cs
```

처음 읽을 때는 DefaultGameData → GameDefinition → GameRulesDefinition → 사건·보상 정의 → 상태 → 규칙·난수 순서를 권장한다.

## 3. Data — 실제 기획 수치를 넣는 곳

### 3.1 DefaultGameData.cs

**소스:** [DefaultGameData.cs](../Assets/01_Main/02_Scripts/Core/Data/DefaultGameData.cs)

**역할:** 정의 클래스들에 기획서 v0.3의 초기 수치를 넣어, 사용할 수 있는 GameDefinition을 생성한다. 정적 클래스이며 전역 진행 상태를 보관하지 않는다.

| 함수·상수 | 역할 |
|---|---|
| `CreateDefault()` | 생존자 2명·지역 3곳·일반 사건 5개와 공통 규칙 생성 |
| `CreatePrototype()` | 마커스·공업 창고·EVENT_001~003으로 첫 탐색용 정의 생성 |
| `CreateRules()` | 단계·소음·생존 수치, 사건 확률, 귀환표·귀환 사건 구성 |
| `CreateSearchRewards()` | 단계와 D20 구간에 맞는 공통 수색표 12행 생성 |
| `CreateSurvivors()` / `CreateRegions()` / `CreateEvents()` | 개별 콘텐츠 정의 배열 생성 |
| `MARCUS_ID`, `SARA_ID`, 지역 ID 상수 | 기본 콘텐츠를 가리킬 때 사용할 ID |

공개 함수는 CreateDefault와 CreatePrototype이며, 나머지 생성 함수는 내부 구현이다. 시제품도 기본판과 같은 규칙을 사용하고 콘텐츠 수만 줄인다. 호출마다 새로운 정의 묶음을 만든다. 같은 묶음 안에서는 불변인 ‘지나치기’ 행동과 빈 효과를 여러 사건이 함께 참조할 수 있다.

**사용 시점:** Unity 없이 진행 로직을 작성·검사할 때 시작 데이터로 사용한다. 현재 이 클래스를 호출한다고 탐색이 시작되지는 않는다. ScriptableObject에서 GameDefinition을 만드는 Unity 콘텐츠 연결은 후속 작업이다.

## 4. Definitions — 고정 설정과 콘텐츠

### 4.1 GameDefinition.cs

**소스:** [GameDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/GameDefinition.cs)

**역할:** 한 판에서 사용할 규칙·생존자·지역·일반 사건을 묶는 최상위 정의다.

- `Rules`: 공통 규칙인 GameRulesDefinition.
- `Survivors`, `Regions`, `Events`: 사용할 콘텐츠의 읽기 전용 목록. 입력 배열을 복사한다.
- 생성자: null, 빈 생존자·지역, 같은 종류의 콘텐츠 ID 중복, 사건 등장 단계 초과를 검사한다.
- 지역 검사: 사건 참조의 존재·중복, 지역 수색표의 전체 구간, 각 단계의 기회·위험 후보 존재를 검사한다. 현재는 범주 확률이 0%인 경우에도 두 범주의 후보를 요구한다.
- `ValidateEventCandidates()`: 해당 지역에 각 단계에서 사용할 기회·위험 사건이 있는지 확인하는 내부 함수다. 후보를 추첨하지는 않는다.

귀환 사건 ID와 같은 ID의 일반 사건은 거절한다. ID 비교는 대소문자를 구분하며, 행동 ID는 다른 사건에서 재사용할 수 있다.

**연결:** 이후 GameSession이 이 정의를 입력받아 진행 상태를 만든다. 날짜·현재 HP·전리품 원본은 여기에 저장하지 않는다.

### 4.2 GameRulesDefinition.cs

**소스:** [GameRulesDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/GameRulesDefinition.cs)

**역할:** 모든 지역과 생존자가 공유할 게임 설정을 보관하고, 설정표의 완전성을 검사한다. 이름에 Rules가 있지만 실제 행동을 실행하는 클래스는 아니다.

| 데이터 | 의미 | 현재 기본 데이터 |
|---|---|---|
| `MaxStage` / `MaxNoise` / `MaxDays` | 단계·소음·날짜 상한 | 5 / 6 / 7 |
| `InitialResources` | 시작 은신처 자원 | 식량 6, 의약품 1, 부품 0 |
| `TargetParts` | 승리 목표 부품 수 | 8 |
| `DailyFoodConsumption` | 하루 식량 소비량 | 2 |
| `StarvationDamage` | 식량 부족일에 적용할 HP 피해 | 2 |
| `HealingMedicineCost` / `HealingAmount` | 치료 비용·회복량 | 의약품 1 / HP 3 |
| `DefaultSearchRewards` | 지역표가 없을 때 사용할 공통 수색표 | 12행 |
| `StageEventChances` | 단계별 사건 범주 확률표 | 5행 |
| `ReturnRisks` / `ReturnEvent` | 소음별 귀환 위험표·귀환 판정 정보 | 5행 / RETURN_001 |

표의 숫자는 DefaultGameData가 전달하는 초기값이며, 이 클래스에 고정된 상수는 아니다. 생성자에서는 단계·일수·목표·치료 수치의 양수 조건, 소음·식량 소비·부족 피해의 비음수 조건을 검사한다.

| 검사 함수 | 확인하는 내용 |
|---|---|
| `ValidateSearchRewards()` | 1~MaxStage의 각 D20 1~20에 보상 행이 정확히 하나 존재하는지 검사. 지역표 검사에도 재사용 |
| `ValidateStageEventChances()` | 단계별 확률 행의 null·범위 초과·중복·누락 검사 |
| `ValidateReturnRisks()` | 소음 0~MaxNoise의 구간 누락·중복·상한 초과 검사 |

입력 배열은 복사하고 읽기 전용으로 노출한다. 귀환 위험표는 검사할 복사본만 정렬하므로 공개 목록이 소음 순으로 정렬되어 있다고 가정하지 않는다. 실제 소음 제한·식량 소비·치료 적용은 이후 진행 로직의 책임이다.

### 4.3 SurvivorDefinition.cs

**소스:** [SurvivorDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/SurvivorDefinition.cs)

**역할:** 생존자의 고정 능력치를 정의한다.

- `ID`, `Name`: 식별자와 표시 이름.
- `MaxHP`: 최대 HP. 생성 시 1 이상이어야 한다.
- `CombatModifier`, `TechModifier`: 사건 판정에 더할 전투·기술 보정. 현재 생성자는 음수 보정도 허용한다.
- 생성자에서 비어 있는 ID·이름을 거절한다.

**연결:** SurvivorState를 만들 때 ID와 HP 범위의 기준으로 사용한다. 현재 HP는 SurvivorState에 둔다. 실제 공개 이름은 `ID`, `MaxHP`이므로 다른 정의의 `Id`와 혼동하지 않는다.

### 4.4 RegionDefinition.cs

**소스:** [RegionDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/RegionDefinition.cs)

**역할:** 지역의 주 자원, 일반 사건 목록, 선택적 수색표를 정의한다.

- `Id`, `Name`, `MainResource`: 지역 식별자·이름·주 자원.
- `EventIds`: 사용할 일반 사건 ID 목록. 원본 배열을 복사하고 비어 있는 ID를 거절한다.
- `SearchRewards`: 지역 전용 수색표. 지정한 배열을 복사하고 null 행을 거절한다.

`SearchRewards == null`이면 공통표를 사용한다. 지정한 표는 전체 교체이며, 빈 표나 빠진 구간을 공통표로 채우지 않는다. 빈 지역표는 RegionDefinition 단독 생성 시 통과할 수 있지만, GameDefinition으로 묶을 때 거절된다.

**연결:** 사건 참조의 존재·중복과 보상표 전체 구간 검사는 GameDefinition에서 수행한다. 현재 그림·설명문·Unity 참조는 포함하지 않는다.

### 4.5 EventDefinition.cs

**소스:** [EventDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/EventDefinition.cs)

**역할:** 일반 사건의 상황과 처리 방식을 정의하고, 행동·효과를 묶는다.

- `Id`, `Name`, `Description`: 사건 식별자·이름·상황 설명.
- `Category`: 추첨 범주인 기회 또는 위험.
- `Mode`: 즉시 결과형·판정형·선택형.
- `MinStage`, `MaxStage`: 등장 가능한 단계. 양 끝을 포함한다.
- `InstantEffect`, `Actions`: 즉시 효과 또는 행동 목록.

| 처리 방식 | 생성자가 요구하는 구성 |
|---|---|
| `INSTANT` | InstantEffect 필수, 행동 0개 |
| `CHECK` | InstantEffect 없음, 판정이 필요한 행동 정확히 1개 |
| `CHOICE` | InstantEffect 없음, 행동 2~3개. 각 행동의 판정 유무는 자유 |

행동 배열을 복사하고 null·같은 사건 내 행동 ID 중복을 거절한다. 등장 단계의 순서는 자체 검사하며 게임 전체의 최대 단계 초과는 GameDefinition이 확인한다.

**연결:** 후보 추첨 후 현재 사건으로 사용할 정의다. 이 객체 생성만으로 즉시 효과가 적용되거나 행동이 선택되지는 않는다.

### 4.6 EventActionDefinition.cs

**소스:** [EventActionDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/EventActionDefinition.cs)

**역할:** 사건에서 선택할 행동 하나와 판정 조건·결과 효과를 정의한다.

- `Id`, `Text`: 사건 내부 행동 ID와 버튼 문구.
- `StatType`: 판정에 사용할 능력치. NONE은 보정 없는 D20이다.
- `Difficulty`: nullable int인 DC. null이면 판정이 없다.
- `RequiresCheck`: Difficulty에 값이 있는지로 계산한다.
- `Effect`: 판정이 없으면 확정 효과, 판정이 있으면 성공 효과.
- `FailureEffect`: 판정 실패 효과. 판정 없는 행동에서는 null이다.

생성자는 두 개다. 짧은 생성자는 ID·문구·확정 효과를 받는다. 긴 생성자는 능력치·DC·성공·실패 효과를 받으며, 짧은 생성자의 공통 검증을 재사용한다. DC는 1 이상이고 20을 상한으로 제한하지 않는다.

**연결:** 이후 행동 처리에서 RequiresCheck를 확인하고, 필요한 경우 D20 판정 후 효과 하나를 선택한다. 실제 판정과 효과 적용은 아직 연결되지 않았다.

### 4.7 EventEffectDefinition.cs

**소스:** [EventEffectDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/EventEffectDefinition.cs)

**역할:** 사건의 한 결과에서 적용할 자원 보상·HP 변화·소음 증가를 묶는다.

- `HpChange`: 음수는 피해, 양수는 회복, 0은 변화 없음.
- `NoiseIncrease`: 비음수 소음 증가량. 현재 소음 감소는 표현하지 않는다.
- `Rewards`: 자원 보상 항목 목록. 배열을 복사하고 null 배열·null 항목을 거절한다. 빈 목록은 유효하다.

자원 항목은 SearchRewardItemDefinition을 재사용하므로 자원 차감은 지원하지 않는다. HP 변화에는 최대 HP나 현재 HP 기준의 제한을 여기서 적용하지 않는다.

**연결:** 이후 규칙에서 지역 주 자원을 해석하고 다음 HP·소음·전리품을 계산한다. 상한 처리와 상태 반영·사망 처리는 그 단계에서 구현한다.

### 4.8 SearchRewardDefinition.cs

**소스:** [SearchRewardDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/SearchRewardDefinition.cs)

**역할:** 수색 보상표의 한 행이다. 특정 단계·D20 구간에 지급할 보상 항목을 담는다.

- `MinStage`, `MaxStage`: 적용 단계 범위.
- `MinDice`, `MaxDice`: 적용 주사위 눈 범위. 1~20이며 양 끝을 포함한다.
- `Rewards`: 보상 항목의 읽기 전용 목록. 빈 목록은 미발견 행이다.
- `Contains(stage, dice)`: 두 값이 모두 행의 범위에 들어가는지 반환한다.

생성자는 범위 순서·유효성·null 항목을 검사하고 배열을 복사한다. Contains는 범위 포함 여부만 판단하며 입력 오류 예외를 던지는 검증 함수는 아니다. 표 전체의 누락·중복은 GameRulesDefinition 또는 GameDefinition이 확인한다.

**연결:** ExplorationRules.CalculateSearchReward가 현재 단계와 눈에 맞는 행을 찾는 데 사용한다.

### 4.9 SearchRewardItemDefinition.cs

**소스:** [SearchRewardItemDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/SearchRewardItemDefinition.cs)

**역할:** 보상표나 사건 효과에 들어갈 자원 지급 항목 하나다.

- `Amount`: 지급 수량. 0 이상이며 음수는 거절한다.
- `ResourceType`: nullable RESOURCE_TYPE. null이면 현재 지역의 주 자원, 값이 있으면 지정 자원이다.
- 생성자는 값이 있는 ResourceType이 유효한 열거형 값인지 검사한다.

**예:** 주 자원이 식량인 지역에서 ‘주 자원 1개’와 ‘식량 1개’가 같은 보상 행에 있으면 최종 식량은 2개다. 현재 이름은 수색용이지만 사건 보상에도 동일한 의미로 재사용한다.

### 4.10 StageEventChanceDefinition.cs

**소스:** [StageEventChanceDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/StageEventChanceDefinition.cs)

**역할:** 한 단계에서 사건 없음·기회·위험 범주의 발생 확률을 정의한다.

- `Stage`: 적용 단계. 1 이상.
- `NoEventChance`, `OpportunityChance`, `DangerChance`: 각 0~100의 정수 백분율.
- 생성자는 세 확률의 합이 정확히 100인지 검사한다.

**연결:** GameRulesDefinition이 모든 단계에 한 행씩 있는지 확인한다. 범주 추첨용 표본을 해석하고 해당 사건 후보를 고르는 로직은 아직 없다.

### 4.11 ReturnRiskDefinition.cs

**소스:** [ReturnRiskDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/ReturnRiskDefinition.cs)

**역할:** 소음 구간에 따라 달라지는 귀환 위험 안내와 수치를 정의한다.

- `MinNoise`, `MaxNoise`: 적용 소음 범위. 양 끝을 포함하고 최소 소음은 0 이상.
- `Name`: ‘안전’, ‘주의’ 같은 표시 이름.
- `EventChance`: 귀환 사건 발생 확률. 0~100.
- `FailureDamage`: 사건 행동 판정에 실패했을 때의 비음수 피해량.

**연결:** GameRulesDefinition이 전체 소음 구간을 검사한다. EventChance는 사건이 발생할 확률이며 행동 실패·사망 확률이 아니다. 실제 발생 판정과 피해 적용은 이후 로직에서 수행한다.

### 4.12 ReturnEventDefinition.cs

**소스:** [ReturnEventDefinition.cs](../Assets/01_Main/02_Scripts/Core/Definitions/ReturnEventDefinition.cs)

**역할:** 귀환 전용 사건의 문구와 행동 판정 조건을 정의한다.

- `Id`, `Name`, `Description`: 귀환 사건 식별자·이름·상황.
- `ActionId`, `ActionText`: 귀환 대응 행동 ID와 버튼 문구.
- `StatType`, `Difficulty`: 적용 능력치와 양수 DC.
- 생성자는 필수 문자열·능력치 열거형·DC를 검사한다.

**연결:** GameRulesDefinition.ReturnEvent에 보관한다. 실패 피해는 ReturnRiskDefinition에만 있어 중복 저장하지 않는다. 기본 데이터는 RETURN_001, 전투 DC 12다. 일반 단계 사건 목록에 넣지 않는다.

## 5. Enums — 데이터에서 사용하는 분류

### 5.1 RESOURCE_TYPE.cs

**소스:** [RESOURCE_TYPE.cs](../Assets/01_Main/02_Scripts/Core/Enums/RESOURCE_TYPE.cs)

자원 종류다. `FOOD`는 식량, `MEDICINE`은 의약품, `PARTS`는 부품이다. 지역 주 자원, 보상 항목, ResourceAmount.Add에서 사용한다. 새 종류를 추가하면 수량 구조·계산·표시도 함께 검토해야 한다.

### 5.2 STAT_TYPE.cs

**소스:** [STAT_TYPE.cs](../Assets/01_Main/02_Scripts/Core/Enums/STAT_TYPE.cs)

판정에 사용할 보정 종류다. `NONE`은 보정 없음, `COMBAT`은 전투, `TECH`는 기술이다. **NONE도 주사위를 굴릴 수 있다.** 주사위를 굴리는지 여부는 EventActionDefinition.RequiresCheck로 판단한다.

### 5.3 EVENT_CATEGORY.cs

**소스:** [EVENT_CATEGORY.cs](../Assets/01_Main/02_Scripts/Core/Enums/EVENT_CATEGORY.cs)

추첨할 사건의 범주다. `OPPORTUNITY`는 기회, `DANGER`는 위험이다. ‘사건 없음’ 요소는 없으며 StageEventChanceDefinition.NoEventChance로 확률을 표현한다. 범주 추첨 결과를 표현할 형식은 이후 로직에서 정한다.

### 5.4 EVENT_MODE.cs

**소스:** [EVENT_MODE.cs](../Assets/01_Main/02_Scripts/Core/Enums/EVENT_MODE.cs)

사건 처리 방식이다. `INSTANT`는 즉시 결과형, `CHECK`는 판정형, `CHOICE`는 선택형이다. 기회·위험 범주와 독립된 분류이며 EventDefinition이 이 값에 따라 필수 효과와 행동 수를 검사한다.

## 6. States — 플레이 중 달라질 값

### 6.1 SurvivorState.cs

**소스:** [SurvivorState.cs](../Assets/01_Main/02_Scripts/Core/States/SurvivorState.cs)

**역할:** 생존자 한 명의 현재 HP를 담는다.

- `SurvivorId`: 생성 시 SurvivorDefinition.ID에서 복사한 식별자.
- `CurrentHp`: 현재 HP. 생성자는 0~definition.MaxHP 범위를 요구한다.
- `IsAlive`: CurrentHp가 0보다 크면 true다.

정의가 null이면 거절하며, 범위를 벗어난 HP를 자동 보정하지 않는다. 프로퍼티가 getter만 있으므로 향후 피해·치료 계산 후 새 SurvivorState를 만들어 교체하는 방식으로 연결한다. 현재 피해·치료 함수는 없다.

### 6.2 ExpeditionState.cs

**소스:** [ExpeditionState.cs](../Assets/01_Main/02_Scripts/Core/States/ExpeditionState.cs)

**역할:** 한 번의 탐색에서 유지할 기본 진행 값을 담는다.

- `SurvivorId`, `RegionId`: 파견 생존자와 방문 지역의 ID.
- `Stage`, `Noise`: 현재 단계·소음.
- `Loot`: 은신처 반영 전의 임시 전리품 ResourceAmount.

생성자는 비어 있는 ID, 단계 1 미만, 음수 소음을 거절한다. ID의 존재와 게임 설정의 최대 단계·최대 소음은 검사하지 않으므로 이후 Session 연결에서 확인해야 한다.

현재 사건·직전 단계 사건·귀환 시작 정보는 아직 없다. 이 클래스도 getter만 제공하며 이동·수색·귀환을 실행하는 함수는 없다.

## 7. Resources — 자원 수량

### 7.1 ResourceAmount.cs

**소스:** [ResourceAmount.cs](../Assets/01_Main/02_Scripts/Core/Resources/ResourceAmount.cs)

**역할:** 식량·의약품·부품의 비음수 수량을 함께 담는 readonly struct다. 은신처 자원·임시 전리품·한 번의 보상을 같은 형식으로 표현하되, 각각 별개의 값으로 보관한다.

- `Food`, `Medicine`, `Parts`: 자원별 수량. 생성 시 음수는 거절한다.
- `IsEmpty`: 세 수량이 모두 0인지 반환한다. default(ResourceAmount)도 빈 값이다.
- `Add(resourceType, amount)`: 지정 자원을 더한 **새 ResourceAmount**를 반환한다. 기존 값은 바뀌지 않는다.

Add는 음수 수량과 잘못된 자원 종류를 거절하며, 정수 덧셈 넘침은 checked로 예외 처리한다. 차감이나 ResourceAmount 두 개를 한 번에 더하는 함수는 아직 없다.

```csharp
ResourceAmount loot = default;
loot = loot.Add(RESOURCE_TYPE.PARTS , 2);
```

반환값을 다시 대입해야 계산 결과를 보관할 수 있다.

## 8. Rules — 이미 구현된 계산

### 8.1 ExplorationRules.cs

**소스:** [ExplorationRules.cs](../Assets/01_Main/02_Scripts/Core/Rules/ExplorationRules.cs)

**역할:** 전달받은 값과 설정으로 수색 보상 또는 행동 성공 여부를 계산하는 정적 클래스다. 난수를 직접 생성하거나 진행 상태를 변경하지 않는다.

| 함수 | 입력·결과 |
|---|---|
| `CalculateSearchReward()` | 단계·D20 눈·지역 주 자원·공통표·선택적 지역표를 받아 ResourceAmount 반환 |
| `IsActionSuccessful()` | `dice + statModifier >= difficulty` 결과 반환 |
| `ValidateDice()` | D20 눈이 1~20인지 검사하는 내부 함수 |

수색 계산 순서는 다음과 같다.

1. 눈·단계·지역 자원 종류를 검사한다.
2. 지역표가 null이면 공통표, 그 외에는 지역표 전체를 사용한다.
3. 현재 단계와 눈에 맞는 보상 행을 찾는다. 없거나 둘 이상이면 예외를 던진다.
4. 보상 항목의 null 자원을 지역 주 자원으로 해석하고 같은 실제 자원은 합산한다.
5. 합산한 ResourceAmount를 반환한다. 항목이 없으면 빈 보상이다.

행동 성공 계산은 눈 1·20에 특별한 자동 성공·실패를 부여하지 않는다. IsActionSuccessful은 눈만 검증하며 능력치·DC의 콘텐츠 검사는 정의 생성 쪽에서 수행한다.

**현재 범위:** 사건 범주·후보 추첨, 사건 효과 적용, 귀환 위험 계산은 아직 이 클래스에 구현하지 않았다. 실제 수색 시 D20을 한 번만 소비하고 보상을 한 번만 반영하는 보장도 향후 Session에서 구현한다.

## 9. Random — 난수 생성 경계

### 9.1 IRandomSource.cs

**소스:** [IRandomSource.cs](../Assets/01_Main/02_Scripts/Core/Random/IRandomSource.cs)

**역할:** 판정용 정수를 얻는 방법을 정의하는 인터페이스다.

`NextInt(minInclusive, maxExclusive)`는 최솟값 이상·최댓값 미만의 정수를 반환하는 규약이다. 인터페이스 자체에는 구현이 없으며 GameRandom이나 고정 값 검사 구현으로 대체할 수 있다.

**설계상 연결:** 이후 GameSession이 전달받아 게임 난수를 소비한다. UI 연출용 난수와의 분리는 아직 Session·UI가 없어 실행 검증하지 않았다.

### 9.2 GameRandom.cs

**소스:** [GameRandom.cs](../Assets/01_Main/02_Scripts/Core/Random/GameRandom.cs)

**역할:** System.Random을 감싸 IRandomSource를 구현한다.

- 기본 생성자: seed를 지정하지 않은 System.Random 생성.
- seed 생성자: 지정 seed로 System.Random 생성.
- `NextInt()`: minInclusive가 maxExclusive 이상이면 거절하고, 나머지는 System.Random.Next에 전달한다.

**예:** `NextInt(1, 21)`은 D20의 1~20, `NextInt(0, 100)`은 0~99다. 상한이 포함되지 않는 점을 유지한다. seed 생성자는 재현 검사에 사용할 수 있지만, 정확한 상황 검사는 지정 값을 반환하는 대체 IRandomSource가 더 명확하다.

## 10. 함께 읽어야 하는 연결 관계

```text
DefaultGameData
  └─ GameDefinition
      ├─ GameRulesDefinition
      │   ├─ SearchRewardDefinition → SearchRewardItemDefinition
      │   ├─ StageEventChanceDefinition
      │   ├─ ReturnRiskDefinition
      │   └─ ReturnEventDefinition
      ├─ SurvivorDefinition → SurvivorState 생성 기준
      ├─ RegionDefinition → 일반 사건 ID·지역 수색표
      └─ EventDefinition → EventActionDefinition → EventEffectDefinition
                                                    └─ SearchRewardItemDefinition

ExplorationRules → 정의와 눈을 받아 결과 값 계산
IRandomSource ← GameRandom이 구현
ExpeditionState → 현재 단계·소음·ResourceAmount 전리품 보관
```

위 연결은 데이터 참조와 현재 계산 함수의 관계다. 출발부터 정산까지 자동으로 호출되는 실행 흐름은 아직 없다.

향후 Session이 연결할 때의 예는 다음과 같다. 이는 **다음 구현 기준**이며 현재 완료된 동작은 아니다.

1. GameDefinition에서 생존자·지역과 규칙을 선택한다.
2. SurvivorState·ExpeditionState를 준비하고 단계 진입 사건을 처리한다.
3. 수색 요청을 검증한 뒤 IRandomSource에서 D20을 한 번 받는다.
4. ExplorationRules로 보상을 계산하고 임시 전리품에 한 번 반영한다.
5. 확정 결과를 보관하고, 결과 확인 후 다음 단계 또는 귀환을 허용한다.
6. 귀환·사망에 따라 전리품을 은신처에 반영하거나 폐기한다.

## 11. 자주 혼동하는 부분

| 혼동하기 쉬운 항목 | 현재 코드의 의미 |
|---|---|
| 정의 생성과 게임 실행 | 생성자는 데이터 검사·보관을 수행한다. 탐색은 아직 실행하지 않음 |
| 범주와 처리 방식 | CATEGORY는 기회·위험, MODE는 즉시·판정·선택 |
| NONE과 판정 없음 | NONE은 보정 없음. 판정 없음은 Difficulty == null |
| 지역표 null과 빈 표 | null은 공통표 사용. 빈 지역표는 전체 콘텐츠 검사에서 오류 |
| 보상 행과 보상 항목 | 행은 단계·눈 구간, 항목은 자원 종류·수량 |
| 설정 피해와 실제 HP 변화 | FailureDamage는 양수 피해량, HpChange는 피해일 때 음수 |
| 귀환 위험 100% | 사건 발생 확정. 행동 실패나 사망 확정이 아님 |
| 보상 반환과 보상 지급 | 계산 함수는 값만 반환. 지급·중복 방지는 향후 Session 책임 |
| 목록의 읽기 전용과 배열 복사 | 읽기 전용 노출에 더해 입력 배열도 복사하여 외부 배열 수정의 영향을 차단 |

## 12. 검사 범위와 관련 문서

소스 설명은 이 문서 작성 시 실제 Core 23개 파일을 읽고 대조한 내용이다. 문서 작성 작업에서는 C# 코드를 변경하거나 검사를 다시 실행하지 않았다.

직전 데이터 구현 작업에서 Core 23개 파일의 C# 컴파일과 `Tests/CoreData/Verify-CoreData.ps1`의 392개 검사가 통과했다. 여기에는 세 지역의 수색 결과 300가지, 기획 데이터·사건 효과, 잘못된 데이터 거절, 배열 복사·읽기 전용 노출, 개발용 혼합 보상 검사가 포함된다. 이 기록은 GameSession·Unity 전체 컴파일·Play Mode·빌드의 검증을 뜻하지 않는다.

- [데이터 구현 안내](LAST_SUPPLY_데이터_구현_안내.md): 생성 함수와 재실행 검사 방법.
- [코드 설계서](LAST_SUPPLY_코드_설계서.md): Session·Unity 연결·결과 확정·수명 관리의 후속 설계.
- [게임 기획서](LAST_SUPPLY_게임_기획서.md): 실제 규칙과 기본판 수치의 기준.

스크립트가 추가되거나 공개 데이터·함수가 변경되면 해당 항목과 위 파일 목록을 함께 갱신한다. 구현 계획은 현재 기능 설명과 구분해서 기록한다.
