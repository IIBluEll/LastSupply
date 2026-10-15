# LAST SUPPLY 개발 인수인계

> 작성일: 2026-10-15 / 다른 PC에서 개발을 이어가기 위한 작업 기록
>
> 기준: 게임 기획서 v0.3, 코드 설계서 v0.3, 작성 시점의 실제 프로젝트 파일
>
> 현재 상태: Core 기반 코드 11개 작성. 현재 소스는 프로퍼티 이름 불일치로 C# 컴파일 실패. 플레이 가능한 게임은 아직 구현하지 않았다.

## 1. 다른 PC에서 가장 먼저 할 일

1. 이 문서와 아래 두 문서, `Assets/01_Main/02_Scripts/Core`의 실제 소스를 확인한다.
   - [게임 기획서](LAST_SUPPLY_게임_기획서.md)
   - [코드 설계서](LAST_SUPPLY_코드_설계서.md)
2. 최신 작업 파일이 전달되었는지 확인한다. 이 문서 작성 시 최신 코드와 기획서는 Git 미추적 상태다. 기존 커밋만 받으면 오늘 작성한 파일이 없을 수 있다.
3. 2장의 `SurvivorDefinition`과 `SurvivorState` 프로퍼티 이름 불일치를 먼저 해결한다.
4. Core 컴파일과 Unity Console을 확인한다. 이후 게임 정의와 진행 상태, GameSession 연결을 준비한다.

**협업 방식:** 사용자가 코드를 직접 입력하며 진행한다. 에이전트는 실제 파일을 검토하고 다음에 작성할 코드를 작은 단위로 제시한다. 이번 인수인계 요청은 문서 작성 요청이며 게임 코드를 직접 수정하는 요청으로 확대하지 않는다.

## 2. 재개 시 우선 해결할 컴파일 오류

### 2.1 확인한 오류

`SurvivorDefinition.cs`의 실제 선언과 `SurvivorState.cs`의 참조 이름이 다르다. C#은 대소문자를 구분하므로 컴파일되지 않는다.

| 위치 | 현재 이름 | 상태 |
|---|---|---|
| SurvivorDefinition 선언 | `ID`, `MaxHP` | 실제 선언 |
| SurvivorState 20행 | `definition.MaxHp` | 없는 프로퍼티 참조, CS1061 |
| SurvivorState 25행 | `definition.Id` | 없는 프로퍼티 참조, CS1061 |
| RegionDefinition 선언 | `Id` | 현재 지역 ID의 실제 이름 |

최소 변경 제안은 생존자 정의의 현재 이름을 유지하고, `SurvivorState`의 참조 두 곳을 맞추는 것이다.

```csharp
if ( currentHp < 0 || currentHp > definition.MaxHP )
{
    throw new ArgumentOutOfRangeException(nameof(currentHp));
}

SurvivorId = definition.ID;
```

위 코드는 수정안이며 파일에 적용하지 않았다. 다음 작업에서 사용자에게 제시하고 실제 파일에 반영됐는지 확인한다. 이전 채팅의 `survivor.Id`, `survivor.MaxHp` 초기화 예시도 현재 생존자 선언에 맞춰 읽어야 한다. 지역의 `region.Id`는 그대로다.

### 2.2 반영을 확인한 이전 정정

- `IRandomSource.NextInt`와 `GameRandom.NextInt`의 두 번째 매개변수는 현재 `maxExclusive`다.
- `NextInt(1, 21)`은 D20의 1~20을 반환한다. 상한은 포함하지 않는다.
- `ExplorationRules.IsActionSuccessful`의 매개변수는 현재 `difficulty`다.
- `CalculateSearchReward` 시작 시 D20 입력 검증이 들어 있다.

## 3. 프로젝트 환경과 다른 PC로 전달할 범위

| 항목 | 현재 확인 내용 |
|---|---|
| 원래 PC의 프로젝트 루트 | `D:/Fork/LastSupply` — 다른 PC에서는 실제 경로 사용 |
| Unity | `6000.3.20f1` |
| Git | 브랜치 `main`, HEAD `42852c3`, remote 이름 `origin` |
| Core 경로 | `Assets/01_Main/02_Scripts/Core` |
| 네임스페이스 | `LastSupply.Core` |
| 현재 Scene 경로 | `Assets/01_Main/01_Scene/SampleScene.unity` |
| HM CodeBase | Git UPM 패키지, 캐시 package.json 기준 `1.0.1` |
| HM CodeBase lock 해시 | `fb08effd5ffc8afab1d4e428c7a1ca9574c3d504` |
| UniTask | `Assets/Plugins/UniTask`에 배치, package.json 기준 `2.5.11` |
| DOTween | `Assets/Plugins/Demigiant/DOTween`에 파일 존재. 버전·실행 연동은 이번에 검증하지 않음 |
| uGUI / Test Framework | manifest 등록값 `2.0.0` / `1.6.0` |

UniTask는 Assets 플러그인으로 이미 들어 있으므로 새 PC에서 UPM으로 중복 설치하지 않는다. HM CodeBase는 `Packages/manifest.json`과 `packages-lock.json`을 전달해 패키지를 복원한다. `Library/PackageCache` 자체를 소스 대신 전달할 필요는 없다.

### 3.1 Git 작업 트리 스냅샷

이 문서 작성 직전 `git status --short`에서 확인한 상태다. 이후 사용자의 커밋 여부에 따라 달라질 수 있다.

- 미추적: `Assets/01_Main`, `Assets/Plugins`, `Assets/Resources`와 각 루트 `.meta`, 최신 기획서·코드 설계서.
- 수정: `Packages/manifest.json`, `Packages/packages-lock.json`.
- 수정: `Assets/Settings/Mobile_RPAsset.asset`, `PC_RPAsset.asset`.
- 수정: `ProjectSettings/PackageManagerSettings.asset`, `ProjectSettings.asset`, `URPProjectSettings.asset`.
- 삭제 상태: 기존 `Assets/Scenes`의 Scene 및 `.meta`, 기존 `Docs/LAST_SUPPLY_게임_기획서_v0.1.md`.
- 현재 Scene은 `Assets/01_Main/01_Scene`에 존재한다. 이동 의도나 Scene 내부 연결까지 이번 검토에서 검증한 것은 아니다.
- 에이전트는 위 변경을 만들거나 되돌리지 않았으며, 커밋·푸시하지 않았다. 이번 요청에서 새로 작성한 것은 이 인수인계 문서다.

### 3.2 PC 이동 시 확인

- Git으로 이동한다면 사용자가 변경 목록을 검토하고 필요한 파일의 커밋·푸시 여부를 확인한다. 현재 HEAD만으로 최신 작업이 전달된다고 가정하지 않는다.
- 파일 복사로 이동한다면 `Assets`, `Packages`, `ProjectSettings`, `Docs`를 `.meta`와 함께 전달한다. 플러그인·리소스도 포함한다.
- `Library`, `Temp`, `Obj`, `Logs`는 현재 `.gitignore`의 재생성 대상이다. 새 PC에서 Unity의 임포트·패키지 복원을 기다린다.
- 새 PC에서 Core C# 파일 11개와 최신 문서 3개가 있는지 확인한다. 다른 PC에 기존 작업이 있다면 덮어쓰기 전에 차이를 확인한다.

## 4. 작성된 코드와 현재 책임

아래 11개 파일은 실제 프로젝트에 존재한다. 클래스가 있다는 사실과 게임 전체가 연결됐다는 사실은 구분한다.

| 파일 | 현재 책임 |
|---|---|
| `RESOURCE_TYPE.cs` | `FOOD`, `MEDICINE`, `PARTS` |
| `ResourceAmount.cs` | 세 자원의 비음수 수량을 담는 readonly struct. `IsEmpty`, 자원별 `Add` 제공. Add는 새 값을 반환하며 정수 넘침 검사 |
| `SearchRewardItemDefinition.cs` | 수량과 선택적 자원 종류. `ResourceType == null`이면 현재 지역 주 자원 |
| `SearchRewardDefinition.cs` | 단계·D20 구간과 보상 항목 목록. 입력 배열을 복사하고 읽기 전용으로 노출 |
| `ExplorationRules.cs` | 보상 행 선택·합산과 `D20 + 보정 >= DC` 계산. 상태 변경·난수 생성 없음 |
| `IRandomSource.cs` | `NextInt(minInclusive, maxExclusive)` 규약 |
| `GameRandom.cs` | System.Random 래퍼. 기본 생성자와 seed 생성자, 빈/역전 범위 거절 |
| `SurvivorDefinition.cs` | 고정 설정: `ID`, `Name`, `MaxHP`, `CombatModifier`, `TechModifier` |
| `RegionDefinition.cs` | `Id`, 이름, 주 자원, 사용 사건 ID, 선택적 지역 보상표. 목록 복사·읽기 전용 노출 |
| `SurvivorState.cs` | 생존자 ID·현재 HP·생존 여부. 현재는 2장의 참조 오류가 있어 컴파일 불가 |
| `ExpeditionState.cs` | 파견 생존자·지역 ID, 단계, 소음, 임시 전리품. 현재 사건·직전 사건·귀환 정보는 아직 없음 |

생존자·탐색 상태는 getter만 제공하는 불변 객체로 시작했다. 이후 Session이 규칙 계산 결과로 새 상태를 만들어 교체하는 방식으로 연결할 예정이다. 실제 Session은 아직 없다.

`ExpeditionState`는 단계 1 이상·소음 0 이상만 검사한다. 최대 단계·최대 소음과 ID 존재 여부는 이후 게임 설정·콘텐츠를 가진 Session/검증 코드에서 확인해야 한다. 정의와 상태를 생성한다고 출발·사건 추첨·수색이 실행되지는 않는다.

## 5. 유지할 게임·설계 결정

### 5.1 게임의 첫 목표

- PC 싱글, 2D 일러스트와 텍스트·버튼 중심의 운 기반 생존 탐색 게임이다.
- 첫 시제품은 생존자 1명·지역 1곳, 최대 5단계, 일반 사건 `EVENT_001~003`과 귀환 사건 `RETURN_001`을 사용한다.
- 출발 → 단계 진입 사건 → 수색/생략 → 다음 단계/귀환 → 생존 또는 사망 → 전리품 반영/폐기까지 이어지는 탐색 한 번을 먼저 완성한다.
- 마커스·공업 창고를 첫 연결 대상으로 제안한 상태다. 기본 완성본은 생존자 2명·지역 3곳·일반 사건 5개다.
- 기본 완성본의 초기값은 7일, 목표 부품 8개, 초기 식량 6·의약품 1·부품 0, 하루 식량 소비 2개다. 밸런스가 검증된 최종 수치는 아니다.

### 5.2 수색 보상 v0.3

- 일반 수색 D20은 한 번만 굴리고, ResourceAmount 하나로 미발견·단일·복수 보상을 반환한다.
- 기본판은 기존대로 마트=식량, 병원=의약품, 창고=부품 한 종류를 지급한다. 혼합 보상은 지원 구조이며 실제 기본판 지급량을 변경한 것은 아니다.
- 보상 항목의 자원이 null이면 지역 주 자원으로 해석한다. 같은 실제 자원으로 해석되는 항목은 합산한다.
- 미발견 행은 보상 항목이 0개인 행이다. 결과의 세 자원이 모두 0일 때만 미발견이다.
- 지역 보상표가 null일 때 공통표를 사용한다. 지역 표를 지정하면 해당 표 전체를 사용하며, 빈 표·누락 구간을 공통표로 채우지 않는다.
- 기존 세 자원을 섞는 것과 새 자원 종류·장비를 추가하는 것은 다른 변경이다. 현재 ResourceAmount는 딕셔너리가 아니라 세 자원 프로퍼티를 가진 값이다.

기본 보상표의 높은 눈 구간은 깊이에 따라 지급량이 다르다. 실제 수치는 게임 기획서 4.2를 그대로 사용한다. 예시용 혼합 보상표를 기본판 데이터로 옮기지 않는다.

### 5.3 상태·난수·표현 경계

- Core는 UnityEngine·UniTask·HM CodeBase를 참조하지 않는 순수 C#이다.
- 향후 게임 상태 원본은 GameSession이 소유한다. 규칙은 값을 받아 계산하고, Provider는 Unity 연결·수명, UI MVP는 표시·입력을 담당한다.
- Session만 게임 판정용 난수를 소비한다. 주사위 연출용 난수와 분리한다.
- 결과와 보상을 연출 전에 확정한다. 결과 확인·화면 재열기·연출 취소가 재굴림이나 재지급을 만들면 안 된다.
- 잘못된 상태·ID·Revision 입력은 상태와 난수를 소비하지 않아야 한다. 결과 ID·Revision·명령 거절 처리는 아직 구현하지 않았다.
- MVP는 UI에만 적용한다. 필요 없는 Manager·추상화·전역 이벤트를 미리 늘리지 않는다.

## 6. 구현하지 않은 항목과 다음 순서

다음은 작업 제안이며, 이 문서 작성 중 구현하지 않았다.

1. **컴파일 복구:** 2장의 프로퍼티 참조를 맞춘 뒤 Core 컴파일과 Unity Console을 다시 확인한다. 이름 전체를 임의로 리팩터링하지 않는다.
2. **게임 정의·규칙 설정:** 생존자·지역과 공통 보상표·최대 단계·최대 소음 등 현재 판에서 사용할 읽기 전용 데이터를 묶는다. 밸런스 수치를 진행 코드에 고정하지 않는다.
3. **사건 정의와 판정 데이터:** 기회/위험, 즉시/판정/선택형, 행동·효과를 정의한다. 사건 ID마다 C# 클래스를 만들지 않는다.
4. **진행 상태와 GameSession:** `GAME_PHASE`, 상태 원본, Snapshot, 결과, 명령 검증을 필요한 만큼 구현한다. 출발의 실제 단계 진입은 사건 추첨과 연결하며, 임시로 사건을 생략한 별도 게임 규칙을 만들지 않는다.
5. **첫 탐색 검증:** 고정 난수로 세 사건 방식·수색·귀환·사망·전리품 반영/폐기와 중복 입력을 확인한다.
6. **Unity 연결:** 콘텐츠 ScriptableObject → 순수 C# 정의 변환, Provider, HM UI MVP, 최소 버튼·텍스트 화면을 연결한다.
7. **연출과 기본판:** UniTask 주사위 연출·취소·재시작 수명, 다일 정산·치료·승패를 순차적으로 연결한다.

현재 미구현: GameDefinition/게임 규칙 설정, GameRunState, GameSession, 사건 정의·추첨·효과, 귀환 위험, SurvivalRules, 하루 정산·승패, Snapshot/ActionReport/Revision, 콘텐츠 전체 검증, ScriptableObject 데이터, Provider/UI/주사위 연출, 재시작·저장.

저장·불러오기, 장비·특성·재굴림 등은 확장 후보이며 기본판에 자동 추가하지 않는다. Scene·Prefab·Inspector·패키지·프로젝트 설정 변경은 코드 제시 작업과 별도로 범위를 확인한다.

## 7. 검증 기록과 한계

| 구분 | 이번 인수인계 작성 시점 결과 |
|---|---|
| 실제 소스 검토 | Core 11개 파일 및 현재 문서·패키지 설정 확인 |
| 현재 Core C# 컴파일 | PowerShell Add-Type으로 실제 11개 파일을 함께 컴파일. CS1061 두 건으로 실패 |
| 현재 11개 파일의 실행 검사 | 전체 컴파일 실패로 실행하지 않음 |
| Unity 전체 컴파일·Console | 확인하지 않음 |
| EditMode / PlayMode / 빌드 / 실제 플레이 | 수행하지 않음 |
| 코드 파일 수정 | 이번 요청에서는 없음 |

이전 채팅에서는 당시 Core 소스 또는 제시 예정 코드를 메모리에서 컴파일해 보상·난수·상태 검사를 수행했다. 그 성공 기록은 현재 디스크의 11개 파일 전체가 컴파일된다는 뜻이 아니다. 특히 마지막 상태 코드 제시 당시의 검사는 `Id`, `MaxHp` 이름을 사용한 제시 코드를 대상으로 했으며, 현재 실제 선언은 `ID`, `MaxHP`다.

임시 검사 코드는 프로젝트의 테스트 파일로 저장하지 않았다. 새 PC에 재실행 가능한 테스트 모음이 이미 있다고 가정하지 말고, 컴파일 복구 후 필요한 검사를 다시 구성한다.

검증 우선순위는 보상표 경계 6/7·15/16·19/20, 혼합 보상·같은 자원 합산, 지역표 전체 교체, 난수 상한 제외, HP 0/최대치와 입력 배열 수정의 영향이다. Session 구현 후에 중복 지급·잘못된 입력의 난수 미소비·귀환/사망 흐름을 추가한다.

## 8. 협업·코딩 규칙

- 존댓말로 결론부터 간결하게 설명한다. 사실·추측·미검증을 구분한다.
- 사용자는 코드를 직접 입력한다. 검토·설계 요청을 실제 코드 파일 수정 허가로 해석하지 않는다. 명시적으로 직접 수정을 요청하면 그 범위에서 작업한다.
- 작성할 코드는 목적·구조 선택 이유·기존 동작 변화와 함께 작은 단위로 제시한다. 컴파일과 실제 실행 검증은 구분한다.
- 기존 사용자 변경·파일 이동·설정 변경을 임의로 되돌리지 않는다.
- 기본 변수 규칙: private/protected `_example`, 매개변수 `example`, 정적 `s_example`, 상수/readonly `EXAMPLE`. 우선순위는 상수·readonly → 정적 → 접근 제한자다.
- 일반 함수 PascalCase, 코루틴 `_cor`, 실제 비동기 완료 대기/진행 작업 반환은 `_async`, 이벤트 바인딩 `OnExampleActioned`.
- 인터페이스 I 접두사, 추상 클래스 A 접두사, 싱글톤 Provider 접미사. UI MVP는 `_model`, `_view`, `_presenter` 접미사이며 UI에만 적용한다.
- enum 이름·요소는 UPPER_SNAKE_CASE, 구조체는 PascalCase다.
- Unity 참조는 Obj/Trans/Txt/Btn/Img/Input/Drop/Sprite/Animator/RectTrans 등 사용자 지정 접미사를 접근 제한자 규칙과 함께 사용한다.
- 짧은 문장·호출은 한 줄을 유지한다. 긴 문장만 필요할 때 줄바꿈한다. 실제 소스에는 `if ( condition )`, 매개변수 구분 ` , ` 스타일을 사용하고 있다.
- 기존 공개 프로퍼티에는 `ID`/`Id`, `MaxHP`/`CurrentHp`가 섞여 있다. 이번에 전체 명명 통일을 승인한 것은 아니다. 우선 실제 선언과 참조를 일치시킨다.

## 9. 새 PC의 채팅에 붙여 넣을 재개 요청

```text
LastSupply 개발을 이어가려고 합니다.
Docs/LAST_SUPPLY_개발_인수인계_2026-10-15.md와 현재 기획서·코드 설계서 v0.3,
Assets/01_Main/02_Scripts/Core의 실제 파일을 먼저 확인해 주세요.

저는 코드를 직접 입력하면서 진행합니다. 코드 파일을 바로 수정하지 말고,
작성할 코드를 작은 단위로 보여 주고 목적과 변경 이유를 설명해 주세요.

인수인계 시점에는 SurvivorDefinition의 ID/MaxHP와 SurvivorState의 Id/MaxHp 참조가
달라 컴파일 오류가 있었습니다. 현재도 남아 있는지 확인하고 최소 수정안을 먼저 제시해 주세요.
그다음 게임 정의·진행 상태와 GameSession 연결을 준비하는 순서로 이어가 주세요.
이전 채팅의 검사 성공을 현재 프로젝트의 Unity 컴파일·플레이 검증으로 간주하지 마세요.
```
