namespace LastSupply.Core
{
    public enum GAME_PHASE
    {
        SHELTER,        // 은신처: 생존자·지역 선택, 치료, 출발
        EVENT_ACTION,   // 일반 사건의 행동 선택
        SEARCH_CHOICE,  // 수색 또는 생략
        STAGE_CHOICE,   // 다음 단계 또는 귀환
        RETURN_ACTION,  // 귀환 사건의 행동 실행
        RESULT,         // 확정된 결과 확인
        DAY_SUMMARY,    // 완료된 탐색·하루 정산 확인
        GAME_OVER       // 게임 종료
    }
}