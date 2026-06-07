using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>범위 기반 발동 베이스 — 스스로 충돌을 체크해 대상 레이어가 겹치면 OnEnter 호출.</summary>
public abstract class Trigger : Entity
{
    protected List<Layer> TargetLayers;   // 이 레이어들만 발동
    public int RetriggerDelay;            // 발동 후 재발동까지 막는 프레임
    public int Cooldown;                  // 남은 쿨다운 (0이면 발동 가능)

    /// <summary>위치·크기·반응 대상 레이어와 재발동 쿨다운을 설정.</summary>
    protected Trigger(double x, double y, int width, int height, List<Layer> targetLayers, int retriggerDelay = 0)
        : base(x, y, width, height)
    {
        TargetLayers = targetLayers;
        RetriggerDelay = retriggerDelay;
        Cooldown = 0;
    }

    /// <summary>쿨다운 중이면 건너뛰고, 대상 레이어가 겹치면 OnEnter 발동 후 쿨다운 설정.</summary>
    public void TryTrigger(Player actor, Layer actorLayer, Scene scene)
    {
        if (Cooldown > 0) { Cooldown--; return; }   // 재발동 방지(겹침 중첩 차단)
        if (TargetLayers.Contains(actorLayer) && Rect.CollideRect(actor.Rect))
        {
            OnEnter(actor, scene);
            Cooldown = RetriggerDelay;
        }
    }

    /// <summary>대상이 범위에 들어왔을 때 동작 (하위 클래스가 구현).</summary>
    public abstract void OnEnter(Player actor, Scene scene);
}
