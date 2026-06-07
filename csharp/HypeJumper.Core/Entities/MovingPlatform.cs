namespace HypeJumper.Core;

/// <summary>시작 지점에서 한 축(x/y)으로 distance만큼 왕복하는 발판 (Phase 1 탑승/관성 테스트).</summary>
public class MovingPlatform : Solid
{
    private readonly (double X, double Y) _origin;  // 왕복 기준점
    private readonly string _axis;                  // "x"(수평) 또는 "y"(수직)
    private readonly double _distance;              // 왕복 거리(픽셀)
    private readonly double _speed;                 // 이동 속도(픽셀/프레임)
    private int _dir = 1;                           // 진행 방향(+1/-1)

    /// <summary>위치·크기와 왕복 축/거리/속도를 설정.</summary>
    public MovingPlatform(double x, double y, int width, int height, string axis, double distance, double speed)
        : base(x, y, width, height)
    {
        _origin = (x, y);
        _axis = axis;
        _distance = distance;
        _speed = speed;
    }

    /// <summary>한 축으로 이동하고 범위 끝에서 방향을 뒤집으며, 실제 델타를 속도로 기록.</summary>
    public override void Update()
    {
        double px = X, py = Y;
        if (_axis == "x")
        {
            X += _speed * _dir;
            Bounce(X - _origin.X, "x");
        }
        else
        {
            Y += _speed * _dir;
            Bounce(Y - _origin.Y, "y");
        }
        Dx = X - px;   // 실제 이동량(탑승 캐리용)
        Dy = Y - py;
        Vx = Dx;       // 관성 전달용 속도 = 실제 델타
        Vy = Dy;
    }

    /// <summary>왕복 범위를 벗어나면 끝에 고정하고 방향을 전환.</summary>
    private void Bounce(double rel, string axis)
    {
        if (rel >= _distance) { SetAxis(axis, _distance); _dir = -1; }
        else if (rel <= 0) { SetAxis(axis, 0); _dir = 1; }
    }

    /// <summary>기준점 기준 상대 위치로 해당 축 좌표를 고정.</summary>
    private void SetAxis(string axis, double rel)
    {
        if (axis == "x") X = _origin.X + rel;
        else Y = _origin.Y + rel;
    }
}
