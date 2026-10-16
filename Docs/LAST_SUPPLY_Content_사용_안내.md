# LAST SUPPLY Content 사용 안내

> 2026-10-16 / 게임 기획서·코드 설계서 v0.3 기준.
>
> 카탈로그, 기본 데이터 에셋 12개, ID 조회, 생성·검증 메뉴를 구현했습니다.
> 사용자 담당 범위는 Unity 인게임 연결·Provider·UI·연출입니다.

## 1. 시작할 에셋

`Assets/01_Main/04_Resource/Content/Catalogs/DefaultGameContent.asset`을 사용합니다.
기본 수치와 사건 참조는 입력된 상태입니다. 정상 사용을 위해 생성 메뉴를 다시 실행할 필요는 없습니다.

| 위치 | 에셋 |
|---|---|
| Rules | DefaultGameConfig 1개 |
| Survivors | SURVIVOR_MARCUS, SURVIVOR_SARA |
| Regions | REGION_MART, REGION_HOSPITAL, REGION_WAREHOUSE |
| Events | EVENT_001~005 |
| Catalogs | DefaultGameContent 1개 |

귀환 사건 RETURN_001은 일반 사건 에셋이 아니라 GameConfig 내부의 ReturnEventData에 들어 있습니다.
초상화·배경 Sprite는 비어 있습니다. UI용 그림을 준비한 뒤 해당 Config에 지정하면 됩니다.
새 그림 제작·Scene·Prefab·UI 연결은 이번 데이터 작업에 포함하지 않았습니다.

## 2. 코드 구성과 책임

```text
Assets/01_Main/02_Scripts/Content/
  Configs/  GameConfig, SurvivorConfig, RegionConfig, EventConfig, GameContentCatalog
  Data/     수색 보상, 사건 효과·행동, 사건 확률, 귀환 설정
  Editor/   DefaultContentAssets, ContentVerification
```

- Config는 Inspector에서 설정하고 ToDefinition으로 Core 정의를 만듭니다.
- GameContentCatalog는 변환과 전체 콘텐츠 검증을 묶습니다. 진행 상태와 난수는 보관하지 않습니다.
- 지역에서 참조한 사건은 같은 에셋이 카탈로그의 일반 사건 목록에 등록되어 있어야 합니다. 다른 에셋의 ID만 같아도 거절합니다.
- Core의 GameDefinition이 ID 중복, 사건 후보, 보상·확률·귀환 표 누락과 중첩을 검사합니다.
- 변환 오류에는 해당 에셋 이름·ID나 카탈로그 이름을 붙입니다.
- 모든 게임 상태는 기존 GameSession이 소유합니다. 런타임 HP와 전리품을 Config에 기록하지 않습니다.
- Editor 하위 코드는 UnityEditor 전용입니다. 플레이어 런타임에 데이터 생성 코드를 넣지 않습니다.

## 3. 인게임에서 연결할 API

Provider에서 카탈로그를 직렬화 참조로 받고, 시작 시 한 번 정의로 변환하면 됩니다.
다음은 연결 예시이며 실제 Provider 구현은 사용자 담당입니다.

```csharp
// Provider의 시작 처리 내부. _contentCatalog는 GameContentCatalog 참조입니다.
GameDefinition definition = _contentCatalog.ToDefinition();
var session = new GameSession(definition, new GameRandom());
GameSnapshot snapshot = session.GetSnapshot();
```

ToDefinition은 설정 오류가 있으면 ArgumentException을 던집니다.
변환에 성공한 뒤 Session을 만들고 UI 입력을 허용하세요. 시작 실패를 빈 정의나 다른 기본값으로 대체하지 않습니다.
Session 명령·Revision·결과 확인은 [GameSession 사용 안내](LAST_SUPPLY_GameSession_사용_안내.md)를 따릅니다.

생존자와 지역의 선택 목록은 definition.Survivors와 definition.Regions를 사용합니다.
문구·능력치·사건 행동은 Core 정의에 있고, 그림 참조는 카탈로그에서 Config를 조회합니다.

```csharp
if ( _contentCatalog.TryGetSurvivorConfig(survivorId, out SurvivorConfig survivorConfig) )
{
    Sprite portraitSprite = survivorConfig.PortraitSprite;
}

if ( _contentCatalog.TryGetRegionConfig(regionId, out RegionConfig regionConfig) )
{
    string description = regionConfig.Description;
    Sprite backgroundSprite = regionConfig.BackgroundSprite;
}
```

ID 조회는 대소문자를 구분합니다. null·빈 ID·없는 ID는 false와 null을 반환합니다.
카탈로그 전체의 유효성은 조회 전 ToDefinition으로 확인합니다.

ToDefinition은 호출할 때마다 독립된 읽기 전용 정의를 만듭니다.
실행 중 Config 값을 변경해도 기존 정의와 Session에는 반영되지 않습니다.
다음 판에 편집한 값을 적용하려면 새 정의를 만드세요.
Config 조회가 반환하는 Sprite·설명 등의 Unity 설정 참조는 이 Core 사본과 별개입니다.

이 경로는 Resources.Load나 Addressables를 사용하지 않습니다.
04_Resource라는 폴더 이름만으로 자동 로딩되지 않으며, 사용자 Provider에 카탈로그 참조를 연결합니다.

## 4. 준비한 기본 값

기존 DefaultGameData.CreateDefault와 기획서 v0.3의 기본 값을 사용합니다.

| 항목 | 값 |
|---|---|
| 최대 단계 / 소음 / 일수 | 5 / 6 / 7 |
| 초기 식량 / 의약품 / 부품 | 6 / 1 / 0 |
| 목표 부품 | 8 |
| 일일 식량 소비 / 부족 피해 | 2 / 2 |
| 치료 비용 / 회복량 | 의약품 1 / HP 3 |
| 마커스 | HP 10, 전투 +4, 기술 +0 |
| 사라 | HP 8, 전투 +1, 기술 +4 |
| 귀환 사건 | RETURN_001, 전투 DC 12 |

| 단계 | 사건 없음 | 기회 | 위험 |
|---|---:|---:|---:|
| 1 | 65 | 20 | 15 |
| 2 | 55 | 20 | 25 |
| 3 | 45 | 20 | 35 |
| 4 | 35 | 20 | 45 |
| 5 | 25 | 20 | 55 |

| 소음 | 표시 | 귀환 사건 확률 | 실패 피해 |
|---|---|---:|---:|
| 0~2 | 안전 | 0% | 0 |
| 3 | 주의 | 30% | 2 |
| 4 | 위험 | 50% | 2 |
| 5 | 매우 위험 | 75% | 3 |
| 6 | 위험 확정 | 100% | 3 |

수색은 D20 한 번으로 해당 지역의 주 자원을 지급합니다.

| 단계 | D20 1~6 | 7~15 | 16~19 | 20 |
|---|---:|---:|---:|---:|
| 1~2 | 0 | 1 | 2 | 3 |
| 3~4 | 0 | 1 | 3 | 4 |
| 5 | 0 | 1 | 4 | 5 |

세 지역은 기본 수색표를 사용합니다.
지역 전용 표를 켜면 해당 표 전체로 교체하며, 빠진 구간을 기본표로 채우지 않습니다.
보상 행의 항목이 비어 있으면 미발견입니다. 복수 자원도 같은 구조로 처리하지만 기본판 수급은 기존 단일 주 자원 값을 유지했습니다.

일반 사건과 지역별 사건 목록은 기존 기획·DefaultGameData와 일치합니다.
지역 설명은 지역명과 주 자원을 설명하는 표시용 문구를 추가했습니다.
위 수치가 실제 플레이에서 균형 잡혔다는 의미는 아닙니다.

## 5. 에디터 메뉴

Unity 상단의 Last Supply > Content에 있습니다.

| 메뉴 | 용도 |
|---|---|
| Create Default Assets | 에셋이 없는 프로젝트에 기존 기본 데이터로 12개 생성 |
| Validate Default Catalog | 준비된 기본 카탈로그의 데이터 유효성 검사 |
| Validate Selected Catalog | 선택한 카탈로그의 데이터 유효성 검사 |
| Run Content Checks | 기본 데이터 일치·실제 직렬화·오류 거절 등 회귀 검사 |

생성은 모든 대상 경로를 먼저 확인합니다.
대상 에셋이나 .meta가 하나라도 있으면 생성 전체를 중단하여 기존 편집 내용을 보존합니다.
DefaultGameData는 최초 생성·비교 기준으로 남겨 두었으며, 런타임에서 카탈로그 대신 자동으로 사용하지 않습니다.

Run Content Checks는 제공된 기본판과 값이 같은지를 검사합니다.
의도적으로 수치를 수정한 카탈로그는 Validate Selected Catalog로 구조적 유효성을 확인하세요.
회귀 검사의 오류 시나리오는 임시 ScriptableObject를 사용하고 검사 후 해제합니다. 저장된 원본 데이터를 변경하지 않습니다.

## 6. 검증 기록과 범위

2026-10-16에 다음을 실행했습니다.

- Core 33개 + Content 런타임 12개 + Editor 2개를 실제 설치된 Unity 모듈 참조로 C# 9 컴파일: 통과.
- Unity 6000.3.20f1 임시 프로젝트에서 실제 기본 에셋 12개 생성·저장·재임포트: 통과.
- 별도의 새 Unity 프로세스에서 디스크 에셋을 로드하고 같은 검사 재실행: 통과.
- ContentVerification: 각 실행 1,396개 assertion 통과.
- 검사 대상: Core 기본 정의 전체 값 일치, ID 조회, null·중복·다른 사건 에셋 참조, 표 누락·중첩, 확률 합계, 사건 행동·후보 누락, 비어 있는 지역 전용 표 거절.
- 변환 뒤 설정을 수정해도 이전 정의가 유지되는지, 지역 전용 복수 보상, 재생성 시 덮어쓰기 거절, 에셋 정의로 Session 출발: 통과.
- Tests/CoreData/Verify-CoreData.ps1: 392개 assertion 통과.
- Tests/CoreSession/Verify-CoreSession.ps1: 진입 45개, 사건 46개, 진행 156,933개 assertion 통과. 진행 검사에는 100개 seed의 완주·새 Session 재시작이 포함됨.

원래 프로젝트가 기존 Unity 에디터에서 열려 있어 배치 실행은 같은 버전의 임시 프로젝트에서 진행했습니다.
Core·Content 소스와 기존 .meta가 원본과 동일함을 해시로 확인한 뒤 검사했고, Unity가 생성한 에셋·.meta를 원래 프로젝트에 복사했습니다.
복사한 파일도 해시 일치를 확인했습니다.
로그는 로컬의 Temp/ContentVerificationProject/ContentCreate.log와 ContentReload.log에 있으며 Temp는 Git 전달 대상이 아닙니다.

확인하지 않은 범위는 원래 프로젝트의 전체 패키지 포함 컴파일, 직접 Inspector 조작, Play Mode, Scene·Prefab·UI 연결, 플레이어 빌드, 재미·밸런스입니다.
원래 프로젝트에서 Unity 메뉴의 Validate Default Catalog를 실행하면 현재 환경의 에셋 연결을 다시 확인할 수 있습니다.

재실행할 Core 검사는 프로젝트 루트의 PowerShell 7에서 실행합니다.

```powershell
pwsh -NoProfile -File .\Tests\CoreData\Verify-CoreData.ps1
pwsh -NoProfile -File .\Tests\CoreSession\Verify-CoreSession.ps1
```

Unity 배치 검사 진입점은 LastSupply.Content.Editor.ContentVerification.Run입니다.
동일 프로젝트가 다른 Unity 에디터에서 열려 있으면 해당 프로젝트의 별도 배치 실행은 할 수 없습니다.

## 7. 사용자 인계 지점

데이터 입력과 참조 연결은 기본판 기준으로 완료했습니다.
이후 사용자 작업 순서는 다음과 같습니다.

1. Provider에 DefaultGameContent 카탈로그를 참조시켜 정의·Session 생성.
2. 은신처에서 생존자·지역 선택과 치료·출발 입력 연결.
3. 탐색에서 사건 행동·수색·추가 탐색·귀환과 확정 결과 표시 연결.
4. 정산·종료·재시작 UI 연결.
5. UniTask 연출의 취소·연타·재시작 수명 처리와 Play Mode 확인.
6. 실제 플레이 결과로 밸런스 조정.

기존 Core 규칙과 진행 API는 이번 작업에서 변경하지 않았습니다.
새 PC로 옮길 때는 Content 스크립트 폴더와 .meta, 04_Resource/Content 에셋 폴더와 .meta, 갱신한 Docs를 함께 전달해야 합니다.
