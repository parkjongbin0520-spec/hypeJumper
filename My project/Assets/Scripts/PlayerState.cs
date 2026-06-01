// 플레이어 상태 — Python PlayerState enum 이식. 전체 미리 정의, Phase별로 로직 추가.
namespace HypeJumper
{
    public enum PlayerState
    {
        Normal,      // 걷기+점프+월슬라이드 (웅크리기는 isDucking 플래그)
        Dash,        // 일반 대시 (Phase 2)
        DashStrike,  // (제거됨 — enum 예약만)
        GrabSeeking, // Z 누름 조준 윈도우 (Phase 3)
        GrabReady,   // (미사용)
        GrabActive   // 잡은 상태, 대상과 함께 (Phase 3)
    }
}
