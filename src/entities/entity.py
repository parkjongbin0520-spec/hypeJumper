"""모든 게임 오브젝트의 최상위 베이스 클래스 정의."""

import pygame


class Entity:
    """위치·크기를 가진 게임 오브젝트의 공통 베이스 (Actor/Solid/Trigger의 부모)."""

    def __init__(self, x, y, width, height):
        """위치(float, 서브픽셀)와 히트박스 크기를 초기화."""
        self.x = float(x)          # 수평 위치 (서브픽셀 정밀도)
        self.y = float(y)          # 수직 위치 (서브픽셀 정밀도)
        self.width = width         # 히트박스 너비
        self.height = height       # 히트박스 높이

    @property
    def rect(self):
        """현재 위치·크기로 충돌 판정용 정수 Rect 반환."""
        return pygame.Rect(int(self.x), int(self.y), self.width, self.height)

    @property
    def center(self):
        """히트박스 중심 좌표(float) 반환."""
        return (self.x + self.width / 2, self.y + self.height / 2)

    def update(self):
        """매 프레임 로직 갱신 (하위 클래스에서 구현)."""
        pass

    def draw(self, surface, offset=(0, 0)):
        """카메라 오프셋을 적용해 화면에 렌더 (하위 클래스에서 구현)."""
        pass
