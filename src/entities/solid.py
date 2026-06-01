"""막히는 오브젝트 베이스 클래스 — 이동 발판 등 Solid 계열의 부모."""

import pygame

import settings as S
from src.entities.entity import Entity


class Solid(Entity):
    """속도로 움직이며 이동 델타를 기록하는 막힘 오브젝트 (MovingPlatform의 부모)."""

    def __init__(self, x, y, width, height):
        """위치·크기와 속도(vx/vy), 프레임 이동 델타(dx/dy)를 초기화."""
        super().__init__(x, y, width, height)
        self.vx = 0.0       # 수평 속도
        self.vy = 0.0       # 수직 속도
        self.dx = 0.0       # 이번 프레임 실제 수평 이동량 (탑승 캐리용)
        self.dy = 0.0       # 이번 프레임 실제 수직 이동량 (탑승 캐리용)

    def update(self):
        """속도만큼 위치를 이동하고 실제 이동 델타를 기록 (하위에서 vx/vy 설정 후 호출)."""
        prev_x, prev_y = self.x, self.y
        self.x += self.vx
        self.y += self.vy
        self.dx = self.x - prev_x
        self.dy = self.y - prev_y

    def draw(self, surface, offset=(0, 0)):
        """카메라 오프셋을 적용해 막힘 오브젝트를 사각형으로 렌더."""
        pygame.draw.rect(surface, S.COLOR_SOLID, self.rect.move(-offset[0], -offset[1]))
