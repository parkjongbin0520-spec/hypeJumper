using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HypeJumper.Core;

namespace HypeJumper;

/// <summary>최상위 게임 상태 — 타이틀/플레이/일시정지 (main.GameState).</summary>
public enum GameState { Title, Playing, Paused }

/// <summary>창·루프 관리 + 입력 수집 + Scene 갱신/렌더 위임 (main.py 이식).</summary>
public class Game1 : Game
{
    private const int ScreenW = Settings.SCREEN_WIDTH, ScreenH = Settings.SCREEN_HEIGHT;
    private const int InternalW = Settings.INTERNAL_W, InternalH = Settings.INTERNAL_H;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _sb = null!;
    private RenderTarget2D _world = null!;        // 줌용 내부 저해상 렌더 타깃(480x272)

    private readonly InputReader _input = new();
    private Audio _audio = null!;
    private Assets _assets = null!;
    private Fonts _fonts = null!;
    private Renderer _renderer = null!;
    private Scene _scene = null!;

    private GameState _state = GameState.Title;
    private int _pauseIndex;
    private bool _pendingJump, _pendingDash, _pendingGrab;  // 누적 엣지 (슬로우 스킵 프레임 보존)
    private int _slowPhase;                                 // 조준 슬로우 프레임 카운터

    /// <summary>창 크기·고정 60fps 타임스텝을 설정하고 타이틀 상태로 시작.</summary>
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenW,
            PreferredBackBufferHeight = ScreenH,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = System.TimeSpan.FromSeconds(1.0 / Settings.FPS);
        Window.Title = Settings.TITLE;
    }

    /// <summary>그래픽/오디오/에셋/씬을 생성하고 Core 오디오 훅을 연결.</summary>
    protected override void LoadContent()
    {
        _sb = new SpriteBatch(GraphicsDevice);
        _world = new RenderTarget2D(GraphicsDevice, InternalW, InternalH);
        _audio = new Audio();
        Sound.Play = _audio.Play;             // Core 로직의 오디오 훅 연결
        Sound.PlayMusic = _audio.PlayMusic;
        _assets = new Assets(GraphicsDevice);
        _fonts = new Fonts();
        _renderer = new Renderer(GraphicsDevice, _assets, _fonts);
        _scene = new Scene();                 // 첫 레벨 로드 (Sound 연결 후라 BGM 정상)
    }

    // ── 갱신 ─────────────────────────────────────────────────────
    /// <summary>상태별 입력 라우팅 + 플레이 중 씬 갱신.</summary>
    protected override void Update(GameTime gameTime)
    {
        _input.Begin();
        switch (_state)
        {
            case GameState.Title: HandleTitle(); break;
            case GameState.Paused: HandlePause(); break;
            case GameState.Playing: HandlePlayKeys(); UpdatePlaying(); break;
        }
        base.Update(gameTime);
    }

    /// <summary>타이틀 — ESC 종료, 그 외 아무 키나 누르면 시작.</summary>
    private void HandleTitle()
    {
        if (_input.Edge(Keys.Escape)) Exit();
        else if (_input.AnyEdge()) _state = GameState.Playing;
    }

    /// <summary>일시정지 메뉴 — 위/아래 이동, Enter/C 확정, ESC 즉시 재개.</summary>
    private void HandlePause()
    {
        int n = Settings.PAUSE_ITEMS.Length;
        if (_input.Edge(Keys.Escape)) Resume();
        else if (_input.Edge(Keys.Up) || _input.Edge(Keys.W)) _pauseIndex = (_pauseIndex - 1 + n) % n;
        else if (_input.Edge(Keys.Down) || _input.Edge(Keys.S)) _pauseIndex = (_pauseIndex + 1) % n;
        else if (_input.Edge(Keys.Enter) || _input.Edge(Keys.C)) ConfirmPause();
    }

    /// <summary>플레이 중 키 — ESC 일시정지, 점프(C)/대시(X)/잡기(Z) 엣지 누적, R 리셋.</summary>
    private void HandlePlayKeys()
    {
        if (_input.Edge(Keys.Escape)) { EnterPause(); return; }
        if (_input.Edge(Keys.C)) _pendingJump = true;
        if (_input.Edge(Keys.X)) _pendingDash = true;
        if (_input.Edge(Keys.Z)) _pendingGrab = true;
        if (_input.Edge(Keys.R)) _scene = new Scene();   // 전체 리셋
    }

    /// <summary>입력 스냅샷을 만들어(슬로우면 일부 스킵) 씬을 갱신하고 엣지 정리.</summary>
    private void UpdatePlaying()
    {
        var inp = BuildInput();
        if (!ShouldStep()) return;            // 조준 슬로우: 이번 프레임 갱신 스킵
        _scene.Update(inp);
        ClearEdges();
    }

    /// <summary>조준 슬로우 중이면 GRAB_SLOW_FACTOR 프레임마다 1번만 갱신(1/N 속도).</summary>
    private bool ShouldStep()
    {
        if (_scene.Player.AimSlow)
        {
            _slowPhase++;
            return _slowPhase % Settings.GRAB_SLOW_FACTOR == 0;
        }
        _slowPhase = 0;
        return true;
    }

    /// <summary>현재 키 상태 + 누적 엣지를 PlayerInput으로 (점프=C, 대시=X, 잡기=Z).</summary>
    private PlayerInput BuildInput() => new()
    {
        Left = _input.Down(Keys.Left) || _input.Down(Keys.A),
        Right = _input.Down(Keys.Right) || _input.Down(Keys.D),
        Up = _input.Down(Keys.Up) || _input.Down(Keys.W),
        Down = _input.Down(Keys.Down) || _input.Down(Keys.S),
        JumpPressed = _pendingJump,
        JumpHeld = _input.Down(Keys.C),
        DashPressed = _pendingDash,
        GrabPressed = _pendingGrab,
        GrabHeld = _input.Down(Keys.Z),
    };

    private void ClearEdges() { _pendingJump = _pendingDash = _pendingGrab = false; }
    private void EnterPause() { _state = GameState.Paused; _pauseIndex = 0; ClearEdges(); }
    private void Resume() { _state = GameState.Playing; ClearEdges(); }
    private void ConfirmPause() { if (_pauseIndex == 0) Resume(); else Exit(); }

    // ── 렌더 ─────────────────────────────────────────────────────
    /// <summary>내부 480x272에 씬을 그린 뒤 창 크기로 도트 확대, 그 위에 HUD/메뉴.</summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_world);
        GraphicsDevice.Clear(Palette.Bg);
        if (_state != GameState.Title)
            _renderer.DrawWorld(_sb, _scene);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _sb.Begin(samplerState: SamplerState.PointClamp);
        _sb.Draw(_world, new Rectangle(0, 0, ScreenW, ScreenH), Color.White);
        _sb.End();

        if (_state == GameState.Title)
        {
            _renderer.DrawTitle(_sb);
        }
        else
        {
            _renderer.DrawHud(_sb, _scene);
            if (_state == GameState.Paused)
                _renderer.DrawPause(_sb, _pauseIndex);
        }
        base.Draw(gameTime);
    }
}
