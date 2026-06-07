using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>잡기 가능한 적 — NTT와 동일한 잡기 인터페이스 + HP/피격/파괴/리스폰. 기본 HP1.</summary>
public class Enemy : Actor, IGrabbable
{
    public Layer Layer = Layer.Grabable;
    public (double X, double Y) Origin;
    public int MaxHp;
    public int Hp;
    public bool Grabbed { get; set; }     // 플레이어가 잡고 있는지
    public bool Destroyed;                  // 파괴(숨김·충돌X) 상태
    public int RespawnTimer;               // 파괴 후 재생성까지 남은 프레임
    public int InvincibleTimer;            // 피격 후 무적(재잡기 불가·투명) 남은 프레임
    public int PushTimer;                  // 밀쳐진 후 복귀 시작까지 남은 프레임
    public bool Returning;                 // origin 복귀 중

    /// <summary>위치·원점·HP·잡힘/무적/파괴 상태를 초기화하고 GRABABLE 레이어로 설정.</summary>
    public Enemy(double x, double y, int hp = 1) : base(x, y, Settings.ENEMY_WIDTH, Settings.ENEMY_HEIGHT)
    {
        Origin = (x, y);
        MaxHp = hp;
        Hp = hp;
        Grabbed = false;
        Destroyed = false;
        RespawnTimer = 0;
        InvincibleTimer = 0;
        PushTimer = 0;
        Returning = false;
    }

    /// <summary>현재 잡을 수 있는 상태인지 (파괴/무적 중이면 불가).</summary>
    public bool Grabbable() => !Destroyed && InvincibleTimer <= 0;

    /// <summary>잡히는 순간 — 밀쳐짐/복귀를 끄고 속도를 0으로.</summary>
    public void OnGrab()
    {
        Grabbed = true;
        PushTimer = 0;
        Returning = false;
        VxExternal = 0.0;
        Vy = 0.0;
    }

    /// <summary>릴리즈 — 발사 반대로 밀쳐지며 피격(HP 감소). HP0이면 파괴, 아니면 무적+복귀.</summary>
    public void OnRelease(int pushX, int pushY)
    {
        Grabbed = false;
        VxInput = 0.0;
        VxExternal = pushX * Settings.PUSH_SPEED;
        Vy = pushY * Settings.PUSH_SPEED;
        TakeHit();
    }

    /// <summary>줄 NTT 전용 — 적은 가산 속도 없음.</summary>
    public (double X, double Y) ReleaseVelocity() => (0.0, 0.0);

    /// <summary>HP를 1 깎고, 0이면 파괴(리스폰 예약), 남으면 무적 프레임 + 복귀 예약.</summary>
    private void TakeHit()
    {
        Hp -= 1;
        if (Hp <= 0)
        {
            Destroyed = true;
            RespawnTimer = Settings.RESPAWN_TIME;
            Sound.Play("enemy_break");
        }
        else
        {
            InvincibleTimer = Settings.INVINCIBLE_TIME;
            PushTimer = Settings.PUSH_RETURN_TIME;
            Sound.Play("enemy_hit");
        }
    }

    /// <summary>파괴 중이면 리스폰 카운트다운, 잡힌 중이면 정지, 그 외 무적·밀쳐짐·복귀 처리.</summary>
    public void Update(IReadOnlyList<RectI> solids)
    {
        if (Destroyed)
        {
            RespawnTimer--;
            if (RespawnTimer <= 0) Respawn();
            return;
        }
        if (InvincibleTimer > 0) InvincibleTimer--;
        if (Grabbed) return;                  // 플레이어가 위치를 고정(anchor)
        if (PushTimer > 0) UpdatePushed(solids);
        else if (Returning) UpdateReturning();
    }

    /// <summary>밀쳐진 동안 공기저항 감속 이동, 벽에 막히면 멈추고 타이머 끝나면 복귀로 전환.</summary>
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

    /// <summary>origin으로 PUSH_RETURN_SPEED만큼 복귀, 도달하면 종료.</summary>
    private void UpdateReturning()
    {
        var (ox, oy) = Origin;
        X = MoveMath.Approach(X, ox, Settings.PUSH_RETURN_SPEED);
        Y = MoveMath.Approach(Y, oy, Settings.PUSH_RETURN_SPEED);
        if (X == ox && Y == oy) Returning = false;
    }

    /// <summary>원점에서 HP 가득 채워 재생성.</summary>
    private void Respawn()
    {
        Destroyed = false;
        Hp = MaxHp;
        (X, Y) = Origin;
        VxInput = VxExternal = Vy = 0.0;
        InvincibleTimer = 0;
        PushTimer = 0;
        Returning = false;
    }
}

/// <summary>강화 적 — 기본 HP2+, Enemy 로직 재사용.</summary>
public class ArmoredEnemy : Enemy
{
    /// <summary>HP를 강화 값으로 설정해 Enemy 초기화.</summary>
    public ArmoredEnemy(double x, double y) : base(x, y, Settings.ARMORED_ENEMY_HP) { }
}
