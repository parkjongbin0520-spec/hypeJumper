namespace HypeJumper.Core;

/// <summary>잡기 대상 공통 인터페이스 — 파이썬 덕타이핑(NTT/Enemy/RopeNTT)을 C# 타입으로 대체.</summary>
public interface IGrabbable
{
    (double X, double Y) Center { get; }      // 중심 좌표 (탐색/순간이동 기준)
    RectI Rect { get; }                        // 히트박스 (조준 표시용, Entity가 제공)
    bool Grabbed { get; }                      // 플레이어가 잡고 있는지
    bool Grabbable();                          // 현재 잡을 수 있는 상태인지 (적: 파괴/무적 제외)
    void OnGrab();                             // 잡히는 순간
    void OnRelease(int pushX, int pushY);      // 릴리즈 — 발사 반대로 밀쳐짐
    (double X, double Y) ReleaseVelocity();    // 릴리즈 시 가산 속도 (줄 진자만 비영)
}
