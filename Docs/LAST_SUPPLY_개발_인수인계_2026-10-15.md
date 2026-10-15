# LAST SUPPLY 개발 인수인계

> 작성일 / 갱신일: 2026-10-15 / 2026-10-15
>
> 기준: 게임 기획서 v0.3, 코드 설계서 v0.3, 현재 프로젝트 파일과 이 채팅의 작업·검증 기록.
>
> 현재 상태: Core 23개 스크립트와 기본 콘텐츠 데이터 작성. 직전 데이터 구현 작업에서 Core C# 컴파일과 392개 검사 통과. GameSession과 플레이 가능한 탐색 흐름은 아직 미구현.
>
> 이번 갱신은 문서 작업이다. C# 코드·Scene·Prefab·Inspector·패키지·프로젝트 설정·Git 인덱스를 변경하거나 검사를 재실행하지 않았다.

## 1. 재개 시 가장 먼저 확인할 내용

1. 이 문서와 아래 문서를 읽고 실제 Core 파일을 확인한다.
   - [Core 스크립트 설명서](LAST_SUPPLY_Core_스크립트_설명서.md): 23개 파일의 역할·주요 데이터·함수·연결 관계.
   - [데이터 구현 안내](LAST_SUPPLY_데이터_구현_안내.md): 기본 콘텐츠 생성 방법과 재실행 검사.
   - [게임 기획서](LAST_SUPPLY_게임_기획서.md): 기본판 규칙과 수치.
   - [코드 설계서](LAST_SUPPLY_코드_설계서.md): Session·Unity 연결·결과 확정·수명 관리 기준.
2. 새 PC에 Core 23개 파일과 각 .meta, Docs의 문서 5개, Tests/CoreData/Verify-CoreData.ps1이 전달됐는지 확인한다.
3. git status와 최근 커밋을 확인한다. 파일이 스테이징되어 있어도 커밋·푸시 완료를 뜻하지 않는다.
4. PowerShell 7에서 아래 검사를 실행하고, Unity 전체 컴파일·Console은 별도로 확인한다.
5. 데이터 정의를 중복 작성하지 말고, 4장의 사건 판정 계산과 GameSession 연결을 사용자와 함께 시작한다.

```powershell
pwsh -NoProfile -File .\Tests\CoreData\Verify-CoreData.ps1
```

**중요:** 과거 인수인계의 SurvivorState 이름 불일치는 해결됐다. 현재 파일은 definition.MaxHP와 definition.ID를 참조한다. 다시 컴파일 복구나 전체 명명 변경부터 시작하지 않는다.

## 2. 현재 완료한 구현

Core 루트는 `Assets/01_Main/02_Scripts/Core`, 네임스페이스는 `LastSupply.Core`다. 모든 Core 코드는 순수 C#이며 UnityEngine·UniTask·HM CodeBase에 의존하지 않는다.

| 폴더 | 파일 | 현재 책임 |
|---|---|---|
| Data | DefaultGameData.cs | 기획 수치를 넣은 기본판·시제품 GameDefinition 생성 |
| Definitions | GameDefinition.cs | 규칙·생존자·지역·일반 사건 묶음, 콘텐츠 참조와 후보 검증 |
| Definitions | GameRulesDefinition.cs | 단계·소음·생존 설정, 수색·사건 확률·귀환 표와 전체 구간 검증 |
| Definitions | SurvivorDefinition.cs | ID·이름·최대 HP·전투·기술 보정 |
| Definitions | RegionDefinition.cs | 지역 ID·이름·주 자원·사용 사건 ID·선택적 지역 수색표 |
| Definitions | EventDefinition.cs | 일반 사건의 범주·처리 방식·등장 단계·상황·효과·행동 |
| Definitions | EventActionDefinition.cs | 행동 ID·문구·판정 유무·능력치·DC·확정/성공/실패 효과 |
| Definitions | EventEffectDefinition.cs | HP 증감·소음 증가·자원 보상 목록 |
| Definitions | SearchRewardDefinition.cs | 단계·D20 구간별 수색 보상 행, Contains |
| Definitions | SearchRewardItemDefinition.cs | 비음수 보상 수량과 선택적 자원 종류 |
| Definitions | StageEventChanceDefinition.cs | 단계별 사건 없음·기회·위험 확률, 합계 100% 검사 |
| Definitions | ReturnRiskDefinition.cs | 소음 구간·위험 표시·귀환 사건 확률·실패 피해 |
| Definitions | ReturnEventDefinition.cs | 귀환 사건 ID·상황·행동·능력치·DC |
| Enums | RESOURCE_TYPE.cs | FOOD / MEDICINE / PARTS |
| Enums | STAT_TYPE.cs | NONE / COMBAT / TECH |
| Enums | EVENT_CATEGORY.cs | OPPORTUNITY / DANGER |
| Enums | EVENT_MODE.cs | INSTANT / CHECK / CHOICE |
| States | SurvivorState.cs | 생존자 ID·현재 HP·생존 여부 |
| States | ExpeditionState.cs | 파견 생존자·지역 ID·현재 단계·소음·임시 전리품 |
| Resources | ResourceAmount.cs | 비음수 자원 수량, IsEmpty, 새 값을 반환하는 Add |
| Rules | ExplorationRules.cs | 수색 보상 선택·합산, D20 + 보정 >= DC 계산 |
| Random | IRandomSource.cs | NextInt(minInclusive, maxExclusive) 규약 |
| Random | GameRandom.cs | System.Random 구현, 기본/seed 생성자, 잘못된 범위 거절 |

Core 폴더 정리 시 스크립트와 기존 .meta를 함께 이동했고, 당시 30개 파일의 내용 보존을 확인했다. 데이터 추가 작업에서는 새 스크립트의 .meta와 Data 폴더 .meta를 작성하고 Core 내 GUID 유효성·중복 여부를 확인했다.

### 2.1 생성해서 사용할 데이터

```csharp
GameDefinition prototype = DefaultGameData.CreatePrototype();
GameDefinition defaultGame = DefaultGameData.CreateDefault();
```

| 구성 | 생존자·지역·사건 |
|---|---|
| CreatePrototype | 마커스 1명, 공업 창고 1곳, EVENT_001~003, RETURN_001 |
| CreateDefault | 마커스·사라, 마트·병원·창고, EVENT_001~005, RETURN_001 |

두 구성은 같은 기본 규칙을 사용하며 시제품만의 임시 게임 규칙은 없다. 생성할 때마다 별도의 정의 묶음을 만들지만, 생성만으로 게임이 시작되지는 않는다.

공통 초기값은 최대 5단계·소음 6·7일, 식량 6/의약품 1/부품 0, 목표 부품 8, 하루 식량 소비 2, 부족 피해 2, 의약품 1당 HP 3 회복이다. 모두 기획의 초기 테스트 값이며 재미·밸런스 검증 결과가 아니다.

### 2.2 데이터 검증과 현재 한계

- 생성자는 null·잘못된 수치·필수 문구를 검사하고 목록 입력을 복사해 읽기 전용으로 노출한다.
- GameRulesDefinition은 공통 수색표의 전체 단계/D20 구간, 단계별 사건 확률표, 귀환 소음표의 누락·중복·범위 초과를 검사한다.
- GameDefinition은 같은 종류의 콘텐츠 ID 중복, 잘못된 지역 사건 참조, 지역 수색표 전체 구간, 사건 등장 단계, 일반 목록의 귀환 사건 ID를 검사한다.
- 모든 지역·단계에 기회·위험 사건 후보가 필요하다. 현재 검증은 해당 범주 확률이 0%여도 후보를 요구한다.
- EventDefinition은 즉시형 효과·행동 0개, 판정형 판정 행동 1개, 선택형 행동 2~3개와 사건 내부 행동 ID 중복을 검사한다.
- EventEffectDefinition은 자원 획득·HP 증감·소음 증가를 표현한다. 자원 차감·소음 감소는 현재 지원하지 않으며 기본 콘텐츠에는 필요하지 않다.
- SearchRewardItemDefinition은 사건 보상에도 재사용한다. 이름의 범위는 수색용이지만 현재 지급 대상·수량의 의미는 같다. 전체 명명 변경을 임의로 진행하지 않는다.
- GameRulesDefinition의 초기 3인자 생성자는 전체 설정을 받는 생성자로 확장됐다. 현재 사용 예시는 DefaultGameData.CreateRules다.
- SurvivorState·ExpeditionState는 getter만 제공한다. ExpeditionState의 ID 존재·단계/소음 상한 확인과 다음 상태 교체는 이후 Session에서 연결해야 한다.
- DefaultGameData는 Unity 연결 전 Core 구현·검사를 위한 코드 기반 콘텐츠다. 최종 ScriptableObject 카탈로그 → GameDefinition 변환은 미구현이며 설계를 JSON·Resources 로딩으로 바꾼 것은 아니다.

## 3. 유지할 게임·설계 결정

### 3.1 첫 완성 목표

PC 싱글, 2D 일러스트·텍스트·버튼 중심의 운 기반 생존 탐색 게임이다. 먼저 생존자 1명·지역 1곳·일반 사건 세 방식과 귀환 사건을 연결하여 다음 탐색 한 번을 완성한다.

```text
출발 → 단계 진입 사건 → 사건 결과 확인 → 수색/생략
     → 다음 단계/귀환 → 생존 또는 사망 → 전리품 반영/폐기
```

기본판의 다일 생존·치료·승패와 콘텐츠 확대는 첫 탐색 이후 순차적으로 연결한다. 저장·장비·특성·재굴림은 기본판에 자동 추가하지 않는다.

### 3.2 수색·사건·귀환

- 수색은 단계당 최대 1회, 능력치 보정·자체 비용·자체 소음 없이 D20 한 번으로 보상 묶음을 결정한다.
- 기본 콘텐츠는 마트=식량, 병원=의약품, 창고=부품의 단일 주 자원 보상표다. 혼합 보상 검사용 데이터를 기본판 수급에 넣지 않는다.
- 항목의 ResourceType이 null이면 지역 주 자원이다. 같은 실제 자원으로 해석되는 항목은 합산한다. 세 자원이 모두 0일 때만 미발견이다.
- 지역표가 null일 때만 공통표를 쓴다. 지정한 지역표는 전체 교체이며 누락을 공통표로 보충하지 않는다.
- 사건 범주와 처리 방식은 독립적이다. STAT_TYPE.NONE은 보정 없는 판정이고, 판정 없음은 Difficulty == null이다.
- 일반 사건에서 사망하면 수색·귀환 판정을 진행하지 않는다. 마지막 단계에서도 수색 또는 생략을 처리한 뒤 귀환한다.
- 귀환 실패 피해는 ReturnRiskDefinition에만 보관한다. ReturnEventDefinition에 중복 저장하지 않는다.
- 소음 0~2는 안전 귀환, 소음 6은 사건 발생 확정이다. 사건 발생과 행동 실패·사망은 서로 다른 판정이다.

### 3.3 상태·난수·표현 경계

- 향후 상태 원본은 GameSession이 소유하고, 규칙은 입력값으로 결과를 계산한다. Provider는 Unity 연결·작업 수명, UI MVP는 표시·입력을 담당한다.
- Session만 게임 판정용 난수를 소비하고 연출 난수와 분리한다. 현재는 Session이 없어 이 보장이 연결된 상태가 아니다.
- D20은 NextInt(1, 21), 확률 표본은 NextInt(0, 100)이다. 상한은 포함하지 않는다.
- 확률 0%·100%, 후보 0개·1개는 설계에 따라 불필요한 난수를 소비하지 않는다.
- 직전 단계에 사건이 없으면 직전 사건 ID도 비운다. 마지막으로 만난 사건 ID와 혼동하지 않는다.
- 결과·보상을 연출 전에 확정하고 상태에 한 번 적용한다. 결과 확인·화면 재열기·연출 취소가 재굴림·재지급을 만들면 안 된다.
- 잘못된 상태·ID·Revision 입력은 상태·자원·난수를 소비하지 않아야 한다. 결과 ID·Revision·명령 거절 처리는 미구현이다.
- MVP는 UI에만 적용하고, 필요 없는 Manager·추상화·전역 이벤트를 미리 늘리지 않는다.

## 4. 다음 작업 순서

데이터 정의를 계속 늘리는 단계에서 실제 탐색을 연결하는 단계로 넘어간다. 아래는 후속 구현 기준이며 이번 문서 갱신에서 구현하지 않았다.

1. **사건 관련 순수 계산:** 이미 있는 StageEventChanceDefinition과 EventDefinition을 사용해 범주 판정·등장 후보 필터·직전 단계 중복 제외를 작은 단위로 작성한다. 난수 생성은 Session에 남긴다. 귀환 위험 행 선택과 사건 효과의 다음 값 계산도 필요한 순서에 맞춰 추가한다.
2. **진행 상태·결과와 GameSession:** GAME_PHASE, GameRunState, GameSnapshot, ActionReport, 결과 후속 흐름·ID·Revision을 필요한 만큼 정의하고 명령 검증을 연결한다. 출발 시 실제 단계 진입 사건을 처리하며 사건을 생략한 별도 시제품 규칙을 만들지 않는다.
3. **첫 탐색 검증:** 고정 값을 반환하는 IRandomSource로 세 사건 방식, 수색/생략, 마지막 단계, 귀환, 사망, 전리품 반영/폐기, 중복 입력과 거절된 입력의 난수 미소비를 확인한다.
4. **Unity 연결:** ScriptableObject 콘텐츠 변환, LastSupplyGameProvider, HM UI MVP와 최소 버튼·텍스트 화면을 연결한다. Scene·Prefab·Inspector·패키지·설정 변경은 별도 범위다.
5. **연출과 기본판:** UniTask 연출·취소·재시작 수명을 연결하고 다일 정산·치료·승패를 구현한다. 하루 종료 순서는 기획서 3.4를 지킨다.

**아직 없는 것:** 사건 추첨·효과 적용·귀환 계산, 확장된 탐색 상태, GameRunState/GameSession, GAME_PHASE, Snapshot/ActionReport/Revision, SurvivalRules, 다일 정산·승패·치료, ScriptableObject 콘텐츠·변환, Provider/UI/주사위 연출·재시작. 저장은 후속 후보다.

기본 데이터에 2명·3지역·5사건이 모두 들어 있다는 사실은 그 콘텐츠로 게임을 플레이할 수 있다는 뜻이 아니다.

## 5. 검증 기록과 재실행 방법

| 구분 | 확인 수준 |
|---|---|
| 실제 소스·문서 확인 | Core 23개 파일 및 문서 검토. 이번 갱신에서 파일 목록·수정된 SurvivorState·패키지 설정을 다시 확인 |
| 마지막 Core C# 컴파일 | 직전 데이터 구현 작업에서 실제 23개 파일을 PowerShell Add-Type으로 함께 컴파일하여 성공 |
| 재실행 가능한 데이터 검사 | Tests/CoreData/Verify-CoreData.ps1 작성·실행, 392개 검사 통과 |
| 검사 주요 범위 | 기본판·시제품, 기획 수치·사건 효과, 세 지역 수색 300가지, 잘못된 데이터 거절, 배열 복사·읽기 전용, 개발용 혼합 지역표 |
| Core 메타데이터 | 데이터 추가 작업 시 스크립트/.meta 짝, Core GUID 유효성·중복 검사 통과 |
| Core 설명 문서 | 직전 문서 작업 시 23개 파일 누락 없음·로컬 링크 26개 확인 |
| Unity 전체 컴파일·Console | 확인하지 않음 |
| EditMode / PlayMode / 빌드 / 실제 플레이 | 수행하지 않음 |
| Session 진행·중복 지급·난수 미소비 | 로직 미구현으로 검증하지 않음 |

이번 인수인계 갱신에서는 검사 스크립트를 재실행하지 않았다. 위 성공 기록은 해당 작업 시점의 결과이며, 새 PC나 이후 코드 변경 후에는 재실행한다.

프로젝트 루트의 PowerShell 7에서 다음을 실행한다.

```powershell
pwsh -NoProfile -File .\Tests\CoreData\Verify-CoreData.ps1
```

검사는 실제 Core 파일을 컴파일하고 메모리에서 데이터를 생성·검증하며 프로젝트 파일을 변경하지 않는다. PowerShell이 .NET C#으로 컴파일한 결과를 Unity의 컴파일·플레이 검증으로 해석하지 않는다. 과거 임시 검사와 달리 데이터 검사 스크립트가 현재 저장소에 존재한다.

## 6. 환경·Git·PC 이동

| 항목 | 이번 갱신에서 확인한 값 |
|---|---|
| 현재 PC 프로젝트 루트 | F:/GitFork/LastSupply |
| 이전 PC 프로젝트 루트 | D:/Fork/LastSupply — 경로를 고정해서 사용하지 않는다 |
| Unity | 6000.3.20f1 |
| Git | main, HEAD e7751f3 (개발 시작), remote 이름 origin |
| 현재 Scene 및 빌드 설정 경로 | Assets/01_Main/01_Scene/SampleScene.unity |
| HM CodeBase | Git UPM, 현재 캐시 package.json 1.0.1 |
| HM CodeBase lock 해시 | fb08effd5ffc8afab1d4e428c7a1ca9574c3d504 |
| UniTask | Assets/Plugins/UniTask, package.json 2.5.11 |
| uGUI / Test Framework | manifest 2.0.0 / 1.6.0 |
| DOTween | 앞선 검토에서 Assets/Plugins/Demigiant/DOTween 파일 존재 확인. 버전·실행 연동 미검증 |

### 6.1 Git 스냅샷

이번 갱신 직전에는 Core 폴더 이동·새 데이터 코드와 .meta, 문서, 검사 스크립트, EditorBuildSettings 변경이 스테이징되어 있었다. HEAD는 e7751f3이며 이 작업들은 아직 그 커밋에 들어 있지 않다. 이번 문서 갱신분은 스테이징된 기존 인수인계서 위에 작업 트리 변경으로 남을 수 있다.

이 스냅샷은 이후 사용자의 작업으로 달라질 수 있다. 원격과의 동기화·푸시 여부는 확인하지 않았으며, 에이전트는 이번 요청에서 스테이징·커밋·푸시를 수행하지 않았다. 기존 스테이징과 사용자 변경을 되돌리지 않는다.

### 6.2 다른 PC로 전달할 파일

- Git 이동 시 최신 변경의 커밋·푸시와 새 PC의 반영 여부를 확인한다. 스테이징만으로는 다른 PC에 전달되지 않는다.
- 복사 이동 시 Assets, Packages, ProjectSettings, Docs, Tests를 전달하고 Assets의 .meta를 함께 보존한다.
- Library, Temp, Obj, Logs 등 .gitignore의 재생성 대상은 소스 전달물과 구분한다.
- UniTask는 Assets 플러그인이므로 UPM으로 중복 설치하지 않는다. HM CodeBase는 manifest와 lock으로 복원한다.
- 새 PC에서 Unity 임포트·패키지 복원을 기다린 뒤 데이터 검사와 Unity Console을 별도로 확인한다. 기존 작업이 있으면 덮어쓰기 전에 차이를 확인한다.

## 7. 최신 협업·수정 권한

- 사용자가 데이터 코드의 직접 작성을 승인하여 에이전트가 남은 데이터 정의·기본 콘텐츠·검사 코드를 작성했다. 이후 데이터 작업은 그 승인 범위 안에서 진행한다.
- 진행 로직부터는 사용자가 참여한다. 다음 코드는 작은 단위로 제시하고 목적·구조 선택 이유·기존 동작 변화를 설명한다. 별도의 직접 수정 요청 없이 로직 파일을 임의로 생성·수정하지 않는다.
- 질문·검토·설계 논의를 파일 수정 허가로 해석하지 않는다. 이번 요청은 인수인계서 갱신이며 게임 코드를 변경하는 요청이 아니다.
- 코드 수정 권한을 Scene·Prefab·Inspector·패키지·프로젝트 설정 변경으로 확대하지 않는다.
- 존댓말로 결론부터 간결하게 답한다. 사실·설계 제안·추측·미검증을 구분하고 사용자 의견에 무조건 동의하지 않는다.
- 단순한 구조를 우선하고 범위 밖 리팩터링·이동·이름 변경·기능 추가는 먼저 제안한다. 기존 사용자 변경과 Git 스테이징을 보존한다.

### 7.1 C#·Unity 컨벤션

- private/protected 필드 _exampleVariable, 매개변수 exampleVariable, 정적 필드 s_exampleVariable, 상수/readonly 필드 EXAMPLE_VARIABLE. 우선순위는 상수·readonly → 정적 → 접근 제한자다.
- 일반 함수 PascalCase, 코루틴 ExampleFunction_cor, 이벤트 바인딩 OnExampleActioned.
- UniTask라도 동기 완료면 일반 이름을 사용한다. 비동기 완료를 기다리거나 진행 중 작업을 반환하면 ExampleFunction_async를 사용한다.
- 인터페이스 I 접두사, 추상 클래스 A 접두사, 싱글톤 Provider 접미사. UI 컴포넌트는 기능명 + UI 종류다.
- MVP는 UI에만 적용한다. Model_model, Class_view, Presenter_presenter 형식이다.
- 구조체는 PascalCase, 열거형 이름·요소는 UPPER_SNAKE_CASE다.
- Unity 참조는 Obj/Trans/Txt/Btn/Img/Input/Drop/Sprite/Animator/RectTrans 접미사와 필드 명명 규칙을 함께 적용한다.
- 짧은 문장·호출·조건은 한 줄로 유지한다. 긴 코드만 가독성을 위해 줄바꿈하고 정상적인 블록 들여쓰기를 유지한다. 현재 소스의 if ( condition ), 인자 구분 ` , ` 관례를 따른다.
- 기존 ID/Id, MaxHP/CurrentHp 혼용을 임의로 통일하지 않는다. 실제 선언과 참조를 맞춘다.

## 8. 다음 채팅에 붙여 넣을 재개 요청

```text
LastSupply 개발을 이어가려고 합니다.
Docs/LAST_SUPPLY_개발_인수인계_2026-10-15.md,
Core 스크립트 설명서·데이터 구현 안내와 기획서·코드 설계서 v0.3,
Assets/01_Main/02_Scripts/Core의 실제 파일을 먼저 확인해 주세요.

현재 Core 23개와 기본 콘텐츠 데이터가 있습니다.
DefaultGameData.CreatePrototype()은 마커스·창고·일반 사건 3개를 사용합니다.
SurvivorState의 ID/MaxHP 참조 오류는 이미 해결됐습니다.
Tests/CoreData/Verify-CoreData.ps1이 있으며 마지막 실행에서 392개 검사를 통과했습니다.
이전 검사 결과를 현재 Unity 컴파일·플레이 검증으로 간주하지 마세요.

데이터 코드는 에이전트가 직접 작성하도록 승인했지만 진행 로직부터는 제가 참여합니다.
로직 파일을 바로 수정하지 말고 작은 단위로 코드를 제시하고 이유를 설명해 주세요.
다음은 기존 데이터를 사용하는 사건 범주 판정·후보 필터 계산과
GameSession 연결을 준비하는 단계입니다. 이미 있는 데이터 정의를 다시 만들지 마세요.
사건·수색·귀환·사망·전리품 반영/폐기가 이어지는 첫 탐색을 먼저 완성해 주세요.
```

## 9. 이번 채팅에서 달라진 점

- 기존 SurvivorState 참조 오류가 해결된 실제 파일을 확인했다. Core 11개 전체가 컴파일되지 않던 이전 기록은 현재 상태가 아니다.
- 사용자가 GameRulesDefinition·사건 열거형·효과·행동·사건 정의를 입력하고, 에이전트가 검토·컴파일·생성자 검사를 진행했다.
- 사용자 요청으로 Core를 역할별 폴더로 정리하고 기존 스크립트와 .meta를 함께 이동했다.
- 데이터 직접 작성 승인 후 사건 확률·귀환 정의·GameDefinition·기본판/시제품 데이터를 추가하고 GameRulesDefinition을 확장했다.
- 재실행 가능한 데이터 검사 스크립트를 작성해 23개 Core 컴파일과 392개 검사 통과를 확인했다.
- 데이터 구현 안내와 Core 전체 스크립트 설명서를 작성하고, 코드 설계서의 구현 현황을 갱신했다.
- 자체 평가 결과는 ‘정의·상태 분리와 데이터 구성 방향은 기획에 부합하나, Session·첫 탐색 연결과 중복 입력 방지는 아직 미구현’이다. 다음 작업은 실제 흐름 연결에 집중한다.
