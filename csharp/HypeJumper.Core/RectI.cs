using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>정수 AABB — pygame.Rect 의미를 그대로 미러(충돌/세터/clipline). 값 타입.</summary>
/// <remarks>
/// 파이썬 코드의 "복사→세터 수정→되읽기"(예: rect=self.rect; rect.Right=solid.Left; X=rect.X)
/// 패턴을 위해 가변 struct로 둔다. 항상 지역 변수로만 변형하므로 가변 struct 함정은 없음.
/// 충돌은 pygame과 동일한 반열림(half-open) 규칙: 모서리가 맞닿는 것은 충돌 아님.
/// </remarks>
public struct RectI
{
    public int X;       // 좌상단 x
    public int Y;       // 좌상단 y
    public int Width;   // 너비
    public int Height;  // 높이

    /// <summary>좌상단 좌표와 크기로 생성한다.</summary>
    public RectI(int x, int y, int width, int height)
    {
        X = x; Y = y; Width = width; Height = height;
    }

    // ── 모서리/중심 (세터는 pygame처럼 위치를 이동시킴) ──────────
    public int Left { readonly get => X; set => X = value; }                  // 좌변
    public int Top { readonly get => Y; set => Y = value; }                   // 상변
    public int Right { readonly get => X + Width; set => X = value - Width; }  // 우변(설정 시 x 이동)
    public int Bottom { readonly get => Y + Height; set => Y = value - Height; } // 하변(설정 시 y 이동)
    public readonly int CenterX => X + Width / 2;   // 중심 x (정수 나눗셈=양수면 pygame과 동일)
    public readonly int CenterY => Y + Height / 2;  // 중심 y

    /// <summary>이동한 새 사각형을 반환(원본 불변, pygame.Rect.move).</summary>
    public readonly RectI Move(int dx, int dy) => new RectI(X + dx, Y + dy, Width, Height);

    /// <summary>중심을 유지한 채 크기를 dx/dy만큼 키운 새 사각형(pygame.Rect.inflate, 렌더용).</summary>
    public readonly RectI Inflate(int dx, int dy) => new RectI(X - dx / 2, Y - dy / 2, Width + dx, Height + dy);

    /// <summary>다른 사각형과 겹치는지 — pygame 반열림 규칙(맞닿음은 비충돌, 0크기는 비충돌).</summary>
    public readonly bool CollideRect(in RectI o)
    {
        if (Width <= 0 || Height <= 0 || o.Width <= 0 || o.Height <= 0)
            return false;
        return X < o.X + o.Width && o.X < X + Width
            && Y < o.Y + o.Height && o.Y < Y + Height;
    }

    /// <summary>리스트에서 처음 겹치는 사각형의 인덱스(없으면 -1) — pygame.Rect.collidelist.</summary>
    public readonly int CollideList(IReadOnlyList<RectI> rects)
    {
        for (int i = 0; i < rects.Count; i++)
            if (CollideRect(rects[i]))
                return i;
        return -1;
    }

    /// <summary>선분이 사각형과 교차/내부 통과하면 true — pygame.Rect.clipline(레이캐스트 LOS용).</summary>
    public readonly bool ClipLine(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double t0 = 0.0, t1 = 1.0;
        // Liang–Barsky: 4변에 대해 진입/이탈 파라미터를 좁혀 유효 구간이 남으면 교차.
        if (!ClipTest(-dx, x1 - Left, ref t0, ref t1)) return false;     // 좌변
        if (!ClipTest(dx, Right - x1, ref t0, ref t1)) return false;     // 우변
        if (!ClipTest(-dy, y1 - Top, ref t0, ref t1)) return false;     // 상변
        if (!ClipTest(dy, Bottom - y1, ref t0, ref t1)) return false;     // 하변
        return true;
    }

    /// <summary>Liang–Barsky 한 변 클립 — 구간이 비면 false.</summary>
    private static bool ClipTest(double p, double q, ref double t0, ref double t1)
    {
        if (p == 0.0)
            return q >= 0.0;                 // 변에 평행: 경계 밖이면 탈락
        double r = q / p;
        if (p < 0.0)                          // 진입(좌/상에서 들어옴)
        {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else                                  // 이탈(우/하로 나감)
        {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }
        return true;
    }

    public override readonly string ToString() => $"RectI({X},{Y},{Width},{Height})";
}
