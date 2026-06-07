using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>엔티티가 속하는 충돌 레이어 (Trigger의 TargetLayers 필터에 사용).</summary>
public enum Layer
{
    Wall,      // 막히는 레이어 (타일맵, Solid)
    Hazard,    // 닿으면 죽는 레이어 (가시, 톱니, 투사체)
    Grabable,  // 잡기 가능 레이어 (NTT, 적)
    Player,    // 플레이어 레이어
}

/// <summary>레이어 간 반응 규칙 — (출발,대상) → 동작. 미정의는 "ignore".</summary>
public static class LayerRules
{
    private static readonly Dictionary<(Layer, Layer), string> Rules = new()
    {
        { (Layer.Player, Layer.Wall), "block" },       // 막힘
        { (Layer.Player, Layer.Hazard), "death" },     // 플레이어 사망
        { (Layer.Player, Layer.Grabable), "pass" },    // 통과 (잡기만 가능)
        { (Layer.Grabable, Layer.Wall), "block" },     // 막힘
        { (Layer.Grabable, Layer.Hazard), "ignore" },  // 무시
    };

    /// <summary>두 레이어가 겹쳤을 때의 동작을 반환 (block/death/pass/ignore).</summary>
    public static string Interaction(Layer src, Layer dst)
        => Rules.TryGetValue((src, dst), out var v) ? v : "ignore";
}
