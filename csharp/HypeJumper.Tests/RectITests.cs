using HypeJumper.Core;
using Xunit;

namespace HypeJumper.Tests;

/// <summary>RectI가 pygame.Rect 의미(반열림 충돌·세터 이동·clipline)와 일치하는지 검증.</summary>
public class RectITests
{
    [Fact]
    public void CollideRect_Overlap_True()
    {
        var a = new RectI(0, 0, 10, 10);
        var b = new RectI(5, 5, 10, 10);
        Assert.True(a.CollideRect(b));
        Assert.True(b.CollideRect(a));
    }

    [Fact]
    public void CollideRect_TouchingEdge_False()  // 반열림: 맞닿음은 비충돌
    {
        var a = new RectI(0, 0, 10, 10);
        Assert.False(a.CollideRect(new RectI(10, 0, 10, 10)));  // 우변 맞닿음
        Assert.False(a.CollideRect(new RectI(0, 10, 10, 10)));  // 하변 맞닿음
        Assert.False(a.CollideRect(new RectI(-10, 0, 10, 10))); // 좌변 맞닿음
    }

    [Fact]
    public void CollideRect_OnePixelOverlap_True()
    {
        var a = new RectI(0, 0, 10, 10);
        Assert.True(a.CollideRect(new RectI(9, 0, 10, 10)));    // x=[9,10) 겹침
    }

    [Fact]
    public void CollideRect_ZeroSize_False()
    {
        var a = new RectI(0, 0, 10, 10);
        Assert.False(a.CollideRect(new RectI(5, 5, 0, 10)));
        Assert.False(new RectI(0, 0, 0, 0).CollideRect(a));
    }

    [Fact]
    public void CollideList_ReturnsFirstIndex_Or_MinusOne()
    {
        var probe = new RectI(0, 0, 10, 10);
        var solids = new[]
        {
            new RectI(100, 0, 10, 10),  // 안 겹침
            new RectI(5, 5, 10, 10),    // 겹침 (첫 충돌)
            new RectI(6, 6, 10, 10),    // 겹침 (뒤)
        };
        Assert.Equal(1, probe.CollideList(solids));
        Assert.Equal(-1, new RectI(500, 500, 5, 5).CollideList(solids));
    }

    [Fact]
    public void Move_ReturnsShifted_OriginalUnchanged()
    {
        var a = new RectI(10, 20, 8, 16);
        var m = a.Move(0, 1);
        Assert.Equal(new RectI(10, 21, 8, 16).ToString(), m.ToString());
        Assert.Equal(20, a.Y);  // 원본 불변
    }

    [Fact]
    public void Setter_Right_Repositions_X()  // rect.Right = solid.Left 패턴
    {
        var r = new RectI(0, 0, 8, 16);
        r.Right = 20;
        Assert.Equal(12, r.X);
        Assert.Equal(20, r.Right);
    }

    [Fact]
    public void Setter_Bottom_Repositions_Y()  // r.Bottom = hit.Top 패턴
    {
        var r = new RectI(0, 0, 8, 16);
        r.Bottom = 100;
        Assert.Equal(84, r.Y);
        Assert.Equal(100, r.Bottom);
    }

    [Theory]
    [InlineData(0, 8, 4)]    // 플레이어 너비 8 → centerx 4
    [InlineData(0, 14, 7)]   // NTT 너비 14 → 7
    [InlineData(3, 16, 11)]  // x=3, 너비 16 → 3+8
    public void CenterX_IntegerDivision(int x, int w, int expected)
    {
        Assert.Equal(expected, new RectI(x, 0, w, 16).CenterX);
    }

    [Fact]
    public void OnGround_Probe_Semantics()  // 발밑 1px 검사 = 착지 판정 핵심
    {
        var solid = new RectI(0, 100, 100, 16);     // 윗면 top=100
        var resting = new RectI(0, 84, 8, 16);      // bottom=100 (얹혀 있음)
        Assert.False(resting.CollideRect(solid));   // 가만히 있을 땐 비충돌
        Assert.True(resting.Move(0, 1).CollideRect(solid));  // 1px 내리면 충돌=on_ground
    }

    [Fact]
    public void ClipLine_CrossingRect_True()
    {
        var r = new RectI(10, 10, 20, 20);          // [10,30)x[10,30)
        Assert.True(r.ClipLine(0, 20, 40, 20));     // 수평 관통
        Assert.True(r.ClipLine(15, 15, 25, 25));    // 내부 선분
        Assert.True(r.ClipLine(0, 0, 20, 20));      // 대각 진입
    }

    [Fact]
    public void ClipLine_OutsideRect_False()
    {
        var r = new RectI(10, 10, 20, 20);
        Assert.False(r.ClipLine(0, 0, 5, 5));       // 완전히 왼위 밖
        Assert.False(r.ClipLine(0, 40, 40, 40));    // 아래로 빗나감
        Assert.False(r.ClipLine(40, 0, 40, 40));    // 오른쪽 밖 수직
    }
}
