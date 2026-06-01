"""움직이는 발판 — 지정 축으로 왕복 이동하는 Solid (Phase 1 탑승/관성 테스트용)."""

import pygame

import settings as S
from src.entities.solid import Solid


class MovingPlatform(Solid):
    """시작 지점에서 한 축(x/y)으로 distance만큼 왕복하는 발판."""

    def __init__(self, x, y, width, height, axis, distance, speed):
        """위치·크기와 왕복 축/거리/속도를 설정 (축/거리/속도는 레벨 데이터)."""
        super().__init__(x, y, width, height)
        self.origin = (float(x), float(y))  # 왕복 기준점
        self.axis = axis                    # 'x'(수평) 또는 'y'(수직)
        self.distance = distance            # 왕복 거리(픽셀)
        self.speed = speed                  # 이동 속도(픽셀/프레임)
        self._dir = 1                       # 진행 방향(+1/-1)

    def update(self):
        """한 축으로 이동하고 범위 끝에서 방향을 뒤집으며, 실제 델타를 속도로 기록."""
        prev_x, prev_y = self.x, self.y
        if self.axis == "x":
            self.x += self.speed * self._dir
            self._bounce(self.x - self.origin[0], "x")
        else:
            self.y += self.speed * self._dir
            self._bounce(self.y - self.origin[1], "y")
        self.dx = self.x - prev_x           # 실제 이동량(탑승 캐리용)
        self.dy = self.y - prev_y
        self.vx, self.vy = self.dx, self.dy  # 관성 전달용 속도 = 실제 델타

    def _bounce(self, rel, axis):
        """왕복 범위를 벗어나면 끝에 고정하고 방향을 전환."""
        if rel >= self.distance:
            self._set_axis(axis, self.distance)
            self._dir = -1
        elif rel <= 0:
            self._set_axis(axis, 0)
            self._dir = 1

    def _set_axis(self, axis, rel):
        """기준점 기준 상대 위치로 해당 축 좌표를 고정."""
        if axis == "x":
            self.x = self.origin[0] + rel
        else:
            self.y = self.origin[1] + rel

    def draw(self, surface, offset=(0, 0)):
        """카메라 오프셋을 적용해 발판을 발판 색으로 렌더."""
        pygame.draw.rect(surface, S.COLOR_PLATFORM, self.rect.move(-offset[0], -offset[1]))
