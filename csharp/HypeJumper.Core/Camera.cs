using System;

namespace HypeJumper.Core;

/// <summary>뷰 크기 그리드의 '현재 방'을 고정 표시하고, 방이 바뀌면 옆 방으로 슬라이드한다.</summary>
/// <remarks>방 격자 인덱스는 floor 나눗셈으로 계산 — C# 정수 / 는 0방향 절삭이라 음수에서 어긋나므로 Math.Floor 사용.</remarks>
public class Camera
{
    public int ViewW;            // 뷰(=한 방) 너비
    public int ViewH;            // 뷰(=한 방) 높이
    public double X;             // 월드 좌상단 x (슬라이드용)
    public double Y;             // 월드 좌상단 y
    public int Col;              // 현재 방 격자 인덱스 (가로)
    public int Row;              // 현재 방 격자 인덱스 (세로)
    private (double X, double Y) _target;  // 슬라이드 목표 좌상단
    public bool Sliding;         // 방 전환 슬라이드 진행 중

    /// <summary>뷰(=한 방) 크기를 설정하고 오프셋·방 인덱스를 초기화.</summary>
    public Camera(int viewW = Settings.INTERNAL_W, int viewH = Settings.INTERNAL_H)
    {
        ViewW = viewW; ViewH = viewH;
        X = 0.0; Y = 0.0;
        Col = 0; Row = 0;
        _target = (0.0, 0.0);
        Sliding = false;
    }

    /// <summary>그리기용 정수 오프셋 (round = 파이썬 round와 동일한 round-half-to-even).</summary>
    public (int X, int Y) Offset => ((int)Math.Round(X), (int)Math.Round(Y));

    /// <summary>격자 인덱스(col,row)의 방 좌상단을 맵 경계 안으로 클램프해 반환.</summary>
    private (double X, double Y) ClampOrigin(int col, int row, int mapW, int mapH)
    {
        double ox = Math.Min(Math.Max(col * ViewW, 0), Math.Max(0, mapW - ViewW));
        double oy = Math.Min(Math.Max(row * ViewH, 0), Math.Max(0, mapH - ViewH));
        return (ox, oy);
    }

    /// <summary>대상이 속한 방으로 즉시 고정 (레벨 로드/리스폰 시 — 슬라이드 없음).</summary>
    public void SnapTo(RectI rect, int mapW, int mapH)
    {
        Col = (int)Math.Floor((double)rect.CenterX / ViewW);
        Row = (int)Math.Floor((double)rect.CenterY / ViewH);
        var (ox, oy) = ClampOrigin(Col, Row, mapW, mapH);
        X = ox; Y = oy;
        _target = (ox, oy);
        Sliding = false;
    }

    /// <summary>현재 방을 완전히 벗어났을 때만 이웃 방으로 슬라이드(히스테리시스). 슬라이드 중이면 true.</summary>
    public bool Update(RectI rect, int mapW, int mapH)
    {
        if (!Sliding)
        {
            var (dox, doy) = ClampOrigin(Col, Row, mapW, mapH);
            int cx = rect.CenterX, cy = rect.CenterY;
            bool inside = (dox <= cx && cx < dox + ViewW) && (doy <= cy && cy < doy + ViewH);
            if (!inside)   // 현재 표시 영역 밖 → 이웃 방으로 전환
            {
                Col = (int)Math.Floor((double)cx / ViewW);
                Row = (int)Math.Floor((double)cy / ViewH);
                var (nox, noy) = ClampOrigin(Col, Row, mapW, mapH);
                _target = (nox, noy);
                Sliding = (nox, noy) != (X, Y);  // 같은 원점(가장자리 클램프)이면 생략
            }
        }
        if (Sliding)
        {
            var (tx, ty) = _target;
            X += (tx - X) * Settings.ROOM_SLIDE_LERP;
            Y += (ty - Y) * Settings.ROOM_SLIDE_LERP;
            if (Math.Abs(tx - X) < 0.5 && Math.Abs(ty - Y) < 0.5)  // 도달 → 스냅 종료
            {
                X = tx; Y = ty;
                Sliding = false;
            }
        }
        return Sliding;
    }
}
