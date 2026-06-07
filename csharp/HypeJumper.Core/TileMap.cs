using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HypeJumper.Core;

/// <summary>텍스트 그리드 맵 로더 + 하드코딩 테스트 맵. 충돌 solids/위험/체크포인트 제공.</summary>
public class TileMap
{
    public List<RectI> Solids = new();            // 정적 충돌 사각형
    public List<Hazard> Hazards = new();          // 닿으면 사망
    public List<RectI> CheckpointRects = new();   // 체크포인트 위치 (Scene이 객체화)
    public List<RectI> GoalRects = new();         // 레벨 종료 위치 (Scene이 객체화)
    public List<MovingPlatform> Platforms = new();
    public List<JumpPad> JumpPads = new();
    public List<Spring> Springs = new();
    public List<NTT> Ntts = new();
    public List<Enemy> Enemies = new();
    public (int X, int Y) Spawn = (Settings.TILE_SIZE * 2, Settings.TILE_SIZE * 2);
    public int Width = Settings.SCREEN_WIDTH;      // 맵 픽셀 너비 (카메라 클램프용)
    public int Height = Settings.SCREEN_HEIGHT;    // 맵 픽셀 높이

    private static readonly string TestMap = BuildTestText();

    /// <summary>맵을 로드: mapFile 주면 파일에서(실패 시 폴백), 아니면 하드코딩 맵 + 코드 배치.</summary>
    public TileMap(string? text = null, string? mapFile = null)
    {
        if (mapFile != null && TryLoadFile(mapFile))
            return;                                 // 파일 로드 성공 → 종료
        LoadText(text ?? TestMap);
        BuildPlatforms();
        BuildObjects();
    }

    // ── 파일 로드 ───────────────────────────────────────────────
    /// <summary>파일에서 맵 로드 시도 — 실패 시 false(하드코딩 폴백).</summary>
    private bool TryLoadFile(string path)
    {
        try
        {
            LoadFile(File.ReadAllText(Paths.ResourcePath(path), Encoding.UTF8));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[TileMap] map load failed ({path}): {e.Message} -> fallback to hardcoded map");
            ResetCollections();                     // 부분 로드분 폐기
            return false;
        }
    }

    /// <summary>파싱 중 채워진 수집 리스트/스폰을 초기 상태로 되돌림 (폴백용).</summary>
    private void ResetCollections()
    {
        Solids.Clear(); Hazards.Clear(); CheckpointRects.Clear(); GoalRects.Clear();
        Platforms.Clear(); JumpPads.Clear(); Springs.Clear(); Ntts.Clear(); Enemies.Clear();
        Spawn = (Settings.TILE_SIZE * 2, Settings.TILE_SIZE * 2);
        Width = Settings.SCREEN_WIDTH;
        Height = Settings.SCREEN_HEIGHT;
    }

    /// <summary>2섹션 텍스트([MAP]/[OBJECTS])를 파싱 — 그리드는 LoadText, 객체는 ParseObjects.</summary>
    private void LoadFile(string text)
    {
        var mapLines = new List<string>();
        var objLines = new List<string>();
        List<string>? target = null;
        foreach (var line in SplitLines(text))
        {
            string head = line.Trim();
            if (head == "[MAP]") target = mapLines;
            else if (head == "[OBJECTS]") target = objLines;
            else if (target != null) target.Add(line);
        }
        LoadText(string.Join("\n", mapLines));
        ParseObjects(objLines);
    }

    /// <summary>[OBJECTS] 줄들을 타입별 엔티티로 생성 (주석 '#'/빈 줄 무시).</summary>
    private void ParseObjects(List<string> lines)
    {
        foreach (var line in lines)
        {
            var tok = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 0 || tok[0].StartsWith("#"))
                continue;
            SpawnObject(tok[0], tok);
        }
    }

    /// <summary>타입 토큰 + 인자로 해당 엔티티를 리스트에 추가 (알 수 없으면 예외→폴백).</summary>
    private void SpawnObject(string kind, string[] t)
    {
        switch (kind)
        {
            case "platform":  // x y w h axis dist speed
                Platforms.Add(new MovingPlatform(Num(t[1]), Num(t[2]), (int)Num(t[3]), (int)Num(t[4]),
                                                 t[5], Num(t[6]), Num(t[7])));
                break;
            case "spring":    // x y w h dir
                Springs.Add(new Spring(Num(t[1]), Num(t[2]), (int)Num(t[3]), (int)Num(t[4]), t[5]));
                break;
            case "ntt":
                Ntts.Add(new NTT(Num(t[1]), Num(t[2])));
                break;
            case "ropentt":   // 천장 피벗 좌표
                Ntts.Add(new RopeNTT(Num(t[1]), Num(t[2])));
                break;
            case "enemy":
                Enemies.Add(new Enemy(Num(t[1]), Num(t[2])));
                break;
            case "armored":
                Enemies.Add(new ArmoredEnemy(Num(t[1]), Num(t[2])));
                break;
            default:
                throw new FormatException($"알 수 없는 객체 타입: {kind}");
        }
    }

    /// <summary>토큰을 double로 파싱 (소수점 불변 문화권).</summary>
    private static double Num(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    // ── 그리드 파싱 ─────────────────────────────────────────────
    /// <summary>글자 그리드를 파싱해 solids/hazards/checkpoint/goal/spawn을 채우고 맵 크기를 계산.</summary>
    private void LoadText(string text)
    {
        int t = Settings.TILE_SIZE;
        var lines = SplitLines(text);
        if (lines.Length > 0)
        {
            int maxLen = 0;
            foreach (var line in lines) maxLen = Math.Max(maxLen, line.Length);
            Width = maxLen * t;
            Height = lines.Length * t;
        }
        for (int r = 0; r < lines.Length; r++)
        {
            string line = lines[r];
            int? runStart = null;
            for (int c = 0; c < line.Length; c++)
            {
                char ch = line[c];
                int x = c * t, y = r * t;
                if (ch == '#')
                {
                    runStart ??= c;                 // 연속 '#' 시작 기록
                    continue;
                }
                if (runStart is int rs)             // 연속 '#' 종료 → 한 Rect로
                {
                    Solids.Add(new RectI(rs * t, y, (c - rs) * t, t));
                    runStart = null;
                }
                if (ch == '^') Hazards.Add(new Hazard(x, y, t, t));
                else if (ch == 'C') CheckpointRects.Add(new RectI(x, y, t, t));
                else if (ch == 'G') GoalRects.Add(new RectI(x, y, t, t));
                else if (ch == 'J') JumpPads.Add(new JumpPad(x, y, t, t));
                else if (ch == 'P') Spawn = (x, y);
            }
            if (runStart is int rs2)                // 행 끝까지 '#'
                Solids.Add(new RectI(rs2 * t, r * t, (line.Length - rs2) * t, t));
        }
    }

    /// <summary>파이썬 str.splitlines() 의미로 줄 분리 (CRLF/CR 정규화, 종단 개행은 빈 줄 추가 안 함).</summary>
    private static string[] SplitLines(string s)
    {
        if (s.Length == 0) return Array.Empty<string>();
        string norm = s.Replace("\r\n", "\n").Replace("\r", "\n");
        var parts = new List<string>(norm.Split('\n'));
        if (parts.Count > 0 && parts[^1].Length == 0 && norm.EndsWith("\n"))
            parts.RemoveAt(parts.Count - 1);        // 종단 개행이 만든 빈 줄 제거
        return parts.ToArray();
    }

    // ── 하드코딩 테스트 맵 ──────────────────────────────────────
    /// <summary>하드코딩 테스트 맵을 글자 그리드로 구성 (파싱은 LoadText).</summary>
    private static string BuildTestText()
    {
        int cols = Settings.SCREEN_WIDTH / Settings.TILE_SIZE;   // 60
        int rows = Settings.SCREEN_HEIGHT / Settings.TILE_SIZE;  // 33
        var g = new char[rows][];
        for (int r = 0; r < rows; r++)
        {
            g[r] = new char[cols];
            for (int c = 0; c < cols; c++) g[r][c] = ' ';
        }
        // 바깥 경계 벽
        for (int c = 0; c < cols; c++) { g[0][c] = '#'; g[rows - 1][c] = '#'; }
        for (int r = 0; r < rows; r++) { g[r][0] = '#'; g[r][cols - 1] = '#'; }
        void HLine(int r, int c0, int c1, char ch) { for (int c = c0; c <= c1; c++) g[r][c] = ch; }
        HLine(rows - 2, 1, cols - 2, '#');                       // 바닥 (아래 두 줄)
        HLine(rows - 3, 1, cols - 2, '#');
        for (int rr = rows - 3; rr <= rows - 1; rr++)            // 낙사 구덩이 (cols 40~45)
            for (int c = 40; c <= 45; c++) g[rr][c] = ' ';
        HLine(rows - 4, 20, 25, '^');                            // 가시 (바닥 위)
        HLine(rows - 9, 8, 14, '#');                             // 좌측 낮은 발판
        HLine(rows - 10, 11, 12, '^');                           // 발판 위 가시
        HLine(rows - 8, 30, 36, '#');                            // 중앙 체크포인트 선반
        g[rows - 9][33] = 'C'; g[rows - 10][33] = 'C';           // 체크포인트(2칸)
        HLine(rows - 7, 48, 56, '#');                            // 우측 상단 선반
        g[rows - 4][9] = 'J';                                    // 점프패드
        g[rows - 4][3] = 'P';                                    // 스폰
        var sb = new StringBuilder();
        for (int r = 0; r < rows; r++)
        {
            if (r > 0) sb.Append('\n');
            sb.Append(g[r]);
        }
        return sb.ToString();
    }

    // ── 하드코딩 발판/오브젝트 (파일 미로드 시) ─────────────────
    /// <summary>움직이는 발판 배치 (탑승/관성/끼임 테스트 유지).</summary>
    private void BuildPlatforms()
    {
        Platforms.Add(new MovingPlatform(320, 360, 90, 14, "x", 160, 1.4));  // 좌우 왕복
        Platforms.Add(new MovingPlatform(800, 300, 64, 14, "y", 150, 1.2));  // 상하 왕복
    }

    /// <summary>스프링/NTT/적/줄NTT 코드 배치 (테스트 맵 보강).</summary>
    private void BuildObjects()
    {
        int t = Settings.TILE_SIZE;
        int rows = Settings.SCREEN_HEIGHT / t;
        Springs.Add(new Spring(50 * t, (rows - 8) * t, t, t, "up"));        // 우측 선반 위 — 위 발사
        Springs.Add(new Spring(t, (rows - 12) * t, 6, 2 * t, "right"));     // 좌측 벽 부착 — 오른쪽 발사
        Ntts.Add(new NTT(16 * t, (rows - 7) * t));                          // 공중
        int floorTop = (rows - 3) * t;                                      // 바닥 윗면
        Ntts.Add(new NTT(13 * t, floorTop - Settings.NTT_HEIGHT));          // 바닥 위 올라탐
        Enemies.Add(new Enemy(27 * t, floorTop - Settings.ENEMY_HEIGHT));   // 일반(HP1)
        Enemies.Add(new ArmoredEnemy(34 * t, floorTop - Settings.ENEMY_HEIGHT));  // 강화(HP2)
        Ntts.Add(new RopeNTT(520, 320));                                    // 줄 NTT(샹들리에)
    }

    // ── 업데이트 / 조회 ─────────────────────────────────────────
    /// <summary>모든 움직이는 발판을 갱신.</summary>
    public void Update()
    {
        foreach (var plat in Platforms)
            plat.Update();
    }

    /// <summary>정적 solid + 발판의 현재 rect를 합쳐 충돌용 리스트로 반환.</summary>
    public List<RectI> SolidRects()
    {
        var result = new List<RectI>(Solids);
        foreach (var plat in Platforms)
            result.Add(plat.Rect);
        return result;
    }
}
