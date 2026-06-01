"""충돌 레이어 정의 — 엔티티 간 반응 규칙을 한 곳에서 관리."""

from enum import Enum, auto


class Layer(Enum):
    """엔티티가 속하는 충돌 레이어 (Trigger의 target_layers 필터에 사용)."""
    WALL = auto()      # 막히는 레이어 (타일맵, Solid)
    HAZARD = auto()    # 닿으면 죽는 레이어 (가시, 톱니, 투사체)
    GRABABLE = auto()  # 잡기 가능 레이어 (NTT, 적)
    PLAYER = auto()    # 플레이어 레이어


# 레이어 간 반응 규칙 — (출발, 대상) → 동작 문자열. 미정의 조합은 "ignore".
_RULES = {
    (Layer.PLAYER, Layer.WALL): "block",      # 막힘
    (Layer.PLAYER, Layer.HAZARD): "death",    # 플레이어 사망
    (Layer.PLAYER, Layer.GRABABLE): "pass",   # 통과 (잡기만 가능)
    (Layer.GRABABLE, Layer.WALL): "block",    # 막힘
    (Layer.GRABABLE, Layer.HAZARD): "ignore", # 무시
}


def interaction(src, dst):
    """두 레이어가 겹쳤을 때의 동작을 반환 (block/death/pass/ignore)."""
    return _RULES.get((src, dst), "ignore")
