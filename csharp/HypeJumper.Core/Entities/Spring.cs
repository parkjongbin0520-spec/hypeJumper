using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>닿으면 지정 방향으로 발사하는 트리거. direction: "up"/"left"/"right". 대시 1회 충전.</summary>
public class Spring : Trigger
{
    public string Direction;   // "up"=바닥 위 발사 / "left","right"=벽 부착 수평 발사

    /// <summary>발사 방향을 받아 트리거를 설정 (PLAYER만 반응, 재발동 쿨다운).</summary>
    public Spring(double x, double y, int width, int height, string direction = "up")
        : base(x, y, width, height, new List<Layer> { Layer.Player }, Settings.SPRING_COOLDOWN)
    {
        Direction = direction;
    }

    /// <summary>방향에 맞춰 발사 — 위는 수직, 좌우 벽 스프링은 대각으로 발사하고 대시 충전.</summary>
    public override void OnEnter(Player actor, Scene scene)
    {
        Sound.Play("spring");
        if (Direction == "up")
            actor.Launch(vy: Settings.SPRING_LAUNCH_V, refillDash: true);
        else if (Direction == "left")
            actor.Launch(vxExternal: -Settings.SPRING_SPEED, vy: Settings.SPRING_WALL_V,
                         refillDash: true, lockFrames: Settings.SPRING_FORCE_TIME);
        else if (Direction == "right")
            actor.Launch(vxExternal: Settings.SPRING_SPEED, vy: Settings.SPRING_WALL_V,
                         refillDash: true, lockFrames: Settings.SPRING_FORCE_TIME);
    }
}
