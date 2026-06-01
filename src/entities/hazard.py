"""위험 요소 — 가시/톱니 등. PLAYER가 닿으면 사망 처리."""

import pygame

import settings as S
from src.layer import Layer
from src.entities.trigger import Trigger


class Hazard(Trigger):
    """닿으면 죽는 정적 위험 (가시). target_layers=[PLAYER]."""

    def __init__(self, x, y, width, height):
        """PLAYER만 반응하도록 트리거를 설정."""
        super().__init__(x, y, width, height, [Layer.PLAYER])

    def on_enter(self, actor, scene):
        """플레이어가 닿으면 씬에 사망을 요청."""
        scene.kill()

    def draw(self, surface, offset=(0, 0)):
        """카메라 오프셋을 적용해 위험 색으로 렌더."""
        pygame.draw.rect(surface, S.COLOR_HAZARD, self.rect.move(-offset[0], -offset[1]))
