"""점프패드 — 닿으면 강제 위쪽 발사. 수평 관성(vx_external) 유지, 대시 1회 충전."""

import pygame

import settings as S
from src import assets
from src import audio
from src.layer import Layer
from src.entities.trigger import Trigger


class JumpPad(Trigger):
    """플레이어가 닿으면 위로 강제 발사하는 트리거 (target_layers=[PLAYER])."""

    def __init__(self, x, y, width, height):
        """PLAYER만 반응하도록 트리거를 설정 (재발동 쿨다운으로 중첩 폭점프 방지)."""
        super().__init__(x, y, width, height, [Layer.PLAYER], retrigger_delay=S.JUMP_PAD_COOLDOWN)

    def on_enter(self, actor, scene):
        """위 방향 발사 — 수평 관성은 유지(vx_external=None)하고 대시 충전."""
        audio.play("jumppad")
        actor.launch(vx_external=None, vy=S.JUMP_PAD_SPEED, refill_dash=True)

    def draw(self, surface, offset=(0, 0)):
        """점프패드 스프라이트(있으면)로, 없으면 점프패드 색 사각형으로 렌더."""
        assets.blit_or_rect(surface, "jumppad", self.rect, S.COLOR_JUMP_PAD, offset)
