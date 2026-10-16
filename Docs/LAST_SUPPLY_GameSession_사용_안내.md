# LAST SUPPLY GameSession 사용 안내

> 2026-10-16 / 게임 기획서·코드 설계서 v0.3 기준.
>
> 순수 C# 진행과 콘텐츠 카탈로그·기본 에셋을 구현했다. 데이터 시작점은 [Content 사용 안내](LAST_SUPPLY_Content_사용_안내.md)를 따른다. 이후 추가한 Provider·탐색 UI 연결과 Play Mode 검사 범위는 [Exploration 프로토타입 사용 안내](LAST_SUPPLY_Exploration_프로토타입_사용_안내.md)에 기록했다. 원본 프로젝트 전체 검증·연출·빌드는 별도 작업이다.

## 1. 상태 소유와 시작

GameSession이 날짜·생존자 HP·은신처 자원·활성 탐색·미확인 결과·종료 사유를 소유한다. GetSnapshot은 생존자 배열을 복사하고, 변경 불가능한 상태·결과 객체를 공유한다. 이전 Snapshot은 이후 명령으로 바뀌지 않는다. 별도의 변경 가능한 GameRunState를 중복 원본으로 만들지 않았다.

```csharp
GameDefinition definition = DefaultGameData.CreatePrototype();
var session = new GameSession(definition, new GameRandom());
GameSnapshot displayed = session.GetSnapshot();
bool accepted = session.TryStartExpedition(
    displayed.Revision, definition.Survivors[0].ID, definition.Regions[0].Id, out string reason);
GameSnapshot current = session.GetSnapshot();
```

CreatePrototype과 CreateDefault는 동일한 진행 규칙을 사용한다. 재시작은 같은 정의와 새로운 IRandomSource로 새 GameSession을 만든다. 기존 Session을 일부 초기화하지 않으며 새 게임의 UI 수명·세대 관리는 이후 Provider가 맡는다.

## 2. 외부 명령

모든 명령의 첫 인자는 **화면을 표시할 때 받은 Revision**이다. 성공 시 true와 null 사유를 반환하고 Revision을 한 번 증가시킨다. 거절 시 false와 사유를 반환하며 상태·자원·난수를 소비하지 않는다. 호출 후 현재 상태는 GetSnapshot으로 받는다.

| 명령 | 허용 상태·추가 인자 |
|---|---|
| TryHeal | SHELTER / 생존자 ID |
| TryStartExpedition | SHELTER / 생존자 ID, 지역 ID |
| TryChooseEventAction | EVENT_ACTION / 현재 사건 ID, 행동 ID |
| TrySearch / TrySkipSearch | SEARCH_CHOICE |
| TryAdvanceStage / TryReturn | STAGE_CHOICE |
| TryResolveReturn | RETURN_ACTION / 귀환 사건 ID, 행동 ID |
| TryConfirmResult | RESULT / PendingReport.Id |
| TryConfirmDaySummary | DAY_SUMMARY / DaySummary.Id |

확인 요청 예시:

```csharp
GameSnapshot displayed = session.GetSnapshot();
bool accepted = session.TryConfirmResult(displayed.Revision, displayed.PendingReport.Id, out string reason);
```

호출 직전의 최신 Revision으로 오래된 버튼 요청을 포장하지 않는다. 화면에 표시한 값과 결과 ID를 함께 전달해야 이전 콜백을 거절할 수 있다.

## 3. 결과·귀환·정산

- 일반 사건에서 생존하면 결과 확인 후 수색을 허용한다. 사망하면 수색·귀환 판정 없이 정산한다.
- 수색은 한 번의 D20으로 ResourceAmount 전체를 계산하여 임시 전리품에 한 번 반영한다. 마지막 단계도 수색 또는 생략 후 귀환한다.
- 판정 없는 행동은 주사위를 소비하지 않는다. STAT_TYPE.NONE을 지정한 판정 행동은 보정 0으로 D20을 굴린다.
- 귀환을 시작할 때 ReturnRisk를 고정한다. 발생 확률 0%·100%에는 발생 난수를 쓰지 않는다. 발생하면 별도의 행동 D20을 한 번 굴리고, 실패해도 생존하면 전리품 전체를 가져온다.
- StageEventId는 해결 후에도 현재 단계 사건을 보관한다. 다음 단계의 중복 제외에는 이전 상태의 StageEventId를 전달한다. 사건 없는 단계를 지나면 다음 단계의 직전 사건 ID는 null이다.
- ActionReport는 적용 전후 상태·실제 보상·D20·보정·DC·후속 흐름을 보관한다. Total은 덧셈 오버플로를 피하도록 long?이다. 확인은 효과를 다시 적용하지 않는다.
- 정산은 전리품 반영/폐기 → 전원 사망 → 목표 달성 → 기한 초과 → 식량 소비·부족 피해 → 전원 사망 재확인 순서다. SurvivalRules는 다음 값만 계산하고 Session이 반영한다.
- DAY_SUMMARY에 진입할 때 정산과 날짜 변경은 완료되어 있다. Snapshot.Day는 계속 진행할 다음 날짜이며, 정산 화면의 완료 날짜는 DaySummary.CompletedDay다. 종료한 경우 NextDay는 null이다.
- DaySummary는 회수·상실 전리품, 은신처 자원의 전후 값, 식량 소비·부족 여부, 피해, 생존자 전후 상태와 사망자 ID를 보관한다. 활성 Expedition과 PendingReport는 정산 시 비운다.
- 정산 확인은 다음 날 SHELTER 또는 GAME_OVER로 이동한다. 결과 ID와 정산 ID는 한 Session 안에서 증가하고 확인·화면 재열기로 재발급하지 않는다.

## 4. 재실행 검사

프로젝트 루트의 PowerShell 7에서 실행한다.

```powershell
pwsh -NoProfile -File .\Tests\CoreData\Verify-CoreData.ps1
pwsh -NoProfile -File .\Tests\CoreSession\Verify-CoreSession.ps1
```

첫 검사는 기존 콘텐츠·수색 데이터를 확인한다. 두 번째 검사는 실제 Core와 Tests/CoreSession의 검사 소스를 C# 9로 함께 컴파일한다. 고정 난수로 사건·수색·귀환·사망·정산·치료·승패·중복 입력·거절 입력의 난수 미소비를 검사하고, 100개 seed로 기본판의 게임 종료와 새 Session 재시작을 확인한다. 프로젝트 파일을 변경하지 않는다.

이는 .NET에서의 Core 컴파일·진행 검사다. Unity 전체 컴파일·Console·EditMode/PlayMode·실제 플레이 및 밸런스 검증을 뜻하지 않는다. 이후 추가한 ScriptableObject 변환·에셋의 Unity 배치 검사는 Content 사용 안내에 기록했다. 다음 사용자 작업은 Provider·최소 UI 연결이다.
