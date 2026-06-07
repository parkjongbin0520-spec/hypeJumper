using System.Collections.Generic;

namespace HypeJumper.Core;

/// <summary>닿으면 죽는 정적 위험 (가시). target=[Player].</summary>
public class Hazard : Trigger
{
    /// <summary>PLAYER만 반응하도록 트리거를 설정.</summary>
    public Hazard(double x, double y, int width, int height)
        : base(x, y, width, height, new List<Layer> { Layer.Player }) { }

    /// <summary>플레이어가 닿으면 씬에 사망을 요청.</summary>
    public override void OnEnter(Player actor, Scene scene) => scene.Kill();
}
