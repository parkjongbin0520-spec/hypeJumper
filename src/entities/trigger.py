"""범위 기반 발동 엔티티 베이스 — target_layers로 반응 대상을 필터링."""

from src.entities.entity import Entity


class Trigger(Entity):
    """스스로 충돌을 체크해 대상 레이어가 겹치면 on_enter를 부르는 베이스 (JumpPad/Spring/Hazard의 부모)."""

    def __init__(self, x, y, width, height, target_layers, retrigger_delay=0):
        """위치·크기·반응 대상 레이어와 재발동 쿨다운(0이면 매 프레임 발동)을 설정."""
        super().__init__(x, y, width, height)
        self.target_layers = target_layers   # 이 레이어들만 발동시킴
        self.retrigger_delay = retrigger_delay  # 발동 후 재발동까지 막는 프레임 (점프패드/스프링용)
        self.cooldown = 0                    # 남은 쿨다운 (0이면 발동 가능)

    def try_trigger(self, actor, actor_layer, scene):
        """쿨다운 중이면 건너뛰고, actor의 레이어가 대상이며 범위가 겹치면 on_enter 발동 후 쿨다운 설정."""
        if self.cooldown > 0:                # 쿨다운 중 — 재발동 방지(겹침 중첩 차단)
            self.cooldown -= 1
            return
        if actor_layer in self.target_layers and self.rect.colliderect(actor.rect):
            self.on_enter(actor, scene)
            self.cooldown = self.retrigger_delay

    def on_enter(self, actor, scene):
        """대상이 범위에 들어왔을 때 동작 (하위 클래스가 구현)."""
        pass
