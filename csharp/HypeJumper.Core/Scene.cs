using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>플레이어가 닿으면 리스폰 지점을 갱신하는 체크포인트 (PLAYER만 반응).</summary>
public class Checkpoint : Trigger
{
    public bool Active;

    /// <summary>PLAYER 대상 트리거로 초기화 (비활성 상태로 시작).</summary>
    public Checkpoint(double x, double y, int width, int height)
        : base(x, y, width, height, new List<Layer> { Layer.Player })
    {
        Active = false;
    }

    /// <summary>플레이어가 닿으면 씬에 이 체크포인트를 활성 지점으로 등록.</summary>
    public override void OnEnter(Player actor, Scene scene) => scene.SetCheckpoint(this);
}

/// <summary>플레이어가 닿으면 다음 레벨로 전환을 요청하는 레벨 종료 트리거 (PLAYER만 반응).</summary>
public class Goal : Trigger
{
    /// <summary>PLAYER 대상 트리거로 초기화.</summary>
    public Goal(double x, double y, int width, int height)
        : base(x, y, width, height, new List<Layer> { Layer.Player }) { }

    /// <summary>플레이어가 닿으면 씬에 레벨 전환을 예약.</summary>
    public override void OnEnter(Player actor, Scene scene) => scene.RequestAdvance();
}

/// <summary>한 레벨의 모든 요소를 보유하고 업데이트 순서를 조율 (렌더는 Platform/Renderer).</summary>
public class Scene
{
    public Camera Camera;
    public int LevelIndex;
    public TileMap Tilemap = null!;
    public List<Checkpoint> Checkpoints = null!;
    public List<Goal> Goals = null!;
    public Player Player = null!;
    private (double X, double Y) _respawnPoint;
    private bool _dead;
    private bool _advancePending;   // 골 도달 → 다음 레벨 전환 예약

    /// <summary>카메라를 만들고 첫 레벨을 로드.</summary>
    public Scene()
    {
        Camera = new Camera();
        LevelIndex = 0;
        _advancePending = false;
        LoadLevel(LevelIndex);
    }

    /// <summary>레벨 시퀀스의 index 맵을 로드하고 플레이어/체크포인트/골/카메라를 초기화.</summary>
    private void LoadLevel(int index)
    {
        LevelIndex = index;
        string? mapFile = index < Settings.LEVEL_FILES.Length ? Settings.LEVEL_FILES[index] : null;
        Tilemap = new TileMap(mapFile: mapFile);
        Checkpoints = new List<Checkpoint>();
        foreach (var r in Tilemap.CheckpointRects)
            Checkpoints.Add(new Checkpoint(r.X, r.Y, r.Width, r.Height));
        Goals = new List<Goal>();
        foreach (var r in Tilemap.GoalRects)
            Goals.Add(new Goal(r.X, r.Y, r.Width, r.Height));
        _respawnPoint = (Tilemap.Spawn.X, Tilemap.Spawn.Y);
        Player = new Player(_respawnPoint.X, _respawnPoint.Y);
        _dead = false;
        _advancePending = false;
        Camera.SnapTo(Player.Rect, Tilemap.Width, Tilemap.Height);
        Sound.PlayMusic("stage1");   // 배경음 루프 (이미 재생 중이면 무시)
    }

    /// <summary>골 트리거가 호출 — 이번 프레임 끝에 다음 레벨로 전환하도록 예약 (1회 골 효과음).</summary>
    public void RequestAdvance()
    {
        if (!_advancePending)
            Sound.Play("goal");
        _advancePending = true;
    }

    /// <summary>다음 레벨로 전환 (마지막 레벨이면 그대로 유지 = 클리어).</summary>
    public void NextLevel()
    {
        if (LevelIndex + 1 < Settings.LEVEL_FILES.Length)
            LoadLevel(LevelIndex + 1);
    }

    // ── 업데이트 ────────────────────────────────────────────────
    /// <summary>발판→탑승/끼임→플레이어→트리거→사망/리스폰 순으로 한 프레임 처리.</summary>
    public void Update(PlayerInput inp)
    {
        if (Camera.Sliding)   // 방 전환 슬라이드 중 = 게임플레이 정지, 카메라만 이동
        {
            Camera.Update(Player.Rect, Tilemap.Width, Tilemap.Height);
            return;
        }
        var rider = RidingPlatform();   // 발판 이동 전 탑승 판정
        Tilemap.Update();               // 발판 이동
        CarryAndPush(rider);            // 캐리/밀기/끼임
        var solids = Tilemap.SolidRects();
        var hazardRects = new List<RectI>();   // 레이캐스트 차단용 가시
        foreach (var hz in Tilemap.Hazards)
            hazardRects.Add(hz.Rect);
        var grabbables = BuildGrabbables();
        Player.Update(inp, solids, grabbables, hazardRects);
        foreach (var ntt in Tilemap.Ntts)      // NTT 밀쳐짐/복귀 이동
            ntt.Update(solids);
        foreach (var en in Tilemap.Enemies)    // 적 무적/밀쳐짐/복귀/리스폰
            en.Update(solids);
        CheckTriggers();                // 위험(사망)·체크포인트·골
        if (_advancePending)            // 골 도달 → 다음 레벨 전환(이번 프레임 종료)
        {
            _advancePending = false;
            NextLevel();
            return;
        }
        if (Player.Crushed || _dead || Player.Y > Tilemap.Height + 100)
            Respawn();
        if (Camera.Update(Player.Rect, Tilemap.Width, Tilemap.Height))
            Player.Dashes = Settings.MAX_DASHES;   // 방 전환 시작 → 대시 1회 초기화
    }

    /// <summary>잡기 대상 목록 — NTT 전부 + 잡을 수 있는 적 (순서: NTT→적).</summary>
    private List<IGrabbable> BuildGrabbables()
    {
        var list = new List<IGrabbable>();
        foreach (var ntt in Tilemap.Ntts)
            list.Add(ntt);
        foreach (var en in Tilemap.Enemies)
            if (en.Grabbable())
                list.Add(en);
        return list;
    }

    /// <summary>모든 위험/체크포인트/골에 발동 검사 (잡기 중엔 발사형 트리거 스킵).</summary>
    private void CheckTriggers()
    {
        foreach (var hz in Tilemap.Hazards)
            hz.TryTrigger(Player, Layer.Player, this);
        foreach (var cp in Checkpoints)
            cp.TryTrigger(Player, Layer.Player, this);
        foreach (var g in Goals)
            g.TryTrigger(Player, Layer.Player, this);
        if (PlayerGrabbing())   // 잡기 중엔 점프패드/스프링 무시(NTT 겹침 무한 잡기 방지)
            return;
        foreach (var jp in Tilemap.JumpPads)
            jp.TryTrigger(Player, Layer.Player, this);
        foreach (var sp in Tilemap.Springs)
            sp.TryTrigger(Player, Layer.Player, this);
    }

    /// <summary>플레이어가 잡기(SEEKING/ACTIVE) 상태인지 여부.</summary>
    private bool PlayerGrabbing()
        => Player.State == PlayerState.GrabSeeking || Player.State == PlayerState.GrabActive;

    // ── 사망 / 리스폰 / 체크포인트 ──────────────────────────────
    /// <summary>사망 요청 (위험 트리거 등이 호출).</summary>
    public void Kill() => _dead = true;

    /// <summary>해당 체크포인트를 활성으로 만들고 리스폰 지점을 갱신.</summary>
    public void SetCheckpoint(Checkpoint cp)
    {
        if (cp.Active)
            return;
        foreach (var c in Checkpoints)
            c.Active = false;
        cp.Active = true;
        _respawnPoint = (cp.X, cp.Y);
        Sound.Play("checkpoint");
    }

    /// <summary>플레이어를 마지막 리스폰 지점에서 재생성하고 잡힌 NTT를 풀어주며 카메라를 스냅.</summary>
    public void Respawn()
    {
        Sound.Play("death");
        Player = new Player(_respawnPoint.X, _respawnPoint.Y);
        _dead = false;
        foreach (var ntt in Tilemap.Ntts)
            ntt.Grabbed = false;     // 잡은 채 사망 시 대상이 얼지 않게 해제
        foreach (var en in Tilemap.Enemies)
            en.Grabbed = false;
        Camera.SnapTo(Player.Rect, Tilemap.Width, Tilemap.Height);
    }

    // ── 움직이는 발판 탑승/밀기/끼임 ────────────────────────────
    /// <summary>플레이어 발밑(1px)에 닿은 발판을 반환 (상승 중이면 제외).</summary>
    private MovingPlatform? RidingPlatform()
    {
        if (Player.Vy < 0)
            return null;
        var feet = Player.Rect.Move(0, 1);
        foreach (var plat in Tilemap.Platforms)
            if (feet.CollideRect(plat.Rect))
                return plat;
        return null;
    }

    /// <summary>탑승 발판은 캐리(겹치면 진행 반대로 탈출, 못하면 끼임), 그 외 겹친 발판은 밀어냄.</summary>
    private void CarryAndPush(MovingPlatform? rider)
    {
        var p = Player;
        p.Crushed = false;
        if (rider != null)
        {
            p.X += rider.Dx;
            p.Y += rider.Dy;
            p.RideVx = rider.Vx;
            p.RideVy = rider.Vy;
            if (ResolveCarry(rider))   // 캐리 후 지형 겹침 → 탈출 시도/샌드위치 끼임
                p.Crushed = true;
        }
        else
        {
            p.RideVx = p.RideVy = 0.0;
        }
        foreach (var plat in Tilemap.Platforms)
        {
            if (ReferenceEquals(plat, rider))
                continue;
            if (p.Rect.CollideRect(plat.Rect))
            {
                PushPlayer(plat);
                if (Pinned(exclude: plat))
                    p.Crushed = true;
            }
        }
    }

    /// <summary>캐리 후 다른 솔리드와 겹치면 발판 진행 반대로 밀어내 탈출. 그래도 발판과 겹치면 샌드위치 끼임(true).</summary>
    private bool ResolveCarry(MovingPlatform rider)
    {
        var hit = OverlappingSolid(exclude: rider);
        if (hit == null)
            return false;
        var r = Player.Rect;
        var h = hit.Value;
        // 발판 진행 방향의 반대로 솔리드 밖으로 스냅 (착지·벽 슬라이드는 탈출=생존)
        if (rider.Dy > 0) r.Bottom = h.Top;
        else if (rider.Dy < 0) r.Top = h.Bottom;
        if (rider.Dx > 0) r.Right = h.Left;
        else if (rider.Dx < 0) r.Left = h.Right;
        Player.X = r.X;
        Player.Y = r.Y;
        // 탈출시켰는데도 발판 본체와 겹치면 = 발판↔솔리드 사이 끼임(샌드위치) → 사망
        return r.CollideRect(rider.Rect);
    }

    /// <summary>exclude 발판을 뺀 솔리드/발판 중 플레이어와 겹치는 첫 Rect를 반환 (없으면 null).</summary>
    private RectI? OverlappingSolid(MovingPlatform exclude)
    {
        var r = Player.Rect;
        int idx = r.CollideList(Tilemap.Solids);
        if (idx != -1)
            return Tilemap.Solids[idx];
        foreach (var plat in Tilemap.Platforms)
            if (!ReferenceEquals(plat, exclude) && r.CollideRect(plat.Rect))
                return plat.Rect;
        return null;
    }

    /// <summary>발판 이동 방향(dx/dy)으로 플레이어를 발판 밖으로 밀어냄.</summary>
    private void PushPlayer(MovingPlatform plat)
    {
        var r = Player.Rect;
        if (plat.Dx > 0) r.Left = plat.Rect.Right;
        else if (plat.Dx < 0) r.Right = plat.Rect.Left;
        if (plat.Dy > 0) r.Top = plat.Rect.Bottom;
        else if (plat.Dy < 0) r.Bottom = plat.Rect.Top;
        Player.X = r.X;
        Player.Y = r.Y;
    }

    /// <summary>exclude 발판을 뺀 모든 솔리드와 플레이어가 겹치면 핀(끼임) true.</summary>
    private bool Pinned(MovingPlatform exclude)
    {
        var r = Player.Rect;
        if (r.CollideList(Tilemap.Solids) != -1)
            return true;
        foreach (var plat in Tilemap.Platforms)
            if (!ReferenceEquals(plat, exclude) && r.CollideRect(plat.Rect))
                return true;
        return false;
    }
}
