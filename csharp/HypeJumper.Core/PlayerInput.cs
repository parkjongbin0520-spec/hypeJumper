namespace HypeJumper.Core;

/// <summary>한 프레임의 입력 스냅샷 (점프=C, 대시=X, 잡기=Z). 테스트/파서티에서 직접 생성 가능.</summary>
public struct PlayerInput
{
    public bool Left;          // 왼쪽 방향키
    public bool Right;         // 오른쪽 방향키
    public bool Up;            // 위 방향키
    public bool Down;          // 아래 방향키 (웅크리기/패스트폴)
    public bool JumpPressed;   // 이번 프레임에 점프키를 새로 누름 (엣지)
    public bool JumpHeld;      // 점프키 유지 중 (가변 점프 높이용)
    public bool DashPressed;   // 이번 프레임에 대시키를 새로 누름 (엣지)
    public bool GrabPressed;   // 이번 프레임에 잡기키(Z)를 새로 누름 (엣지)
    public bool GrabHeld;      // 잡기키(Z) 유지 중 (조준/잡기 유지용)
}
