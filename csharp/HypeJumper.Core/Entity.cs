namespace HypeJumper.Core;

/// <summary>위치·크기를 가진 게임 오브젝트의 공통 베이스 (Actor/Solid/Trigger의 부모).</summary>
public class Entity
{
    public double X;     // 수평 위치(서브픽셀)
    public double Y;     // 수직 위치(서브픽셀)
    public int Width;    // 히트박스 너비
    public int Height;   // 히트박스 높이

    /// <summary>위치(float, 서브픽셀)와 히트박스 크기를 초기화.</summary>
    public Entity(double x, double y, int width, int height)
    {
        X = x; Y = y; Width = width; Height = height;
    }

    /// <summary>현재 위치·크기로 충돌 판정용 정수 RectI 반환((int) 절삭=파이썬 int()와 동일).</summary>
    public RectI Rect => new RectI((int)X, (int)Y, Width, Height);

    /// <summary>히트박스 중심 좌표(float) 반환.</summary>
    public (double X, double Y) Center => (X + Width / 2.0, Y + Height / 2.0);

    /// <summary>매 프레임 로직 갱신 (하위 클래스에서 구현).</summary>
    public virtual void Update() { }
}
