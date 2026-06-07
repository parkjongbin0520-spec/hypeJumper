namespace HypeJumper.Core;

/// <summary>속도로 움직이며 이동 델타를 기록하는 막힘 오브젝트 (MovingPlatform의 부모).</summary>
public class Solid : Entity
{
    public double Vx;   // 수평 속도
    public double Vy;   // 수직 속도
    public double Dx;   // 이번 프레임 실제 수평 이동량 (탑승 캐리용)
    public double Dy;   // 이번 프레임 실제 수직 이동량 (탑승 캐리용)

    /// <summary>위치·크기와 속도/델타를 초기화.</summary>
    public Solid(double x, double y, int width, int height) : base(x, y, width, height) { }

    /// <summary>속도만큼 위치를 이동하고 실제 이동 델타를 기록 (하위에서 Vx/Vy 설정 후 호출).</summary>
    public override void Update()
    {
        double px = X, py = Y;
        X += Vx;
        Y += Vy;
        Dx = X - px;
        Dy = Y - py;
    }
}
