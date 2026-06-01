"""카메라 — 플레이어를 부드럽게 추적하고 맵 경계 안으로 클램프하는 뷰포트 오프셋 관리."""

import settings as S


class Camera:
    """월드 좌상단 오프셋을 보유하고 lerp 추적 + 경계 클램프를 수행 (그리기 시 offset 감산)."""

    def __init__(self, view_w=S.SCREEN_WIDTH, view_h=S.SCREEN_HEIGHT):
        """뷰포트 크기를 설정하고 오프셋을 원점으로 초기화."""
        self.view_w = view_w          # 화면(뷰포트) 너비
        self.view_h = view_h          # 화면(뷰포트) 높이
        self.x = 0.0                  # 월드 좌상단 x (서브픽셀, lerp용 float)
        self.y = 0.0                  # 월드 좌상단 y

    @property
    def offset(self):
        """그리기에 쓸 정수 오프셋 튜플 (rect.move(-offset) 형태로 감산)."""
        return (round(self.x), round(self.y))

    def _target_topleft(self, target_rect):
        """대상을 뷰포트 중앙에 두는 좌상단 좌표를 반환."""
        return (target_rect.centerx - self.view_w / 2,
                target_rect.centery - self.view_h / 2)

    def _clamp(self, x, y, map_w, map_h):
        """오프셋을 맵 경계 [0, map-view] 안으로 가둠 (맵이 화면보다 작으면 0 고정)."""
        max_x = max(0, map_w - self.view_w)
        max_y = max(0, map_h - self.view_h)
        return (min(max(x, 0), max_x), min(max(y, 0), max_y))

    def update(self, target_rect, map_w, map_h):
        """대상 중심으로 lerp 추적 후 맵 경계로 클램프 (매 프레임 호출)."""
        tx, ty = self._target_topleft(target_rect)
        self.x += (tx - self.x) * S.CAMERA_LERP
        self.y += (ty - self.y) * S.CAMERA_LERP
        self.x, self.y = self._clamp(self.x, self.y, map_w, map_h)

    def snap(self, target_rect, map_w, map_h):
        """대상에 즉시 중앙 정렬 (레벨 로드/리스폰 시 — 긴 패닝 방지)."""
        tx, ty = self._target_topleft(target_rect)
        self.x, self.y = self._clamp(tx, ty, map_w, map_h)
