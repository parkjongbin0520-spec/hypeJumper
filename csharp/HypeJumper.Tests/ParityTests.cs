using System.Text.Json;
using HypeJumper.Core;

namespace HypeJumper.Tests;

/// <summary>C# 포트 Player가 파이썬 원본과 프레임 단위로 일치하는지 검증 (parity_dump.py 정답지 대조).</summary>
public class ParityTests
{
    private const double Eps = 1e-9;   // double 동일 연산이면 사실상 0, JSON 왕복 여유분
    private static ParityData? _cache;

    /// <summary>출력 폴더의 parity_data.json을 1회 로드/캐시.</summary>
    private static ParityData Data()
    {
        if (_cache != null) return _cache;
        string path = Path.Combine(AppContext.BaseDirectory, "Parity", "parity_data.json");
        var opt = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
        _cache = JsonSerializer.Deserialize<ParityData>(File.ReadAllText(path), opt)!;
        return _cache;
    }

    public static IEnumerable<object[]> ScenarioNames()
    {
        foreach (var sc in Data().Scenarios)
            yield return new object[] { sc.Name };
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void MatchesPythonReference(string name)
    {
        var sc = Data().Scenarios.First(s => s.Name == name);
        var solids = sc.Solids.Select(s => new RectI(s[0], s[1], s[2], s[3])).ToList();
        var p = new Player(sc.PlayerStart[0], sc.PlayerStart[1]);
        for (int i = 0; i < sc.Steps.Count; i++)
        {
            p.Update(ToInput(sc.Steps[i]), solids);
            var e = sc.Frames[i];
            string ctx = $"[{name}] frame {i}";
            Close(e.X, p.X, ctx, "x");
            Close(e.Y, p.Y, ctx, "y");
            Close(e.VxIn, p.VxInput, ctx, "vx_in");
            Close(e.VxExt, p.VxExternal, ctx, "vx_ext");
            Close(e.Vy, p.Vy, ctx, "vy");
            Assert.True(string.Equals(e.State, p.State.ToString(), StringComparison.OrdinalIgnoreCase),
                $"{ctx} state py={e.State} cs={p.State}");
            Assert.True(e.OnGround == p.OnGround, $"{ctx} on_ground py={e.OnGround} cs={p.OnGround}");
            Assert.True(e.OnWall == p.OnWall, $"{ctx} on_wall py={e.OnWall} cs={p.OnWall}");
            Assert.True(e.IsDucking == p.IsDucking, $"{ctx} is_ducking py={e.IsDucking} cs={p.IsDucking}");
            Assert.True(e.Dashes == p.Dashes, $"{ctx} dashes py={e.Dashes} cs={p.Dashes}");
            Assert.True(e.DashTimer == p.DashTimer, $"{ctx} dash_timer py={e.DashTimer} cs={p.DashTimer}");
            Assert.True(e.CeilingStick == p.CeilingStick, $"{ctx} ceiling_stick py={e.CeilingStick} cs={p.CeilingStick}");
            Assert.True(e.HangTimer == p.HangTimer, $"{ctx} hang_timer py={e.HangTimer} cs={p.HangTimer}");
        }
    }

    private static void Close(double expected, double actual, string ctx, string field)
        => Assert.True(Math.Abs(expected - actual) < Eps,
            $"{ctx} {field} py={expected:R} cs={actual:R} (Δ={Math.Abs(expected - actual):R})");

    private static PlayerInput ToInput(InputDto d) => new()
    {
        Left = d.Left, Right = d.Right, Up = d.Up, Down = d.Down,
        JumpPressed = d.JumpPressed, JumpHeld = d.JumpHeld, DashPressed = d.DashPressed,
        GrabPressed = d.GrabPressed, GrabHeld = d.GrabHeld,
    };

    // ── JSON DTO (snake_case 자동 매핑) ─────────────────────────
    public class ParityData { public List<ScenarioDto> Scenarios { get; set; } = new(); }

    public class ScenarioDto
    {
        public string Name { get; set; } = "";
        public double[] PlayerStart { get; set; } = System.Array.Empty<double>();
        public int[][] Solids { get; set; } = System.Array.Empty<int[]>();
        public List<InputDto> Steps { get; set; } = new();
        public List<FrameDto> Frames { get; set; } = new();
    }

    public class InputDto
    {
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool JumpPressed { get; set; }
        public bool JumpHeld { get; set; }
        public bool DashPressed { get; set; }
        public bool GrabPressed { get; set; }
        public bool GrabHeld { get; set; }
    }

    public class FrameDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double VxIn { get; set; }
        public double VxExt { get; set; }
        public double Vy { get; set; }
        public string State { get; set; } = "";
        public bool OnGround { get; set; }
        public bool OnWall { get; set; }
        public bool IsDucking { get; set; }
        public int Dashes { get; set; }
        public int DashTimer { get; set; }
        public int CeilingStick { get; set; }
        public int HangTimer { get; set; }
    }
}
