// 한 프레임 입력 스냅샷 — Python PlayerInput 이식. MonoBehaviour가 채워 PlayerController에 전달.
namespace HypeJumper
{
    public struct PlayerInputState
    {
        public bool Left;
        public bool Right;
        public bool Up;
        public bool Down;
        public bool JumpPressed;   // 이번 프레임에 점프키를 새로 누름 (엣지)
        public bool JumpHeld;      // 점프키 유지 중 (가변 점프 높이용)
        public bool DashPressed;   // 대시키 엣지 (Phase 2)
        public bool GrabPressed;   // 잡기키 엣지 (Phase 3)
        public bool GrabHeld;      // 잡기키 유지 (Phase 3)
    }
}
