// 게임 전역 상수 — Python settings.py 이식.
// ⚠️ y축 반전: Pygame은 아래가 +y(점프 음수)였지만, Unity는 위가 +y이므로
//    점프/월점프 수직값을 '양수'(위 방향)로 바꿔 옮겼다. 중력은 vy에서 빼서 적용한다.
// ⚠️ 단위: 값은 '픽셀/프레임' 기준 튜닝값. PPU(1유닛=16픽셀)로 나눠 월드 유닛으로 변환해 쓴다.
// ⚠️ 프레임: 상수는 60fps/프레임 기준. FixedUpdate(fixedDeltaTime=1/60)에서 매 스텝 그대로 적용.

namespace HypeJumper
{
    public static class GameSettings
    {
        // ── 단위/화면 ─────────────────────────────
        public const float PPU = 16f;            // 픽셀-퍼-유닛 (1타일=16px=1unit)
        public const int TileSize = 16;          // 타일 크기 (픽셀)

        // ── 플레이어 히트박스 (픽셀) ──────────────
        public const float PlayerWidth = 8f;
        public const float NormalHitboxHeight = 16f;
        public const float DuckHitboxHeight = 8f;

        // ── Phase 1: 이동 ─────────────────────────
        public const float PlayerMaxSpeed = 4f;
        public const float GroundAccel = 1.2f;
        public const float GroundDecel = 1.2f;
        public const float AirMult = 0.65f;
        public const float AirFriction = 0.85f;
        public const float SpeedReduce = 0.8f;
        public const float PlatformInertiaX = 1.0f;
        public const float PlatformInertiaY = 1.0f;
        public const float WallSlideGravity = 0.3f;
        public const float WallSlideMaxFall = 3f;   // 월슬라이드 종단 낙하(양수 크기)

        // ── Phase 1: 점프/중력 (위=+y로 부호 반전) ─
        public const float JumpSpeed = 6.5f;        // 점프 시작 속도 (위 방향, Python -6.5 → +6.5)
        public const float GravityUp = 0.4f;        // 상승 중 버튼 유지
        public const float GravityUpRelease = 1.2f; // 상승 중 버튼 뗌
        public const float GravityDown = 0.8f;      // 하강
        public const float MaxFallSpeed = 12f;      // 최대 낙하(양수 크기)
        public const float FastFallGravity = 1.6f;
        public const float FastMaxFall = 20f;

        // ── Phase 1: 월 점프 (위=+y) ──────────────
        public const float WallJumpH = 6f;          // 벽 반대 수평 반발
        public const float WallJumpV = 6.5f;        // 위 방향 (Python -6.5 → +6.5)

        // ── Phase 1: 버퍼/코요테 (프레임) ─────────
        public const int CoyoteTime = 6;
        public const int WallCoyoteTime = 4;
        public const int JumpBuffer = 6;
        public const int WallJumpBuffer = 4;
        public const int NearGroundDistance = 6;    // 픽셀

        // ── Phase 1: 충돌 ─────────────────────────
        public const int CeilingStickDivisor = 2;
        public const int MaxCeilingStickFrames = 10;

        // ── Phase 2: 대시 (나중에 이식) ───────────
        public const int MaxDashes = 1;
        public const float DashSpeed = 8.0f;
        public const float EndDashSpeed = 5.0f;
        public const float EndDashUpMult = 0.75f;
        public const int DashTime = 9;

        // (Phase 2.5 / 3 상수는 해당 기능 이식할 때 여기에 추가)
    }
}
