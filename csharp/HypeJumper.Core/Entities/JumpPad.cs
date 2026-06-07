using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>플레이어가 닿으면 위로 강제 발사하는 트리거. 수평 관성 유지, 대시 1회 충전.</summary>
public class JumpPad : Trigger
{
    /// <summary>PLAYER만 반응하도록 설정 (재발동 쿨다운으로 중첩 폭점프 방지).</summary>
    public JumpPad(double x, double y, int width, int height)
        : base(x, y, width, height, new List<Layer> { Layer.Player }, Settings.JUMP_PAD_COOLDOWN) { }

    /// <summary>위 방향 발사 — 수평 관성은 유지(vxExternal=null)하고 대시 충전.</summary>
    public override void OnEnter(Player actor, Scene scene)
    {
        Sound.Play("jumppad");
        actor.Launch(vxExternal: null, vy: Settings.JUMP_PAD_SPEED, refillDash: true);
    }
}
