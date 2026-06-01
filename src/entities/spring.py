"""스프링 (Celeste 방식) — 벽/바닥 부착, 닿으면 부착 방향으로 강제 발사. 대시 1회 충전."""

import pygame

import settings as S
from src import assets
from src import audio
from src.layer import Layer
from src.entities.trigger import Trigger


class Spring(Trigger):
    """닿으면 지정 방향으로 발사하는 트리거. direction: 'up'/'left'/'right' (target=[PLAYER])."""

    def __init__(self, x, y, width, height, direction="up"):
        """발사 방향을 받아 트리거를 설정 (PLAYER만 반응, 재발동 쿨다운)."""
        super().__init__(x, y, width, height, [Layer.PLAYER], retrigger_delay=S.SPRING_COOLDOWN)
        self.direction = direction  # 'up'=바닥 부착 위 발사 / 'left','right'=벽 부착 수평 발사

    def on_enter(self, actor, scene):
        """방향에 맞춰 발사 — 위는 수직, 좌우 벽 스프링은 대각(수평+위)으로 발사하고 대시 충전."""
        audio.play("spring")
        if self.direction == "up":
            actor.launch(vy=S.SPRING_LAUNCH_V, refill_dash=True)
        elif self.direction == "left":
            actor.launch(vx_external=-S.SPRING_SPEED, vy=S.SPRING_WALL_V,
                         refill_dash=True, lock_frames=S.SPRING_FORCE_TIME)
        elif self.direction == "right":
            actor.launch(vx_external=S.SPRING_SPEED, vy=S.SPRING_WALL_V,
                         refill_dash=True, lock_frames=S.SPRING_FORCE_TIME)

    def draw(self, surface, offset=(0, 0)):
        """방향별 스프링 스프라이트(위/벽)로, 없으면 스프링 색 사각형으로 렌더."""
        name = "spring_up" if self.direction == "up" else "spring_wall"
        assets.blit_or_rect(surface, name, self.rect, S.COLOR_SPRING, offset)
