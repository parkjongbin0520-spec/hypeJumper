using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace HypeJumper;

/// <summary>키보드 상태/엣지 추적 — main.py의 KEYDOWN 엣지 + get_pressed 홀드를 대체.</summary>
/// <remarks>프레임마다 Begin() 1회 호출 후 Down/Edge/AnyEdge로 조회 (이전 프레임 상태와 비교).</remarks>
public class InputReader
{
    private KeyboardState _cur;
    private KeyboardState _prev;

    /// <summary>이번 프레임 키보드 상태를 캡처 (이전 상태는 직전 프레임 값).</summary>
    public void Begin()
    {
        _prev = _cur;
        _cur = Keyboard.GetState();
    }

    /// <summary>키가 현재 눌려 있는지 (홀드).</summary>
    public bool Down(Keys k) => _cur.IsKeyDown(k);

    /// <summary>이번 프레임에 새로 눌렸는지 (엣지).</summary>
    public bool Edge(Keys k) => _cur.IsKeyDown(k) && !_prev.IsKeyDown(k);

    /// <summary>이번 프레임에 아무 키나 새로 눌렸는지 (타이틀 '아무 키나 시작').</summary>
    public bool AnyEdge() => _cur.GetPressedKeys().Any(k => !_prev.IsKeyDown(k));
}
