namespace HypeJumper.Core;

/// <summary>플레이어 상태 전체 정의 — Normal 외에는 후속 Phase에서 로직 추가 (settings.py enum 1:1).</summary>
public enum PlayerState
{
    Normal,       // 걷기+점프+월슬라이드 (웅크리기는 IsDucking 플래그)
    Dash,         // 일반 대시 (Phase 2)
    DashStrike,   // 대시 중 잡기, 벽력일섬 (Phase 3, 예약)
    GrabSeeking,  // Z홀드 조준 중 (Phase 3)
    GrabReady,    // 잡기 가능 확인, 재입력 대기 (Phase 3, 예약)
    GrabActive,   // 순간이동 완료, NTT와 이동 중 (Phase 3)
}
