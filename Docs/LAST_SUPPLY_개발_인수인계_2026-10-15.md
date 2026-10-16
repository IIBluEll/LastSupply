# LAST SUPPLY 개발 인수인계

> 최초 작성: 2026-10-15 / 최종 갱신: 2026-10-16
>
> 기준: 게임 기획서·코드 설계서 v0.3, 현재 프로젝트 파일, 이전 구현·검증 기록.
>
> **현재 상태:** Core·콘텐츠 데이터·GameSession·은신처 MVP·Exploration 프로토타입 연결 완료. 생존자·지역 선택부터 사건 표시, 수색, 귀환, 일일 정산, 다음 날 또는 게임 종료까지 연결되어 있습니다. 치료와 새 게임은 Core에서 지원하지만 UI 연결은 남아 있습니다.
>
> 이번 갱신은 인수인계 문서만 수정했습니다. 게임 코드·Scene·Prefab·패키지·프로젝트 설정·Git 인덱스는 변경하지 않았으며, 아래 실행 검사는 이번 문서 갱신에서 재실행하지 않았습니다. 기존 문서 링크를 유지하기 위해 파일명의 날짜는 보존합니다.

## 1. 다른 PC·다음 채팅에서 먼저 할 일

1. 이 문서와 [Exploration 프로토타입 사용 안내](LAST_SUPPLY_Exploration_프로토타입_사용_안내.md)를 읽습니다. 예전 안내의 `GameSession 미구현`, `Provider·최소 UI 연결 예정` 문구는 현재 진행 상태가 아닙니다.
2. Git 상태와 최신 커밋을 확인하고, Assets·Packages·ProjectSettings·Docs·Tests 및 `.meta`가 전달되었는지 확인합니다. 스테이징만으로는 다른 PC에 전달되지 않습니다.
3. Unity 6000.3.20f1에서 패키지 복원과 임포트를 완료하고 Console의 컴파일 오류를 확인합니다.
4. `Assets/01_Main/01_Scene/SampleScene.unity`를 열고 기존 Provider의 카탈로그·은신처 View 참조를 확인합니다. Provider는 화면을 닫아도 함께 비활성화되지 않도록 은신처 Panel과 별도 오브젝트에 둡니다.
5. Play에서 생존자와 지역을 선택해 출발합니다. Provider의 `_explorationView`가 비어 있으면 기존 Canvas 아래에 탐색 화면을 생성하고 은신처의 TMP 폰트를 재사용합니다. 기본 프로토타입 사용에 별도의 탐색 View 수동 연결은 필요하지 않습니다.
6. 실제 화면에서 사건 문구·버튼·정산까지 확인한 뒤 5장의 다음 작업을 진행합니다. 이미 완료한 데이터 정의·GameSession·탐색 MVP를 다시 작성하지 않습니다.

첫 단계의 기본 확률은 사건 없음 65%, 기회 20%, 위험 15%입니다. 출발 직후 사건 대신 수색 화면이 나오는 것은 정상입니다. 사건 표시를 위해 재굴림하거나 확률을 임의로 바꾸지 않습니다.

## 2. 완료된 구현과 파일 위치

게임 스크립트 루트는 `Assets/01_Main/02_Scripts`이며 현재 C# 파일은 총 **55개**입니다. 외부 플러그인과 Tests의 검사 코드는 이 수에 포함하지 않습니다.

| 영역 | 파일 수·위치 | 완료한 책임 |
|---|---|---|
| Core | 33개 / `Core` | 읽기 전용 정의·상태, 탐색·생존 규칙, 난수, GameSession·Snapshot·결과·정산 |
| Content | 14개 / `Content` | Config 5개·직렬화 데이터 7개·Editor 도구 2개, 카탈로그 변환·ID 조회·검증 |
| Game | 1개 / `Game/LastSupplyGameProvider.cs` | 카탈로그로 Session 생성, 명령 전달, 화면 전환, Presenter 수명 관리 |
| 은신처 UI | 3개 / `UI/Shelter` | Model·View·Presenter, 생존자·지역 선택, 출발, 상태·거절 사유 표시 |
| 탐색 UI | 4개 / `UI/Exploration` | Model·View·Presenter와 런타임 화면 생성기, 사건부터 정산·종료까지 표시·입력 |

### 2.1 Core와 GameSession

- `GameSession`이 날짜·생존자 HP·은신처 자원·활성 탐색·미확인 결과·종료 사유의 원본을 소유합니다. `GetSnapshot()`으로 읽기 전용 상태를 제공합니다.
- 사건 범주 추첨·후보 필터·직전 단계 사건 중복 제외, 선택형·판정형·즉시 결과형 처리, 수색·생략·다음 단계·귀환·사망을 구현했습니다.
- `ActionReport`에 이미 적용한 효과와 D20·보정·DC·실제 보상·HP/소음 전후 값을 보관합니다. 결과 확인으로 재굴림·재지급하지 않습니다.
- `SurvivalRules`와 일일 정산, 식량 부족 피해, 전원 사망·목표 달성·기한 초과 판정, 치료를 구현했습니다.
- 모든 외부 명령은 `expectedRevision`을 검증하며, 필요한 사건·행동·결과·정산 ID도 확인합니다. 거절된 요청은 상태·자원·게임 난수를 소비하지 않습니다.
- 새 게임은 같은 정의와 새 난수원으로 **새 GameSession을 생성**합니다. 현재 Provider에는 새 게임 UI와 Session 교체 흐름이 없습니다.
- 과거 `SurvivorState` 참조 오류는 해결됐습니다. 현재 `definition.ID`, `definition.MaxHP`를 사용하므로 옛 오류 복구나 전체 명명 변경부터 시작하지 않습니다.

정확한 API·정산 순서는 [GameSession 사용 안내](LAST_SUPPLY_GameSession_사용_안내.md)를 따릅니다. 별도의 변경 가능한 `GameRunState`를 중복 원본으로 추가하지 않습니다.

### 2.2 콘텐츠와 실제 에셋

`GameContentCatalog.ToDefinition()`으로 ScriptableObject에서 순수 C# 정의를 생성합니다. 생존자·지역 ID 조회와 콘텐츠 참조·표 누락·중복·수치 검증을 제공합니다.

`Assets/01_Main/04_Resource/Content`에 실제 기본 에셋 **12개**가 있습니다.

| 폴더 | 구성 |
|---|---|
| Rules | `DefaultGameConfig.asset` 1개 |
| Survivors | 마커스·사라 2개 |
| Regions | 마트·병원·창고 3개 |
| Events | `EVENT_001`~`EVENT_005` 5개 |
| Catalogs | `DefaultGameContent.asset` 1개 |

귀환 사건 `RETURN_001`은 GameConfig 안의 데이터이며 별도 EventConfig 에셋이 아닙니다. 초상화·지역 배경 Sprite 참조는 비어 있습니다.

Unity의 `Last Supply > Content` 메뉴에서 기본 에셋 생성·기본/선택 카탈로그 검증·회귀 검사를 실행할 수 있습니다. `Create Default Assets`는 대상 에셋 또는 `.meta`가 하나라도 존재하면 생성을 거절하므로 기존 데이터 갱신 도구로 사용하지 않습니다. `Run Content Checks`는 제공된 기본판과 수치가 같은지도 검사합니다. 의도적으로 밸런스를 바꾼 카탈로그는 구조 검증과 구분합니다.

`DefaultGameData.CreatePrototype()`과 `CreateDefault()`도 유지합니다. 전자는 마커스·창고·일반 사건 3개, 후자는 생존자 2명·지역 3곳·일반 사건 5개이며 두 구성은 같은 규칙을 사용합니다. 현재 Unity Provider의 시작 데이터는 연결된 카탈로그입니다.

### 2.3 Provider·은신처·탐색 화면

- 은신처는 살아 있는 생존자와 지역을 선택해 출발합니다. Dropdown의 안내 항목은 미선택이며 실제 항목은 인덱스 1부터입니다.
- Provider가 Session 명령을 전달하고 성공 후 현재 상태에 맞게 두 화면을 전환합니다. 두 UI의 Presenter는 표시·입력만 담당합니다.
- `ExplorationPrototypeUI`는 기존 Canvas와 폰트로 최소 화면을 생성합니다. 나중에 직접 구성한 `ExplorationPanel_view`와 참조를 연결하면 그 화면을 사용합니다.
- Provider와 두 Panel은 서로를 끄지 않는 계층이어야 합니다. Provider를 껐다 켜면 현재 Session과 Revision을 유지하며 화면을 복구합니다. 종료 시 Provider가 생성한 탐색 Panel만 정리합니다.
- 탐색 구현에서는 원본 Scene·Prefab·콘텐츠 수치·프로젝트 설정을 수정하지 않았습니다. 현재 Git에 있는 기존 Scene 수정과 폰트/TMP 추가 상태는 그대로 보존합니다.

| 상태 | 현재 화면에서 가능한 일 |
|---|---|
| EVENT_ACTION | 일반 사건 제목·설명과 실제 행동 수에 맞는 버튼 1~3개 표시 |
| SEARCH_CHOICE | 수색 또는 생략 |
| STAGE_CHOICE | 다음 단계 또는 귀환 |
| RETURN_ACTION | 귀환 사건·행동·실패 피해 표시와 판정 요청 |
| RESULT | 확정된 결과·보상·HP/소음 변화 표시와 결과 확인 |
| DAY_SUMMARY | 완료 날짜·전리품 회수/상실·식량 정산·사망 정보 표시와 정산 확인 |
| GAME_OVER | 실제 종료 사유 표시 |

즉시 결과형 사건은 Core에서 효과를 적용한 뒤 곧바로 RESULT로 갑니다. 결과 확인과 정산 확인은 별도 버튼이며, 숨겨진 이전 버튼 입력이 다음 상태를 진행시키지 않도록 처리했습니다. 현재는 동기식 프로토타입이며 새 판은 Play를 다시 실행합니다.

## 3. 유지할 규칙과 구현 경계

- Core는 UnityEngine·UniTask·HM CodeBase에 의존하지 않는 순수 C#입니다. Content는 편집용 데이터 변환, Provider는 Unity 연결, UI MVP는 표시와 입력을 맡습니다. MVP를 게임 규칙 전체로 확대하지 않습니다.
- 게임 판정 난수는 Session에서 소비합니다. UI가 별도로 굴리거나 계산하지 않습니다. D20은 `NextInt(1, 21)`, 확률 표본은 `NextInt(0, 100)`이며 상한은 제외합니다.
- Presenter는 화면에 **표시한 Snapshot.Revision과 ID**로 요청합니다. 오래된 버튼을 호출 직전 최신 Revision으로 포장하지 않습니다.
- 수색은 단계당 최대 1회이며 보정·자체 비용·자체 소음 없이 D20 한 번으로 전체 보상 묶음을 정합니다. 같은 자원은 합산하고, 세 자원이 모두 0일 때만 미발견입니다.
- 지역 전용 수색표가 있으면 공통표를 완전히 대체합니다. 누락된 구간을 공통표로 보충하지 않습니다. 기본판은 지역 주 자원 보상이며 개발용 혼합 보상을 임의로 섞지 않습니다.
- 사건 범주와 처리 방식은 독립적입니다. `Difficulty == null`은 판정 없음, 판정 행동의 `STAT_TYPE.NONE`은 능력치 보정 0입니다.
- 일반 사건에서 사망하면 수색·귀환 판정을 하지 않습니다. 마지막 단계에서도 수색 또는 생략 후 귀환합니다.
- 귀환 사건 발생과 행동 실패는 별도 판정입니다. 실패해도 생존하면 전리품 전체를 회수하며, 사망하면 폐기합니다. 귀환 실패 피해의 원본은 ReturnRiskDefinition입니다.
- 정산 순서는 전리품 반영/폐기 → 전원 사망 → 목표 달성 → 기한 초과 → 식량 소비·부족 피해 → 전원 사망 재확인입니다. 계속 진행하면 날짜를 넘깁니다.
- DAY_SUMMARY 진입 시 정산은 끝나 있습니다. 표시할 완료 날짜는 `DaySummary.CompletedDay`이며 이미 다음 날인 `Snapshot.Day`와 구분합니다. 정산 확인에서 효과를 다시 적용하지 않습니다.
- 연출을 추가해도 이미 확정된 결과를 보여 줍니다. 화면 재열기·취소·재시작으로 재굴림·중복 보상이 생기면 안 됩니다.

기본 규칙의 초기값은 최대 5단계·소음 6·7일, 자원 6/1/0, 목표 부품 8, 하루 식량 2, 부족 피해 2, 의약품 1당 HP 3 회복입니다. 재미·밸런스 검증으로 확정한 수치는 아닙니다. 상세 표는 [Content 사용 안내](LAST_SUPPLY_Content_사용_안내.md)와 실제 에셋을 확인합니다.

## 4. 검증 기록과 재실행

다음은 **이전 구현 작업에서 실행한 기록**입니다. 이번 갱신에서는 파일·문서·환경·Git 상태를 대조했으며, 컴파일·Play Mode 검사를 다시 실행하지 않았습니다.

| 검증 | 이전 실행 결과와 범위 |
|---|---|
| 게임 소스 컴파일 | 게임 C# 55개를 실제 Unity/TMP/uGUI 모듈과 HM MVP 소스로 C# 9 컴파일 성공 |
| Core 데이터 | 392개 assertion 통과. 기본판·시제품·수색 300가지·오류 입력·읽기 전용 데이터 등 |
| Session | 진입 45개·사건 46개·진행 156,933개 assertion 통과. 100개 seed의 게임 종료·새 Session 재시작 포함 |
| Content / Unity | 임시 프로젝트에서 에셋 생성·저장·재임포트 후 검사, 새 Unity 프로세스에서 재로드 검사. 각 1,396개 assertion 통과 |
| 콘텐츠 에셋 전달 | Unity 생성 에셋과 `.meta` 복사 시 해시 일치·참조 GUID 연결·중복 여부 확인 |
| Exploration / Unity Play Mode | 별도 임시 프로젝트에서 100개 seed·8,338개 assertion 통과. 실제 Dropdown/Button의 `onValueChanged`/`onClick` 호출 |

탐색 검사는 마커스·창고 조합으로 사건 세 방식, 수색·추가 탐색·귀환·정산·다음 날/종료, 사건 문구와 결과 값, 오래된 Revision 거절, 숨겨진 이전 버튼, Provider 비활성화/재활성화를 확인했습니다. 실제 게임 소스와 기본 데이터·폰트·uGUI/TMP·HM MVP 소스를 사용했지만 **원본 프로젝트의 모든 패키지와 원본 Scene을 통째로 실행한 검사는 아닙니다.**

직접 마우스 입력, 레이아웃 육안 검수, 모든 생존자·지역 조합의 UI 실행, 원본 프로젝트 전체 Play Mode·플레이어 빌드, 재미·밸런스는 별도 확인이 필요합니다. 사용자는 앞선 은신처 연결 단계의 실행이 잘 된다고 보고했으며, 이를 최신 탐색 화면 전체의 수동 검증으로 확대하지 않습니다.

이전 검증의 원시 로그는 Temp 아래에 생성됐으며 이번 확인에서는 해당 로그가 남아 있지 않았습니다. 위 수치는 기존 검증 기록이며 현재 로그를 다시 열어 확인한 결과가 아닙니다. 필요한 경우 아래 도구로 재생성합니다.

프로젝트 루트의 PowerShell 7에서 실행합니다.

```powershell
pwsh -NoProfile -File .\Tests\CoreData\Verify-CoreData.ps1
pwsh -NoProfile -File .\Tests\CoreSession\Verify-CoreSession.ps1
pwsh -NoProfile -File .\Tests\Exploration\Verify-Exploration.ps1
```

Unity 경로가 다르면 다음처럼 지정합니다.

```powershell
pwsh -NoProfile -File .\Tests\Exploration\Verify-Exploration.ps1 -UnityPath 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'
```

Core 두 검사는 .NET에서 실제 Core를 컴파일·검사합니다. 탐색 검사는 `Temp/ExplorationVerification-*`에 별도 프로젝트를 만들고 Unity Play Mode를 실행합니다. 기존 `Library/PackageCache`의 HM/uGUI 캐시와 `Assets/TextMesh Pro`, 프로젝트 폰트가 필요하므로 새 PC에서는 먼저 Unity 패키지 복원·임포트를 완료해야 합니다. 검사 Scene과 로그는 임시 프로젝트 안에 만들며 원본 Scene·패키지·설정을 덮어쓰지 않습니다.

Content 검사는 Unity 메뉴 `Last Supply > Content > Run Content Checks`로 실행할 수 있습니다. 배치 진입점은 `LastSupply.Content.Editor.ContentVerification.Run`입니다.

## 5. 남은 작업과 권장 순서

| 순서 | 다음 작업 | 완료 기준 |
|---|---|---|
| 1 | 원본 SampleScene에서 직접 플레이 | 선택·출발·사건·수색/생략·추가 탐색/귀환·결과·정산·종료를 실제 입력으로 확인하고 문구·버튼 크기·폰트 잘림 점검 |
| 2 | 은신처 치료와 새 게임 UI | 기존 Core의 TryHeal 사용, 새 Session 생성과 UI 갱신 연결. 종료 후 Play 재실행 없이 새 판 시작 |
| 3 | 탐색 View 구성 보완 | 필요하면 수동 구성한 View로 교체, 상태별 가독성과 버튼 배치 확인. 현재 런타임 생성기는 프로토타입용 |
| 4 | UniTask 주사위·보상 연출 | 결과는 미리 확정하고 연출만 비동기로 처리. 화면 닫힘·비활성화·재시작의 취소와 이전 콜백 무효화 확인 |
| 5 | 반복 플레이와 수치 조정·빌드 | 자원 수급·생존 일수·위험 선택·진행 속도를 기록하고 기본값 조정, 대상 PC 플레이어 빌드 확인 |

**바로 다음 작업은 1번 원본 화면 확인입니다.** 확인 후 기능 구현을 이어간다면 치료·새 게임 UI가 우선입니다. 치료 규칙과 승패·다일 정산은 이미 Core에 있으므로 새로 계산하지 않습니다. 현재 Provider에는 TryHeal 전달 함수도 없으므로 치료 연결 때 함께 추가해야 합니다.

신규 일러스트, 저장/로드, 장비·특성·재굴림 등은 현재 완료 범위 밖이며 별도 요청 없이 추가하지 않습니다.

## 6. 환경·Git·PC 이동

| 항목 | 2026-10-16 문서 갱신 시 확인 |
|---|---|
| 현재 프로젝트 루트 | `D:/Fork/LastSupply` — 이전 PC 경로를 코드에 고정하지 않습니다 |
| Unity | `6000.3.20f1` |
| Git | `main`, HEAD `48963cc` / GameSession 기반 탐색·생존 진행 로직 구현 |
| 실행 Scene | `Assets/01_Main/01_Scene/SampleScene.unity` |
| HM CodeBase | Git UPM, 현재 캐시 package.json `1.0.1` |
| HM lock 해시 | `fb08effd5ffc8afab1d4e428c7a1ca9574c3d504` |
| UniTask | `Assets/Plugins/UniTask`, `2.5.11` |
| uGUI / Test Framework | manifest `2.0.0` / `1.6.0` |

갱신 시작 시 Game/UI, 폰트·TMP, Scene, 관련 문서·검사 등 추가 136개·수정 4개 경로가 스테이징되어 있었습니다. **현재 HEAD에 최신 탐색 구현이 모두 들어 있다고 간주하지 않습니다.** 이 문서 갱신분은 추가 작업 트리 변경으로 남깁니다. 이후 사용자가 Git을 조작하면 이 스냅샷은 달라질 수 있습니다.

원격과의 동기화·푸시 여부는 확인하지 않았습니다. 이번 요청에서 스테이징·커밋·푸시는 수행하지 않았으며 기존 인덱스와 사용자 변경을 보존합니다.

- Git으로 이동할 때는 필요한 변경의 커밋·푸시와 새 PC 반영을 확인합니다. 검사 코드와 문서도 포함합니다.
- 파일 복사로 이동할 때는 Assets·Packages·ProjectSettings·Docs·Tests와 모든 필요한 `.meta`를 함께 전달합니다. 콘텐츠 에셋뿐 아니라 한글 폰트·TMP 리소스도 필요합니다.
- Library·Temp·Obj·Logs는 소스 전달물과 구분하며 새 PC에서 재생성합니다. 임시 검증 로그를 장기 보관하려면 Temp 밖에 별도로 보관합니다.
- UniTask는 Assets 플러그인이므로 UPM으로 중복 설치하지 않습니다. HM CodeBase는 manifest와 lock을 기준으로 복원합니다.
- 새 PC의 기존 변경을 덮어쓰기 전에 차이를 확인합니다. 임포트 후 Console·검사 도구·실제 화면을 각각 확인합니다.

## 7. 협업과 수정 권한

사용자는 데이터와 GameSession 직접 구현을 위임했고, 은신처 UI는 코드 안내를 따라 작업했습니다. 이후 Exploration 프로토타입과 사건 표시까지 에이전트의 직접 구현을 명시적으로 요청하여 해당 연결을 완료했습니다. 사용자가 원래 인게임 로직·UI를 담당하려던 역할 분담과 이후 특정 작업 위임을 함께 고려합니다.

이전 위임을 이후 모든 UI·기능 변경의 포괄 허가로 확대하지 않습니다. 현재 요청은 인수인계 문서 갱신입니다. 질문·코드 검토·설계 논의는 파일 수정 허가가 아니며, 직접 수정 요청이 있을 때 해당 범위만 변경합니다. 코드 권한을 Scene·Prefab·Inspector·패키지·프로젝트 설정 변경 허가로 확대하지 않습니다.

구현 시 기존 사용자 변경과 Git 스테이징을 보존하고 단순한 구조를 우선합니다. 범위 밖 리팩터링·이동·이름 변경은 먼저 제안합니다. 답변은 존댓말로 결론부터 말하고 코드 검토·컴파일·실행 검증을 구분합니다.

C#·Unity 컨벤션은 사용자의 AGENTS.md 지시를 우선합니다. 핵심은 private/protected `_exampleVariable`, 매개변수 `exampleVariable`, 정적 `s_exampleVariable`, 상수/readonly `EXAMPLE_VARIABLE`, 이벤트 바인딩 `OnExampleActioned`, 비동기 대기 함수 `_async`, UI에만 MVP 적용, 열거형 이름·요소 UPPER_SNAKE_CASE입니다. Unity 참조의 Obj/Trans/Txt/Btn/Img/Drop 등 접미사를 함께 사용합니다. 짧은 문장·호출은 한 줄로 유지하고 실제 선언의 ID/Id·MaxHP/CurrentHp를 임의로 통일하지 않습니다.

## 8. 참고 문서와 다음 채팅용 요청

- [게임 기획서](LAST_SUPPLY_게임_기획서.md): v0.3 게임 규칙과 수치.
- [코드 설계서](LAST_SUPPLY_코드_설계서.md): 책임 경계와 현재 구현 상태.
- [GameSession 사용 안내](LAST_SUPPLY_GameSession_사용_안내.md): 명령·Revision·결과·정산 규약.
- [Content 사용 안내](LAST_SUPPLY_Content_사용_안내.md): 에셋·변환·생성/검증 메뉴와 기본 수치.
- [Exploration 프로토타입 사용 안내](LAST_SUPPLY_Exploration_프로토타입_사용_안내.md): 현재 실행 방법·화면 구성・UI 검증 범위.
- [Core 스크립트 설명서](LAST_SUPPLY_Core_스크립트_설명서.md)와 [데이터 구현 안내](LAST_SUPPLY_데이터_구현_안내.md): 초기 데이터 구현 당시 설명. 최신 파일 수·진행 상태는 이 인수인계서와 후속 사용 안내를 우선합니다.

```text
LastSupply 개발을 이어가려고 합니다.
Docs/LAST_SUPPLY_개발_인수인계_2026-10-15.md와
Exploration 프로토타입 사용 안내, 기획서·코드 설계서 v0.3,
현재 Git 상태와 실제 코드를 먼저 확인해 주세요.

현재 Core·Content·GameSession·은신처 MVP·Exploration 프로토타입이 있습니다.
생존자·지역 선택 → 사건 → 수색/생략 → 추가 탐색/귀환 → 결과 → 정산 → 다음 날/종료가 연결돼 있습니다.
기본 콘텐츠 에셋 12개와 게임 C# 55개가 있으며, 탐색 화면은 기존 Canvas 아래에 런타임으로 생성됩니다.
첫 단계에 사건이 없으면 수색 화면이 나오는 것이 정상입니다.

이전 별도 Unity Play Mode 검사에서 100개 seed·8,338개 assertion을 통과했지만
원본 Scene의 수동 UI·레이아웃·전체 패키지 통합·빌드는 별도 확인이 필요합니다.
다음은 원본 화면 확인이며, 이후 치료·새 게임 UI와 연출이 남아 있습니다.
치료·다일 정산·승패는 Core에 이미 있으므로 다시 구현하지 마세요.

우선 현재 상태와 다음 작업을 설명해 주세요.
실제 코드 수정은 제가 직접 요청한 범위만 진행하고 Scene·Prefab·설정 변경으로 확대하지 마세요.
```
