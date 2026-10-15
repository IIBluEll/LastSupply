# LAST SUPPLY 데이터 구현 안내

> 2026-10-15 / 게임 기획서·코드 설계서 v0.3 기준
>
> Core의 고정 설정과 콘텐츠 데이터를 작성한 상태다. GameSession·진행 규칙·Unity 연결의 구현 완료를 뜻하지 않는다.

Core 전체 23개 파일의 역할·주요 데이터·함수·연결 관계는 [Core 스크립트 설명서](LAST_SUPPLY_Core_스크립트_설명서.md)를 참고한다.

## 1. 사용할 데이터

`Assets/01_Main/02_Scripts/Core/Data/DefaultGameData.cs`에서 생성한다.

```csharp
GameDefinition prototype = DefaultGameData.CreatePrototype();
GameDefinition defaultGame = DefaultGameData.CreateDefault();
```

| 생성 함수 | 포함하는 콘텐츠 |
|---|---|
| `CreatePrototype()` | 마커스 1명, 공업 창고 1곳, EVENT_001~003, RETURN_001 |
| `CreateDefault()` | 생존자 2명, 지역 3곳, EVENT_001~005, RETURN_001 |

두 구성 모두 같은 기본 규칙과 수색표를 사용한다. 시제품은 콘텐츠 수만 줄이며 확률·귀환·보상 규칙을 바꾸지 않는다. 호출할 때마다 독립된 정의를 생성한다.

`DefaultGameData`는 Unity 연결 전 Core 구현·검사에 쓰는 코드 기반 기본 데이터다. 최종 콘텐츠 경로는 설계대로 ScriptableObject 카탈로그에서 GameDefinition으로 변환하는 방식이며, 그 변환·에셋·그림 참조는 아직 만들지 않았다. 이 파일을 이유로 JSON이나 Resources 로딩으로 콘텐츠 설계를 변경하지 않는다.

## 2. 정의별 역할

| 파일 | 역할 |
|---|---|
| `Definitions/GameRulesDefinition.cs` | 단계·소음·일수 상한, 초기 자원, 목표 부품, 식량·치료 수치, 수색표, 사건 확률표, 귀환 설정 |
| `Definitions/StageEventChanceDefinition.cs` | 한 단계의 사건 없음·기회·위험 확률. 합계 100% |
| `Definitions/ReturnRiskDefinition.cs` | 소음 구간, 위험 표시 이름, 귀환 사건 발생 확률, 실패 피해 |
| `Definitions/ReturnEventDefinition.cs` | 귀환 사건의 ID·문구·행동·능력치·DC. 피해를 중복 저장하지 않음 |
| `Definitions/GameDefinition.cs` | 사용할 생존자·지역·일반 사건과 공통 규칙을 묶은 읽기 전용 정의 |

기존 GameRulesDefinition의 3개 인자 생성자는 전체 규칙을 받는 생성자로 확장했다. 생성 예시는 DefaultGameData.CreateRules에 있다. 기존 공개 이름인 SurvivorDefinition.ID/MaxHP와 그 외 Id는 유지했다.

GameRunState·GameSnapshot·ActionReport·Revision은 진행 로직과 함께 작성할 대상이다. 이번 데이터 작업에서 임의의 상태 전이나 결과 형식을 확정하지 않았다.

## 3. 데이터 검사

생성 시 다음 오류를 예외로 거절한다.

- 음수·범위 밖 설정, 사건 범주 확률 합계 오류.
- 단계별 확률표의 누락·중복, 귀환 소음 구간의 누락·중복·상한 초과.
- 공통·지역별 수색표의 단계/D20 구간 누락·중복·상한 초과. 비어 있는 지역표도 거절하며 공통표로 보충하지 않는다.
- null 정의, 같은 종류의 콘텐츠 ID 중복, 존재하지 않거나 중복된 지역 사건 참조.
- 일반 사건 목록의 귀환 사건 ID, 최대 단계를 초과하는 일반 사건.
- 각 지역·단계에서 기회 또는 위험 사건 후보 누락.

예외 메시지는 개발용이다. Unity 연결 시 시작 오류로 표시할 경계는 별도로 작성한다. 데이터 검사는 게임 난수를 생성하거나 진행 상태를 바꾸지 않는다.

## 4. 재실행할 검사

프로젝트 루트에서 PowerShell 7로 실행한다.

```powershell
pwsh -NoProfile -File .\Tests\CoreData\Verify-CoreData.ps1
```

실제 Core 파일을 함께 컴파일하고 기본판·시제품 구성, 기획 수치·사건 효과, 세 지역의 수색 결과 300가지, 잘못된 데이터 거절, 배열 복사·읽기 전용 노출, 개발용 혼합 지역표를 검사한다. 검사 스크립트는 프로젝트 파일을 변경하지 않는다.

Unity 전체 컴파일·Play Mode·빌드, 사건 추첨·상태 전이·사망·전리품 정산·중복 입력 방지는 이 검사의 범위가 아니다. 다음 작업은 이 데이터를 소비하는 순수 C# 탐색 규칙과 GameSession을 사용자와 함께 작성하는 것이다.
