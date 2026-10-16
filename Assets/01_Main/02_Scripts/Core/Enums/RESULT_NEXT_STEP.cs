namespace LastSupply.Core
{
    public enum RESULT_NEXT_STEP
    {
        NONE,
        SEARCH_CHOICE,      // 일반 사건에서 생존: 수색 선택으로 이동
        FINISH_STAGE,       // 수색 완료: 현재 단계 종료 처리
        FINISH_EXPEDITION   // 사망 또는 귀환 판정 완료: 탐색 정산
    }
}