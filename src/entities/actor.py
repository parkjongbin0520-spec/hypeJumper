"""이동·충돌 공통 로직을 가진 Actor 베이스 클래스 정의."""

from src.entities.entity import Entity


class Actor(Entity):
    """속도·축분리 충돌을 처리하는 동적 오브젝트 베이스 (Player/Enemy/NTT의 부모)."""

    def __init__(self, x, y, width, height):
        """속도 컴포넌트(입력/외적/수직)와 충돌 플래그를 초기화."""
        super().__init__(x, y, width, height)
        self.vx_input = 0.0     # 플레이어 입력에 의한 수평 속도
        self.vx_external = 0.0  # 외적 요인(대시·발판 관성) 수평 속도, 상한선 없음
        self.vy = 0.0           # 수직 속도
        self.collisions = self._empty_collisions()

    @staticmethod
    def _empty_collisions():
        """충돌 방향 플래그 딕셔너리를 초기 상태로 생성."""
        return {"up": False, "down": False, "left": False, "right": False}

    @property
    def vx(self):
        """최종 수평 속도 = 입력 속도 + 외적 속도."""
        return self.vx_input + self.vx_external

    def move(self, solids):
        """축분리 방식으로 이동 후 솔리드 충돌 해소 (수평 먼저, 수직 나중)."""
        self.collisions = self._empty_collisions()
        self.x += self.vx
        self._collide_axis(solids, horizontal=True)
        self.y += self.vy
        self._collide_axis(solids, horizontal=False)

    def _collide_axis(self, solids, horizontal):
        """한 축 이동 후 겹친 솔리드 방향으로 밀어내고 충돌 플래그를 기록."""
        rect = self.rect
        for solid in solids:
            if not rect.colliderect(solid):
                continue
            if horizontal:
                self._resolve_horizontal(rect, solid)
                self.x = rect.x
            else:
                self._resolve_vertical(rect, solid)
                self.y = rect.y

    def _resolve_horizontal(self, rect, solid):
        """수평 충돌 해소 — 이동 방향 반대편 벽면에 밀착시키고 좌/우 플래그 설정."""
        if self.vx > 0:        # 오른쪽으로 이동 중 → 솔리드 왼쪽 면에 밀착
            rect.right = solid.left
            self.collisions["right"] = True
        elif self.vx < 0:      # 왼쪽으로 이동 중 → 솔리드 오른쪽 면에 밀착
            rect.left = solid.right
            self.collisions["left"] = True

    def _resolve_vertical(self, rect, solid):
        """수직 충돌 해소 — 바닥/천장에 밀착시키고 상/하 플래그 설정."""
        if self.vy > 0:        # 하강 중 → 바닥 위에 착지
            rect.bottom = solid.top
            self.collisions["down"] = True
        elif self.vy < 0:      # 상승 중 → 천장 아래에 머리 박음
            rect.top = solid.bottom
            self.collisions["up"] = True
