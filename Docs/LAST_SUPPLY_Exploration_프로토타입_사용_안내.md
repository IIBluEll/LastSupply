# LAST SUPPLY Exploration 프로토타입 사용 안내

> 2026-10-16 / 기존 은신처 MVP와 Core 진행 규칙을 유지하고 탐색 UI를 연결했습니다.
> 이번 화면은 동기식 프로토타입이며, 주사위 애니메이션이나 신규 그림은 포함하지 않습니다.

## 1. 실행

기존 SampleScene에서 Play를 실행하고 은신처에서 생존자·지역을 선택한 뒤 출발합니다.
Provider가 기존 Canvas 아래에 ExplorationPanel을 런타임으로 생성하므로 추가 Inspector 연결 없이 사용할 수 있습니다.
폰트는 은신처의 TMP Text에 연결된 폰트를 재사용합니다.

원본 Scene·Prefab·데이터 에셋·패키지·프로젝트 설정 파일은 이번 구현에서 변경하지 않았습니다.
런타임 Panel은 Play 중 Hierarchy에서 확인할 수 있습니다.

기본 데이터의 첫 단계는 사건 없음 65%, 기회 20%, 위험 15%입니다.
사건이 없으면 바로 수색 선택 화면을 표시하며, 추가 단계에서 다시 사건을 추첨합니다.
화면 표시를 위해 사건이나 주사위 결과를 강제로 바꾸지 않습니다.

## 2. 연결된 흐름

| 상태 | 표시와 입력 |
|---|---|
| EVENT_ACTION | 일반 사건 제목·설명, 실제 행동 수만큼 버튼 1~3개 |
| SEARCH_CHOICE | 수색 또는 수색 생략 |
| STAGE_CHOICE | 다음 단계 또는 귀환 |
| RETURN_ACTION | 귀환 사건 설명·판정 행동·실패 피해 |
| RESULT | 이미 확정된 D20·보정·DC·성공 여부·획득·HP와 소음 전후 값, 결과 확인 |
| DAY_SUMMARY | 완료한 날짜·회수와 상실·식량 소비와 부족·사망자 수, 정산 확인 |
| GAME_OVER | 승리·전원 사망·기한 초과 중 실제 종료 사유 |

HUD는 지역·날짜·단계·생존자 HP·소음·귀환 사건 확률·임시 전리품을 표시합니다.
일반 사건의 선택형·판정형·즉시 결과형을 기존 데이터로 처리합니다.
즉시 결과형은 Core가 진입 시 효과를 적용한 뒤 곧바로 결과를 표시합니다.

결과 확인은 추가 보상 지급이나 재굴림을 하지 않습니다.
정산은 이미 끝난 상태를 표시하며, 정산 확인 후 다음 날 은신처 또는 종료 화면으로 이동합니다.
현재 프로토타입에는 새 게임 버튼과 은신처 치료 UI가 없으며, 새 판은 Play를 다시 실행해 시작합니다.

## 3. 파일 책임

| 파일 | 역할 |
|---|---|
| UI/Exploration/ExplorationModel_model.cs | Snapshot에서 표시할 생존자·지역·사건·결과와 입력 조건 준비 |
| UI/Exploration/ExplorationPanel_view.cs | 텍스트·버튼 표시, UI 입력 이벤트 전달, 리스너 해제 |
| UI/Exploration/ExplorationPresenter_presenter.cs | 상태별 화면 구성, 표시된 Revision·사건·행동·결과 ID를 Provider에 전달 |
| UI/Exploration/ExplorationPrototypeUI.cs | 기존 Canvas와 폰트를 사용하는 최소 런타임 화면 생성 |
| Game/LastSupplyGameProvider.cs | Session 명령과 성공 후 화면 전환, 두 Presenter의 수명 관리 |

Core의 GameSession·규칙 계산·콘텐츠 수치는 변경하지 않았습니다.
UI에 별도의 주사위나 보상 계산을 추가하지 않았습니다.

Provider의 _explorationView가 비어 있으면 기본 화면을 생성합니다.
나중에 직접 만든 ExplorationPanel_view를 연결하면 그 화면을 사용합니다.
이 경우 해당 View의 텍스트·행동 버튼 3개·진행 버튼 참조를 연결해야 합니다.

Provider와 두 Panel은 서로 활성화를 방해하지 않는 계층에 두어야 합니다.
기존 은신처와 탐색 화면의 전환은 Provider와 Presenter를 통해 수행합니다.
Provider를 껐다 켜면 새 Session을 만들지 않고 현재 상태의 화면을 다시 엽니다.
Provider 종료 시 직접 생성한 탐색 Panel만 정리합니다.

## 4. 입력 처리

- Presenter는 화면에 표시된 Snapshot.Revision으로 요청합니다.
- 일반 사건·귀환은 표시한 사건과 행동 ID를 함께 전달합니다.
- 결과와 정산 확인 버튼을 별도 오브젝트로 구성하여 이전 버튼 입력이 다음 확인으로 해석되지 않게 했습니다.
- 숨겨진 버튼의 이벤트를 다시 호출해도 상태를 바꾸지 않습니다.
- 화면이 열려 있을 때만 이벤트를 구독하며, 닫힘·비활성화·종료 시 해제합니다.
- 잘못된 요청은 최신 Snapshot과 Core의 거절 사유를 표시합니다.

## 5. 실행한 검증

Unity 6000.3.20f1에서 복사한 임시 프로젝트로 실제 Play Mode를 실행했습니다.
원래 열려 있던 작업용 Unity와 Scene은 그대로 두었습니다.

- 같은 Core·Content·Game·UI 소스와 실제 기본 콘텐츠 에셋, uGUI/TMP 및 HM MVP 소스를 사용했습니다.
- 검사 Scene에서 은신처 선택부터 실제 onValueChanged/onClick 이벤트를 호출했습니다.
- 100개 seed로 사건·수색·추가 탐색·귀환·결과·정산·은신처 복귀 또는 종료까지 진행했습니다.
- 8,338개 assertion이 통과했습니다.
- 선택형·판정형·즉시 결과형 사건 모두의 제목·설명·버튼 수와 결과 값을 확인했습니다.
- 숨겨진 이전 버튼 재호출·오래된 Revision·Provider 비활성화와 재활성화를 확인했습니다.
- Core 데이터 검사 392개, Session 진입 45개·사건 46개·진행 156,933개 assertion도 통과했습니다.

프로토타입 UI 이벤트 연결과 상태 전환 검증이며, 원본 프로젝트 전체의 Play Mode·직접 마우스 입력·레이아웃 육안 검수·플레이어 빌드·재미와 밸런스 검증을 뜻하지 않습니다.
검사 도구의 최초 실행은 임시 에디터 검색 초기화 예외 때문에 중단됐고, Editor update 기반으로 검사 진입을 바꾼 뒤 통과했습니다.

## 6. 검사 재실행

프로젝트 루트의 PowerShell 7에서 실행합니다.

```powershell
pwsh -NoProfile -File .\Tests\Exploration\Verify-Exploration.ps1
```

필요하면 -UnityPath로 Unity.exe 경로를 지정합니다.
기존 Library/PackageCache의 uGUI와 HM 소스, Assets/TextMesh Pro 및 프로젝트의 한글 폰트가 필요합니다.
도구는 Temp/ExplorationVerification-*에 별도 프로젝트를 만들고, 그 프로젝트에서만 Scene을 생성하고 Play Mode를 실행합니다.
기존 데이터나 Scene을 덮어쓰지 않습니다. 로그는 임시 프로젝트 안에 남깁니다.

검사 소스는 Tests/Exploration/ExplorationPlayModeChecks.cs에 있으며, 배치 검사 전용입니다.

## 7. 다음 작업

1. 기존 Scene에서 실제 선택·사건 문구·버튼 크기와 가독성 확인.
2. 필요하면 직접 만든 탐색 View로 런타임 프로토타입 레이아웃 교체.
3. 은신처 치료·종료 후 새 게임 UI 연결.
4. UniTask 주사위·보상 연출과 취소 수명 처리.
5. 플레이 기록을 보고 수급·난이도·진행 속도 조정.
