using System;
using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>잡기 가능한 기본 엔티티 — 고정형(중력 없음), 잡히면 플레이어가 위치를 고정한다.</summary>
public class NTT : Actor, IGrabbable
{
    public Layer Layer = Layer.Grabable;
    public (double X, double Y) Origin;   // 밀쳐진 뒤 복귀할 원래 위치
    public bool Grabbed { get; set; }     // GRAB_ACTIVE 중 플레이어가 잡고 있는지
    public int PushTimer;                  // 밀쳐진 후 복귀 시작까지 남은 프레임
    public bool Returning;                 // origin 복귀 중 여부

    /// <summary>위치·원점·잡힘/밀쳐짐 상태를 초기화하고 GRABABLE 레이어로 설정.</summary>
    public NTT(double x, double y) : base(x, y, Settings.NTT_WIDTH, Settings.NTT_HEIGHT)
    {
        Origin = (x, y);
        Grabbed = false;
        PushTimer = 0;
        Returning = false;
    }

    /// <summary>현재 잡을 수 있는 상태인지 (기본 NTT는 항상 가능).</summary>
    public virtual bool Grabbable() => true;

    /// <summary>플레이어가 잡는 순간 — 밀쳐짐/복귀를 끄고 속도를 0으로.</summary>
    public virtual void OnGrab()
    {
        Grabbed = true;
        PushTimer = 0;
        Returning = false;
        VxExternal = 0.0;
        Vy = 0.0;
    }

    /// <summary>릴리즈 — 발사 반대 방향(pushX/Y, -1~1)으로 밀쳐지고 복귀 타이머 시작.</summary>
    public virtual void OnRelease(int pushX, int pushY)
    {
        Grabbed = false;
        VxInput = 0.0;
        VxExternal = pushX * Settings.PUSH_SPEED;
        Vy = pushY * Settings.PUSH_SPEED;
        PushTimer = Settings.PUSH_RETURN_TIME;
        Returning = false;
    }

    /// <summary>릴리즈 시 가산 속도 — 기본 NTT는 0 (줄 NTT만 진자 접선속도).</summary>
    public virtual (double X, double Y) ReleaseVelocity() => (0.0, 0.0);

    /// <summary>잡힌 상태면 정지(플레이어가 고정), 밀쳐졌으면 감속 이동 후 origin으로 복귀.</summary>
    public virtual void Update(IReadOnlyList<RectI> solids)
    {
        if (Grabbed) return;                  // 플레이어가 매 프레임 위치를 덮어씀(anchor)
        if (PushTimer > 0) UpdatePushed(solids);
        else if (Returning) UpdateReturning();
    }

    /// <summary>밀쳐진 동안 공기저항으로 감속 이동, 벽에 막히면 멈추고 타이머 종료 시 복귀로 전환.</summary>
    private void UpdatePushed(IReadOnlyList<RectI> solids)
    {
        VxInput = 0.0;
        VxExternal *= Settings.AIR_FRICTION;
        Vy *= Settings.AIR_FRICTION;
        Move(solids);
        if (Col.Left || Col.Right) VxExternal = 0.0;
        if (Col.Up || Col.Down) Vy = 0.0;
        PushTimer--;
        if (PushTimer <= 0) Returning = true;
    }

    /// <summary>origin 좌표로 PUSH_RETURN_SPEED만큼 서서히 복귀, 도달하면 복귀 종료.</summary>
    private void UpdateReturning()
    {
        var (ox, oy) = Origin;
        X = MoveMath.Approach(X, ox, Settings.PUSH_RETURN_SPEED);
        Y = MoveMath.Approach(Y, oy, Settings.PUSH_RETURN_SPEED);
        if (X == ox && Y == oy) Returning = false;
    }
}

/// <summary>줄 NTT(샹들리에형) — 천장 pivot에 매달려 상시 진자, 파괴 불가. 릴리즈 시 진자 속도 전달.</summary>
public class RopeNTT : NTT
{
    public (double X, double Y) Pivot;   // 천장 피벗 (렌더 줄 표시용)
    public double Angle;                  // 줄과 수직선이 이루는 각(라디안)
    public double AngularVel;             // 각속도
    public double Length;                 // 줄 길이

    /// <summary>피벗·각도·각속도·줄 길이를 설정하고 진자 위치에서 NTT를 초기화.</summary>
    public RopeNTT(double pivotX, double pivotY,
                   double startAngle = Settings.ROPE_START_ANGLE, double length = Settings.ROPE_LENGTH)
        : base(pivotX + Math.Sin(startAngle) * length - Settings.NTT_WIDTH / 2.0,
               pivotY + Math.Cos(startAngle) * length - Settings.NTT_HEIGHT / 2.0)
    {
        Pivot = (pivotX, pivotY);
        Angle = startAngle;
        AngularVel = 0.0;
        Length = length;
    }

    /// <summary>줄은 밀쳐지지 않음(영구 퍼즐 요소) — 잡힘만 해제하고 진자는 계속.</summary>
    public override void OnRelease(int pushX, int pushY) => Grabbed = false;

    /// <summary>현재 진자 접선속도(vx, vy)를 반환 — 릴리즈 시 플레이어에 가산.</summary>
    public override (double X, double Y) ReleaseVelocity()
    {
        double vx = AngularVel * Length * Math.Cos(Angle);
        double vy = AngularVel * Length * -Math.Sin(Angle);
        return (vx, vy);
    }

    /// <summary>잡혀도 진자 운동을 계속 갱신 (플레이어가 anchor로 따라 흔들림).</summary>
    public override void Update(IReadOnlyList<RectI> solids) => Swing();

    /// <summary>진자 공식으로 각속도·각도·위치를 갱신 (PLANNING 공식).</summary>
    private void Swing()
    {
        AngularVel += -(Settings.ROPE_GRAVITY / Length) * Math.Sin(Angle);
        Angle += AngularVel;
        var (px, py) = Pivot;
        X = px + Math.Sin(Angle) * Length - Width / 2.0;
        Y = py + Math.Cos(Angle) * Length - Height / 2.0;
    }
}
