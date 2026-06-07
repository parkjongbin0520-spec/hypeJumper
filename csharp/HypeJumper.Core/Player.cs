using System;
using System.Collections.Generic;
using System.Linq;

namespace HypeJumper.Core;

/// <summary>플레이어 본체 — 상태머신, 통합 버퍼, NORMAL 이동/대시/고급 무브/잡기 (player.py 1:1 이식).</summary>
/// <remarks>렌더 전용(스프라이트 선택/조준 그리기/_anim_t)은 Platform/Renderer로 분리. 물리 상태는 double.</remarks>
public class Player : Actor
{
    public PlayerState State;
    // 통합 버퍼 4종
    public InputBuffer GroundCoyote;
    public InputBuffer WallCoyote;
    public InputBuffer JumpBuf;
    public InputBuffer WallJumpBuf;
    // 프레임 상태 플래그
    public bool OnGround;
    public bool OnWall;
    public int WallDir;            // 현재 접촉 벽 방향 (-1 좌, +1 우)
    public int LastWallDir;        // 마지막 접촉 벽 (월 코요테용)
    public int Facing = 1;         // 바라보는 방향 (-1 좌, +1 우)
    public bool WallSliding;
    public bool FastFall;
    public bool IsDucking;
    public int CeilingStick;       // 천장 밀착 남은 프레임
    public bool NearGround;        // 바닥 근접 (점프 종류 판정)
    private bool _prevOnGround;    // 직전 접지 (착지음 엣지)
    public bool WjInputLock;       // 월 점프 후 '벽 쪽' 입력 무시
    public double RideVx;          // 탑승 발판 수평 속도 (관성 전달)
    public double RideVy;          // 탑승 발판 수직 속도
    public bool Crushed;           // 발판 끼임(사망 신호)
    // 대시 (Phase 2)
    public int Dashes;
    public int DashTimer;
    private (int X, int Y) _dashDir;
    private double _dashVx;
    private double _dashVy;
    // 고급 무브먼트 (Phase 2.5)
    public InputBuffer DashJumpBuf;
    public InputBuffer WallBounceBuf;
    private bool _wallBounceDown;
    public (int X, int Y) LastDashDir;
    public int WallNearDir;
    public int HangTimer;
    private double _hangGrav = 1.0;
    private bool _hangDescendOnly;
    public string LastTech = "";
    public int TechFlash;
    public int LaunchLock;
    public int LaunchLockDir;
    // 잡기 (Phase 3C)
    public IGrabbable? GrabTarget;
    public bool GrabOk;
    public int GrabTimer;
    public int GrabReadyTimer;
    public int AimTimer;
    public bool AimSlow;

    /// <summary>위치·상태·버퍼·플래그를 초기화.</summary>
    public Player(double x, double y) : base(x, y, Settings.PLAYER_WIDTH, Settings.NORMAL_HITBOX_HEIGHT)
    {
        State = PlayerState.Normal;
        GroundCoyote = new InputBuffer(Settings.COYOTE_TIME);
        WallCoyote = new InputBuffer(Settings.WALL_COYOTE_TIME);
        JumpBuf = new InputBuffer(Settings.JUMP_BUFFER);
        WallJumpBuf = new InputBuffer(Settings.WALL_JUMP_BUFFER);
        DashJumpBuf = new InputBuffer(Settings.AUTO_JUMP_BUFFER);
        WallBounceBuf = new InputBuffer(Settings.WALL_BOUNCE_BUFFER);
        Dashes = Settings.MAX_DASHES;
        _dashDir = (0, 0);
        LastDashDir = (0, 0);
    }

    // ── 메인 업데이트 ───────────────────────────────────────────
    /// <summary>상태에 맞는 로직을 실행. grabbables=잡기 대상, hazards=레이캐스트 차단용 rect.</summary>
    public void Update(PlayerInput inp, IReadOnlyList<RectI> solids,
                       IReadOnlyList<IGrabbable>? grabbables = null, IReadOnlyList<RectI>? hazards = null)
    {
        grabbables ??= Array.Empty<IGrabbable>();
        hazards ??= Array.Empty<RectI>();
        if (State == PlayerState.Normal)
            UpdateNormal(inp, solids, grabbables, hazards);
        else if (State == PlayerState.Dash)
            UpdateDash(inp, solids);
        else if (State == PlayerState.GrabSeeking || State == PlayerState.GrabActive)
            UpdateGrabbing(inp, solids, grabbables, hazards);
        // DashStrike enum은 예약만 (기능 미사용)
    }

    /// <summary>NORMAL 한 프레임 — Z 누름 시 조준 윈도우(슬로우) 진입, 아니면 일반 이동.</summary>
    private void UpdateNormal(PlayerInput inp, IReadOnlyList<RectI> solids,
                             IReadOnlyList<IGrabbable> grabbables, IReadOnlyList<RectI> hazards)
    {
        if (inp.GrabPressed)   // Z 누름(엣지) → 짧은 조준 윈도우 + 슬로우 시작
        {
            State = PlayerState.GrabSeeking;
            AimTimer = Settings.GRAB_AIM_TIME;
            AimSlow = true;
            UpdateGrabbing(inp, solids, grabbables, hazards);
            return;
        }
        NormalMovement(inp, solids);
    }

    /// <summary>일반 이동 파이프라인 — 대시 트리거 우선, 그 외 입력→이동→충돌 순서.</summary>
    private void NormalMovement(PlayerInput inp, IReadOnlyList<RectI> solids)
    {
        if (DashTriggered(inp) && StartDash(inp))
        {
            UpdateDash(inp, solids);    // 트리거 프레임에서 바로 대시 시작
            return;
        }
        PreState(inp);          // 직전 접촉 정보로 슬라이드/패스트폴 판정
        UpdateDuck(inp);        // 웅크리기 진입/해제 (히트박스 높이)
        UpdateHorizontal(inp);  // 수평 속도 (입력/외적)
        HandleJump(inp);        // 점프/월점프 (버퍼+코요테)
        ApplyVertical(inp);     // 중력/낙하상한/천장밀착
        Move(solids);           // Actor 축분리 충돌 → Col 갱신
        ResolveLanding();       // 착지/천장 처리 (vy 정리)
        UpdateContacts(solids); // 1px 프로브로 지상/벽 접촉 갱신 + 코요테 충전
        TickBuffers();          // 버퍼 카운트다운
    }

    // ── 상태 판정 ───────────────────────────────────────────────
    /// <summary>직전 프레임 접촉 플래그로 월슬라이드/패스트폴 여부만 판정.</summary>
    private void PreState(PlayerInput inp)
    {
        bool pressingWall = (inp.Right && WallDir == 1) || (inp.Left && WallDir == -1);
        bool descending = Vy > 0;
        FastFall = (!OnGround) && descending && inp.Down;
        WallSliding = OnWall && descending && pressingWall && !FastFall;
    }

    /// <summary>이동 후 1px 프로브로 지상/벽 접촉 + 바닥 근접을 판정하고 코요테 충전.</summary>
    private void UpdateContacts(IReadOnlyList<RectI> solids)
    {
        var rect = Rect;
        OnGround = rect.Move(0, 1).CollideList(solids) != -1;   // 바로 아래 1px 검사
        // 하강 중 바닥이 NEAR_GROUND_DISTANCE 안에 있으면 근접
        NearGround = Vy >= 0 && rect.Move(0, Settings.NEAR_GROUND_DISTANCE).CollideList(solids) != -1;
        if (OnGround)
        {
            GroundCoyote.Set();
            WjInputLock = false;            // 착지하면 입력 잠금 해제
            Dashes = Settings.MAX_DASHES;   // 착지/지상이면 대시 재충전
            HangTimer = 0;                  // 착지 시 체공 종료
        }
        bool touchLeft = rect.Move(-1, 0).CollideList(solids) != -1;
        bool touchRight = rect.Move(1, 0).CollideList(solids) != -1;
        OnWall = (!OnGround) && (touchLeft || touchRight);
        WallDir = touchLeft ? -1 : (touchRight ? 1 : 0);
        if (OnWall)
        {
            WallCoyote.Set();
            LastWallDir = WallDir;
        }
        WallNearDir = ProbeWallNear(solids);   // 월바운스용 넓은 벽 감지(버퍼)
        if (OnGround && !_prevOnGround)         // 공중→지상 전이 = 착지음(1회)
            Sound.Play("land");
        _prevOnGround = OnGround;
    }

    /// <summary>월바운스 버퍼 — WALL_BOUNCE_RANGE 안에 벽이 있으면 그 방향(-1/+1) 반환.</summary>
    private int ProbeWallNear(IReadOnlyList<RectI> solids)
    {
        var rect = Rect;
        var leftZone = new RectI(rect.X - Settings.WALL_BOUNCE_RANGE, rect.Y, Settings.WALL_BOUNCE_RANGE, rect.Height);
        var rightZone = new RectI(rect.Right, rect.Y, Settings.WALL_BOUNCE_RANGE, rect.Height);
        if (leftZone.CollideList(solids) != -1) return -1;
        if (rightZone.CollideList(solids) != -1) return 1;
        return 0;
    }

    // ── 수평 이동 (Approach 방식) ───────────────────────────────
    /// <summary>지상/공중에 따라 입력 속도와 외적 속도를 Approach로 갱신.</summary>
    private void UpdateHorizontal(PlayerInput inp)
    {
        if (LaunchLock > 0)   // 발사 입력 잠금: 입력 무시, 외력이 그대로 밀어냄
        {
            VxInput = 0.0;
            return;
        }
        int direction = (inp.Right ? 1 : 0) - (inp.Left ? 1 : 0);
        if (direction != 0)
            Facing = direction;     // 실제 좌우 입력이 있을 때만 바라보는 방향 갱신
        double accel, decel;
        if (OnGround)
        {
            accel = Settings.GROUND_ACCEL; decel = Settings.GROUND_DECEL;
            VxExternal = MoveMath.Approach(VxExternal, 0, Settings.SPEED_REDUCE);  // 빠른 감속
        }
        else
        {
            accel = Settings.GROUND_ACCEL * Settings.AIR_MULT; decel = Settings.GROUND_DECEL * Settings.AIR_MULT;
            VxExternal *= Settings.AIR_FRICTION;   // 공기 저항식 감쇠
        }
        if (WjInputLock)   // 월 점프 후: 벽 쪽 '연속 홀드'만 정점까지 무시
        {
            if (Vy >= 0 || direction != LastWallDir)   // 정점 도달 또는 벽쪽 입력 릴리즈 → 해제
                WjInputLock = false;
            else                                       // 벽 쪽 계속 홀드 중 → 무시
                direction = 0;
        }
        if (direction != 0)
        {
            if (VxExternal * direction > Settings.PLAYER_MAX_SPEED)
                VxInput = MoveMath.Approach(VxInput, 0, accel);     // 외적 초과: 추가 가속 불가
            else
                VxInput = MoveMath.Approach(VxInput, direction * Settings.PLAYER_MAX_SPEED, accel);
        }
        else
        {
            VxInput = MoveMath.Approach(VxInput, 0, decel);
        }
    }

    // ── 수직 / 중력 ─────────────────────────────────────────────
    /// <summary>천장 밀착/월슬라이드/일반 중력을 분기 적용하고 낙하 상한을 건다.</summary>
    private void ApplyVertical(PlayerInput inp)
    {
        if (CeilingStick > 0)   // 천장 밀착 중: 중력 정지
        {
            CeilingStick -= 1;
            Vy = 0;
            return;
        }
        if (WallSliding)        // 월 슬라이드: 느린 종단속도로 cap
        {
            Vy = Math.Min(Vy + Settings.WALL_SLIDE_GRAVITY, Settings.WALL_SLIDE_MAX_FALL);
            return;
        }
        bool applyHang = HangTimer > 0 && !(_hangDescendOnly && Vy < 0);
        Vy += GravityValue(inp);    // 체공 중력 감소 적용
        if (applyHang)              // 실제 적용된 프레임만 소진
            HangTimer -= 1;
        double maxFall = FastFall ? Settings.FAST_MAX_FALL : Settings.MAX_FALL_SPEED;
        if (Vy > maxFall)           // 낙하만 상한 (상승 vy엔 상한 없음)
            Vy = maxFall;
    }

    /// <summary>현재 상승/하강·버튼 유지·패스트폴 여부로 적용할 중력값을 선택 (체공 중 감소).</summary>
    private double GravityValue(PlayerInput inp)
    {
        double g;
        if (Vy < 0)   // 상승 중
            g = inp.JumpHeld ? Settings.GRAVITY_UP : Settings.GRAVITY_UP_RELEASE;
        else if (FastFall)
            g = Settings.FAST_FALL_GRAVITY;
        else
            g = Settings.GRAVITY_DOWN;
        if (HangTimer > 0 && !(_hangDescendOnly && Vy < 0))
            g *= _hangGrav;     // 체공 중 중력 감소
        return g;
    }

    // ── 점프 / 월 점프 ──────────────────────────────────────────
    /// <summary>점프 입력을 버퍼에 넣고, 지상/벽 조건이 맞으면 즉시 발동.</summary>
    private void HandleJump(PlayerInput inp)
    {
        // 버퍼된 월바운스: 수직대시 점프를 벽 직전에 눌렀어도 벽에 닿는 순간 자동 발동
        if (WallBounceBuf.IsActive() && WallNearDir != 0)
        {
            DoWallBounce(_wallBounceDown);
            WallBounceBuf.Consume();
            DashJumpBuf.Consume();
            return;
        }
        if (inp.JumpPressed)
        {
            JumpBuf.Set();
            WallJumpBuf.Set();
        }
        // 대시 점프 창 활성 시 슈퍼/하이퍼/월바운스 우선 (일반 점프보다 먼저)
        if (JumpBuf.IsActive() && DashJumpBuf.IsActive() && TryDashTech(inp))
        {
            JumpBuf.Consume();
            WallJumpBuf.Consume();
            DashJumpBuf.Consume();
            GroundCoyote.Consume();
            return;
        }
        // 수직대시 점프인데 아직 벽이 없음 → 월바운스 의도를 버퍼에 저장
        if (JumpBuf.IsActive() && DashJumpBuf.IsActive() && LastDashDir.Y < 0 && WallNearDir == 0)
        {
            WallBounceBuf.Set();
            _wallBounceDown = inp.Down;
            JumpBuf.Consume();
            WallJumpBuf.Consume();
            return;
        }
        if (JumpBuf.IsActive() && (OnGround || GroundCoyote.IsActive() || NearGround))
        {
            DoJump();
            JumpBuf.Consume();
            WallJumpBuf.Consume();      // 한 번 누름이 월점프로 이어지지 않게
            GroundCoyote.Consume();
        }
        else if (WallJumpBuf.IsActive() && WallJumpReady(inp))
        {
            DoWallJump();
            WallJumpBuf.Consume();
            JumpBuf.Consume();          // 한 번 누름이 일반점프로 이어지지 않게
            WallCoyote.Consume();
        }
    }

    /// <summary>월 점프 조건 — 바닥 근처가 아니고, 공중 벽(또는 벽 코요테) 접촉. 방향키 무관.</summary>
    private bool WallJumpReady(PlayerInput inp)
    {
        if (NearGround)    // 바닥 근처면 일반 점프 우선 (옆튐 방지)
            return false;
        return OnWall || WallCoyote.IsActive();
    }

    /// <summary>일반 점프 — 웅크리기 해제 후, 탑승 발판 관성을 더해 상승 속도를 부여.</summary>
    private void DoJump()
    {
        if (IsDucking)
        {
            SetHeight(Settings.NORMAL_HITBOX_HEIGHT);
            IsDucking = false;
        }
        Vy = Settings.JUMP_SPEED + RideVy * Settings.PLATFORM_INERTIA_Y;   // 발판 수직 관성
        VxExternal += RideVx * Settings.PLATFORM_INERTIA_X;               // 발판 수평 관성
        Sound.Play("jump");
    }

    /// <summary>월 점프 — 벽 반대로 외적 속도를 주고 위로 튕기며 잠시 벽 재부착을 잠금.</summary>
    private void DoWallJump()
    {
        Vy = Settings.WALL_JUMP_V;
        VxExternal = -LastWallDir * Settings.WALL_JUMP_H;
        VxInput = 0.0;
        WjInputLock = true;     // 정점까지 '벽 쪽' 입력 무시
        Sound.Play("walljump");
    }

    // ── 대시 (Phase 2) ──────────────────────────────────────────
    /// <summary>외력 발사(점프패드/스프링) — 지정 축 속도를 덮어쓰고 대시를 1회 충전. lockFrames>0이면 수평 입력 잠금.</summary>
    public void Launch(double? vxExternal = null, double? vy = null, bool refillDash = true, int lockFrames = 0)
    {
        State = PlayerState.Normal;   // 대시 중이면 강제 종료(중력 재개)
        DashTimer = 0;                // 대시 타이머 즉시 소진
        CeilingStick = 0;             // 천장 밀착 해제
        HangTimer = 0;                // 체공(부유) 해제
        // 주의: DashJumpBuf·LastDashDir은 보존 → 스프링/패드 거쳐도 슈퍼/하이퍼/월바운스 연결 유지
        if (vy.HasValue)              // 수직 발사 속도
            Vy = vy.Value;
        if (vxExternal.HasValue)      // 수평 발사 속도 (None이면 기존 관성 유지)
            VxExternal = vxExternal.Value;
        if (refillDash)               // 닿으면 대시 1회 충전
            Dashes = Settings.MAX_DASHES;
        if (lockFrames > 0 && vxExternal.HasValue)   // 수평 발사 → 입력 잠금
        {
            LaunchLock = lockFrames;
            LaunchLockDir = vxExternal.Value > 0 ? 1 : -1;
            VxInput = 0.0;
        }
    }

    /// <summary>대시 발동 조건 — 대시키 엣지 + 충전 있음 + 방향키 입력(없으면 미발동).</summary>
    private bool DashTriggered(PlayerInput inp)
    {
        bool hasDir = inp.Left || inp.Right || inp.Up || inp.Down;
        return inp.DashPressed && Dashes > 0 && hasDir;
    }

    /// <summary>8방향 정규화 벡터로 대시 속도를 고정하고 DASH 상태로 진입 (방향 없으면 실패).</summary>
    private bool StartDash(PlayerInput inp)
    {
        int dx = (inp.Right ? 1 : 0) - (inp.Left ? 1 : 0);
        int dy = (inp.Down ? 1 : 0) - (inp.Up ? 1 : 0);
        if (dx == 0 && dy == 0)   // 방향 입력 없으면 대시 안 함
            return false;
        double length = MoveMath.Hypot(dx, dy);   // 대각선 정규화 → 8방향 등속
        _dashVx = dx / length * Settings.DASH_SPEED;
        _dashVy = dy / length * Settings.DASH_SPEED;
        _dashDir = (dx, dy);
        LastDashDir = (dx, dy);   // 슈퍼/하이퍼 판정용 (종료 후에도 유지)
        State = PlayerState.Dash;
        DashTimer = Settings.DASH_TIME;
        Dashes -= 1;
        Sound.Play("dash");
        if (IsDucking)            // 웅크리기 중 대시 → 히트박스 복구
        {
            SetHeight(Settings.NORMAL_HITBOX_HEIGHT);
            IsDucking = false;
        }
        return true;
    }

    /// <summary>대시 한 프레임 — 중력 무시, 고정 속도 등속 이동, 충돌·접촉·버퍼 갱신.</summary>
    private void UpdateDash(PlayerInput inp, IReadOnlyList<RectI> solids)
    {
        VxInput = 0.0;
        VxExternal = _dashVx;     // 대시 속도로 고정 (중력/마찰 무시)
        Vy = _dashVy;
        DashTimer -= 1;
        Move(solids);
        DashResolveCollisions();  // 벽/천장/바닥에 막히면 해당 축 속도 0
        UpdateContacts(solids);
        DashJumpBuf.Set();        // 대시 중 점프 창 유지 (종료 후 카운트다운)
        TickBuffers();
        if (inp.JumpPressed && TryDashTech(inp))   // 대시 중 점프 → 슈퍼/하이퍼/월바운스
        {
            State = PlayerState.Normal;
            return;
        }
        if (DashTimer <= 0)
            EndDash();
    }

    /// <summary>대시 중 충돌 시 막힌 축 속도를 0으로 (끼임 방지, 타이머는 계속).</summary>
    private void DashResolveCollisions()
    {
        if (Col.Left || Col.Right) { VxExternal = _dashVx = 0.0; }
        if (Col.Up || Col.Down) { Vy = _dashVy = 0.0; }
    }

    /// <summary>대시 종료 — 방향별 속도 처리 후 NORMAL 복귀.</summary>
    private void EndDash()
    {
        var (dx, dy) = _dashDir;
        if (dy <= 0)   // 아래 성분(대각 아래 포함)이면 속도 유지 → 그 외만 처리
        {
            if (dx != 0)   // 수평 성분 → vx 급감속 컷
                VxExternal = Math.CopySign(Settings.END_DASH_SPEED, dx);
            if (dy < 0)    // 위 성분 → vy 감속
                Vy *= Settings.END_DASH_UP_MULT;
        }
        DashJumpBuf.Set();   // 대시 종료 시점에 창을 새로 채움 → 대시 후 AUTO_JUMP_BUFFER 프레임간 허용
        State = PlayerState.Normal;
    }

    // ── 고급 무브먼트 (Phase 2.5): 슈퍼 / 하이퍼 / 월바운스 ──────
    /// <summary>대시 점프 창에서 조건별 테크 발동 — 월바운스/슈퍼/하이퍼. 발동 시 true.</summary>
    private bool TryDashTech(PlayerInput inp)
    {
        var (dx, dy) = LastDashDir;
        if (dy < 0 && WallNearDir != 0)   // 위 성분 대시 중 벽 근접(버퍼) → 월바운스
        {
            DoWallBounce(inp.Down);
            return true;
        }
        if (OnGround || GroundCoyote.IsActive() || NearGround)
        {
            if (dy == 0 && dx != 0)   // 수평 대시 + 지상 → 슈퍼
            {
                DoSuperJump(inp);
                return true;
            }
            if (dy > 0)               // 대각/수직 아래 대시 + 지상 → 하이퍼
            {
                DoHyperJump(inp);
                return true;
            }
        }
        return false;
    }

    /// <summary>코너 부스트 — 점프 시점 방향키 부호 우선, 없으면 대시 방향 부호.</summary>
    private int BoostSign(PlayerInput inp, int fallbackDx)
    {
        int d = (inp.Right ? 1 : 0) - (inp.Left ? 1 : 0);
        if (d != 0)
            return d;
        return fallbackDx >= 0 ? 1 : -1;
    }

    /// <summary>발동 테크 이름을 HUD 표시용으로 기록하고 해당 테크 효과음 재생.</summary>
    private void FlashTech(string name)
    {
        LastTech = name;
        TechFlash = Settings.FPS / 2;   // 0.5초간 표시
        Sound.Play(name.Split('-')[0].ToLowerInvariant());   // SUPER/HYPER/WALLBOUNCE(-DIAG→wallbounce)
    }

    /// <summary>테크 체공(부유) 설정 — time 프레임 동안 중력×grav. descendOnly면 하강에만 적용.</summary>
    private void SetHang(int time, double grav, bool descendOnly = false)
    {
        HangTimer = time;
        _hangGrav = grav;
        _hangDescendOnly = descendOnly;
    }

    /// <summary>슈퍼 대시 — 큰 수평 속도 + 일반 점프 높이 + 하강 체공.</summary>
    private void DoSuperJump(PlayerInput inp)
    {
        int sign = BoostSign(inp, LastDashDir.X);
        VxExternal = sign * Settings.SUPER_JUMP_H;
        VxInput = 0.0;
        Vy = Settings.JUMP_SPEED;
        SetHang(Settings.SUPER_HANG_TIME, Settings.SUPER_HANG_GRAV, descendOnly: true);
        FlashTech("SUPER");
    }

    /// <summary>하이퍼 대시 — 더 큰 수평(×1.25) + 일반 점프 높이 + 하강 체공.</summary>
    private void DoHyperJump(PlayerInput inp)
    {
        int sign = BoostSign(inp, LastDashDir.X);
        VxExternal = sign * Settings.SUPER_JUMP_H * Settings.DUCK_SUPER_JUMP_X_MULT;
        VxInput = 0.0;
        Vy = Settings.JUMP_SPEED * Settings.DUCK_SUPER_JUMP_Y_MULT;
        SetHang(Settings.HYPER_HANG_TIME, Settings.HYPER_HANG_GRAV, descendOnly: true);
        FlashTech("HYPER");
    }

    /// <summary>월바운스 — 벽 반대로 크게 + 위(기본) 또는 아래입력 시 대각(수평 위주).</summary>
    private void DoWallBounce(bool down)
    {
        VxExternal = -WallNearDir * Settings.SUPER_WALL_JUMP_H;
        VxInput = 0.0;
        if (down)   // 대각선 월바운스: 수평 위주 빠르게
        {
            Vy = Settings.WALL_BOUNCE_DIAG_Y;
            FlashTech("WALLBOUNCE-DIAG");
        }
        else        // 기본 월바운스: 강하게 위로 + 체공 부유
        {
            Vy = Settings.SUPER_WALL_JUMP_SPEED;
            SetHang(Settings.WALL_BOUNCE_HANG_TIME, Settings.WALL_BOUNCE_HANG_GRAV);
            FlashTech("WALLBOUNCE");
        }
        WjInputLock = true;   // 벽 재부착 방지
    }

    // ── 잡기 (Phase 3C) ─────────────────────────────────────────
    /// <summary>잡은 상태(ACTIVE)는 홀드 유지·뗌 시 릴리즈, 조준 윈도우(SEEKING)는 만료 시 자동 취소.</summary>
    private void UpdateGrabbing(PlayerInput inp, IReadOnlyList<RectI> solids,
                               IReadOnlyList<IGrabbable> grabbables, IReadOnlyList<RectI> hazards)
    {
        if (State == PlayerState.GrabActive && HoldingTarget())
        {
            if (!inp.GrabHeld)   // Z 뗌 → 릴리즈(테크)
            {
                EndGrab(inp);
                return;
            }
            VxInput = VxExternal = Vy = 0.0;   // 잡은 상태만 얼음
            GrabTimer -= 1;
            AnchorTo(GrabTarget!, solids);
            if (GrabTimer <= 0)
                EndGrab(inp);                  // 시간 초과 → 강제 릴리즈
            return;
        }
        NormalMovement(inp, solids);           // 조준 윈도우 중엔 일반 이동 유지
        if (State != PlayerState.GrabSeeking && State != PlayerState.GrabActive)
            return;                            // 대시 등으로 상태 바뀌면 조준 종료
        var blockers = CombineBlockers(solids, hazards);   // 레이캐스트는 벽+가시 모두로 막힘
        (GrabTarget, GrabOk) = FindGrabTarget(grabbables, blockers);
        if (GrabTarget != null && GrabOk)
        {
            StartGrabActive(solids);           // 대상 확보 → 즉시 순간이동(얼음 시작)
            return;
        }
        AimTimer -= 1;                          // 대상 없음/막힘 → 윈도우 카운트다운
        if (AimTimer <= 0)                      // 만료 → 잡기 취소, 슬로우 해제
        {
            State = PlayerState.Normal;
            GrabTarget = null;
            AimSlow = false;
        }
    }

    /// <summary>현재 잡고 있는 대상이 유효한지 여부.</summary>
    private bool HoldingTarget() => GrabTarget != null && GrabTarget.Grabbed;

    /// <summary>순간이동 — 대상 중심으로 이동(솔리드 박힘 보정) 후 GRAB_ACTIVE 진입.</summary>
    private void StartGrabActive(IReadOnlyList<RectI> solids)
    {
        var target = GrabTarget!;
        target.OnGrab();
        AnchorTo(target, solids);
        VxInput = VxExternal = Vy = 0.0;   // 잡는 순간 속도 정리
        AimSlow = false;                   // 잡으면 조준 슬로우 종료
        State = PlayerState.GrabActive;
        GrabTimer = Settings.MAX_GRAB_TIME;
        Sound.Play("grab");
    }

    /// <summary>플레이어 중심을 대상 중심에 맞추되, 솔리드와 겹치면 위로 빼서 발을 올림.</summary>
    private void AnchorTo(IGrabbable target, IReadOnlyList<RectI> solids)
    {
        var (tcx, tcy) = target.Center;
        X = tcx - Width / 2.0;
        Y = tcy - Height / 2.0;
        int idx = Rect.CollideList(solids);
        if (idx != -1)   // 대상이 바닥 등에 박혀 겹치면 솔리드 위로 스냅
        {
            var r = Rect;
            r.Bottom = solids[idx].Top;
            X = r.X; Y = r.Y;
        }
    }

    /// <summary>Z 뗌/시간초과 — 잡은 대상이 있으면 릴리즈(테크), 없으면 NORMAL 복귀.</summary>
    private void EndGrab(PlayerInput inp)
    {
        AimSlow = false;
        if (HoldingTarget())
            ReleaseGrab(inp);
        else
        {
            State = PlayerState.Normal;
            GrabTarget = null;
        }
    }

    /// <summary>범위 내 가장 가까운 대상을 찾고, 장애물 없는 첫 대상이면 (대상,true) 반환.</summary>
    private (IGrabbable?, bool) FindGrabTarget(IReadOnlyList<IGrabbable> grabbables, List<RectI> blockers)
    {
        var (cx, cy) = Center;
        var cands = new List<(double Dist, IGrabbable Ntt)>();
        foreach (var ntt in grabbables)
        {
            var (tx, ty) = ntt.Center;
            double dist = MoveMath.Hypot(tx - cx, ty - cy);
            if (dist <= Settings.GRAB_RANGE)
                cands.Add((dist, ntt));
        }
        var ordered = cands.OrderBy(c => c.Dist).ToList();   // 안정 정렬(파이썬 sort 동일)
        foreach (var (_, ntt) in ordered)   // 가까운 순으로 시야 확인
        {
            if (HasLos(ntt, blockers))
                return (ntt, true);          // 장애물 없는 가장 가까운 대상
        }
        if (ordered.Count > 0)
            return (ordered[0].Ntt, false);  // 있지만 전부 막힘 (빨강)
        return (null, false);
    }

    /// <summary>플레이어 중심→대상 중심 직선이 벽/가시에 막히지 않으면 true (레이캐스트).</summary>
    private bool HasLos(IGrabbable target, List<RectI> blockers)
    {
        var (cx, cy) = Center;
        var (tx, ty) = target.Center;
        foreach (var b in blockers)
        {
            if (b.ClipLine(cx, cy, tx, ty))   // 차단 rect와 교차 → 장애물 있음
                return false;
        }
        return true;
    }

    /// <summary>레이캐스트 차단용 — 솔리드 + 가시 rect 합본.</summary>
    private static List<RectI> CombineBlockers(IReadOnlyList<RectI> solids, IReadOnlyList<RectI> hazards)
    {
        var list = new List<RectI>(solids.Count + hazards.Count);
        list.AddRange(solids);
        list.AddRange(hazards);
        return list;
    }

    /// <summary>릴리즈 — 위=월바운스 / 좌우=슈퍼 / 아래(±좌우)=대시 / 무입력=점프. 대상 밀쳐냄 + 대시 충전.</summary>
    private void ReleaseGrab(PlayerInput inp)
    {
        int dx = (inp.Right ? 1 : 0) - (inp.Left ? 1 : 0);
        bool up = inp.Up, down = inp.Down;
        var target = GrabTarget!;
        GrabTarget = null;
        Dashes = Settings.MAX_DASHES;   // 릴리즈 후 대시 1회 충전
        Sound.Play("release");
        var (rvx, rvy) = target.ReleaseVelocity();   // 줄 진자 접선속도(고정/적은 0)
        if (down)   // 아래/아래+좌우 → 대시 (그 방향으로 발사)
        {
            target.OnRelease(-dx, -1);   // 대상은 위로 밀쳐짐
            StartDash(inp);              // DASH 상태 진입
            return;
        }
        State = PlayerState.Normal;
        LastDashDir = (dx, up ? -1 : 0);
        int pushY;
        if (up)     // 위/위대각 → 월바운스(상승+체공)
        {
            VxExternal = dx != 0 ? BoostSign(inp, 0) * Settings.SUPER_WALL_JUMP_H : 0.0;
            VxInput = 0.0;
            Vy = Settings.SUPER_WALL_JUMP_SPEED;
            SetHang(Settings.WALL_BOUNCE_HANG_TIME, Settings.WALL_BOUNCE_HANG_GRAV);
            FlashTech("WALLBOUNCE");
            pushY = 1;
        }
        else if (dx != 0)   // 좌/우 → 슈퍼
        {
            DoSuperJump(inp);
            pushY = 0;
        }
        else                // 입력 없음 → 일반 점프
        {
            DoJump();
            pushY = 1;
        }
        VxExternal += rvx;   // 줄 NTT면 진자 접선속도 가산
        Vy += rvy;
        target.OnRelease(-dx, pushY);   // 대상은 플레이어 발사 반대로 밀쳐짐
    }

    // ── 착지 / 천장 / 벽 충돌 처리 ──────────────────────────────
    /// <summary>이동 후 충돌 결과로 착지(vy=0)·천장 밀착·벽쪽 수평속도 0을 처리.</summary>
    private void ResolveLanding()
    {
        if (Col.Down)
            Vy = 0;
        if (Col.Up)
        {
            CeilingStick = (int)Math.Min(Math.Abs(Vy) / Settings.CEILING_STICK_DIVISOR, Settings.MAX_CEILING_STICK_FRAMES);
            Vy = 0;
        }
        // 벽에 박으면 벽 쪽 수평 속도 0
        if (Col.Left)
        {
            VxInput = Math.Max(VxInput, 0.0);
            VxExternal = Math.Max(VxExternal, 0.0);
        }
        if (Col.Right)
        {
            VxInput = Math.Min(VxInput, 0.0);
            VxExternal = Math.Min(VxExternal, 0.0);
        }
    }

    // ── 웅크리기 ────────────────────────────────────────────────
    /// <summary>지상+아래 입력(이동 입력 없음)일 때만 히트박스를 낮춤.</summary>
    private void UpdateDuck(PlayerInput inp)
    {
        bool wantDuck = OnGround && inp.Down && !(inp.Left || inp.Right);
        if (wantDuck && !IsDucking)
        {
            SetHeight(Settings.DUCK_HITBOX_HEIGHT);
            IsDucking = true;
        }
        else if (!wantDuck && IsDucking)
        {
            SetHeight(Settings.NORMAL_HITBOX_HEIGHT);
            IsDucking = false;
        }
    }

    /// <summary>발밑(bottom)을 고정한 채 히트박스 높이를 변경.</summary>
    private void SetHeight(int newHeight)
    {
        Y += Height - newHeight;
        Height = newHeight;
    }

    // ── 버퍼 갱신 ───────────────────────────────────────────────
    /// <summary>지상/벽에서 떨어졌으면 코요테를, 입력 버퍼는 매 프레임 감소.</summary>
    private void TickBuffers()
    {
        if (!OnGround)
            GroundCoyote.Tick();
        if (!OnWall)
            WallCoyote.Tick();
        JumpBuf.Tick();
        WallJumpBuf.Tick();
        DashJumpBuf.Tick();        // 대시 점프 창 카운트다운
        WallBounceBuf.Tick();      // 월바운스 입력 버퍼 카운트다운
        if (LaunchLock > 0)        // 발사 입력 잠금 카운트다운
            LaunchLock -= 1;
        if (TechFlash > 0)         // 테크 이름 표시 타이머
            TechFlash -= 1;
    }
}
