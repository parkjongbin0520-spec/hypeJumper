using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>충돌 방향 플래그 (파이썬 dict {up,down,left,right} 대응).</summary>
public struct Collisions
{
    public bool Up, Down, Left, Right;
}

/// <summary>속도·축분리 충돌을 처리하는 동적 오브젝트 베이스 (Player/Enemy/NTT의 부모).</summary>
public class Actor : Entity
{
    public double VxInput;     // 플레이어 입력에 의한 수평 속도
    public double VxExternal;  // 외적 요인(대시·발판 관성) 수평 속도, 상한선 없음
    public double Vy;          // 수직 속도
    public Collisions Col;     // 이번 이동의 충돌 방향 플래그

    /// <summary>속도 컴포넌트와 충돌 플래그를 초기화.</summary>
    public Actor(double x, double y, int width, int height) : base(x, y, width, height)
    {
        Col = new Collisions();
    }

    /// <summary>최종 수평 속도 = 입력 속도 + 외적 속도.</summary>
    public double Vx => VxInput + VxExternal;

    /// <summary>축분리 방식으로 이동 후 솔리드 충돌 해소 (수평 먼저, 수직 나중).</summary>
    public void Move(IReadOnlyList<RectI> solids)
    {
        Col = new Collisions();
        X += Vx;
        CollideAxis(solids, horizontal: true);
        Y += Vy;
        CollideAxis(solids, horizontal: false);
    }

    /// <summary>한 축 이동 후 겹친 솔리드 방향으로 밀어내고 충돌 플래그를 기록.</summary>
    private void CollideAxis(IReadOnlyList<RectI> solids, bool horizontal)
    {
        var rect = Rect;
        for (int i = 0; i < solids.Count; i++)
        {
            var solid = solids[i];
            if (!rect.CollideRect(solid))
                continue;
            if (horizontal) { ResolveHorizontal(ref rect, solid); X = rect.X; }
            else { ResolveVertical(ref rect, solid); Y = rect.Y; }
        }
    }

    /// <summary>수평 충돌 해소 — 이동 방향 반대편 벽면에 밀착시키고 좌/우 플래그 설정.</summary>
    private void ResolveHorizontal(ref RectI rect, in RectI solid)
    {
        if (Vx > 0) { rect.Right = solid.Left; Col.Right = true; }   // 오른쪽 이동 → 솔리드 왼면 밀착
        else if (Vx < 0) { rect.Left = solid.Right; Col.Left = true; } // 왼쪽 이동 → 솔리드 오른면 밀착
    }

    /// <summary>수직 충돌 해소 — 바닥/천장에 밀착시키고 상/하 플래그 설정.</summary>
    private void ResolveVertical(ref RectI rect, in RectI solid)
    {
        if (Vy > 0) { rect.Bottom = solid.Top; Col.Down = true; }    // 하강 → 바닥 착지
        else if (Vy < 0) { rect.Top = solid.Bottom; Col.Up = true; } // 상승 → 천장 박음
    }
}
