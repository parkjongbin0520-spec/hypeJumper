"""카메라 — 셀레스트식 방(구역) 고정 + 빠른 슬라이드 전환. 맵을 뷰 크기 그리드로 자동 분할."""

import settings as S


class Camera:
    """뷰 크기 그리드의 '현재 방'을 고정 표시하고, 방이 바뀌면 옆 방으로 슬라이드한다."""

    def __init__(self, view_w=S.INTERNAL_W, view_h=S.INTERNAL_H):
        """뷰(=한 방) 크기를 설정하고 오프셋·방 인덱스를 초기화."""
        self.view_w = view_w
        self.view_h = view_h
        self.x = 0.0                  # 월드 좌상단 x (슬라이드용 float)
        self.y = 0.0
        self.col = None               # 현재 방 격자 인덱스 (가로)
        self.row = None               # 현재 방 격자 인덱스 (세로)
        self._target = (0.0, 0.0)     # 슬라이드 목표 좌상단
        self.sliding = False          # 방 전환 슬라이드 진행 중

    @property
    def offset(self):
        """그리기용 정수 오프셋 튜플."""
        return (round(self.x), round(self.y))

    def _clamp_origin(self, col, row, map_w, map_h):
        """격자 인덱스(col,row)의 방 좌상단을 맵 경계 안으로 클램프해 반환."""
        ox = min(max(col * self.view_w, 0), max(0, map_w - self.view_w))
        oy = min(max(row * self.view_h, 0), max(0, map_h - self.view_h))
        return float(ox), float(oy)

    def snap_to(self, rect, map_w, map_h):
        """대상이 속한 방으로 즉시 고정 (레벨 로드/리스폰 시 — 슬라이드 없음)."""
        self.col = int(rect.centerx // self.view_w)
        self.row = int(rect.centery // self.view_h)
        ox, oy = self._clamp_origin(self.col, self.row, map_w, map_h)
        self.x, self.y = ox, oy
        self._target = (ox, oy)
        self.sliding = False

    def update(self, rect, map_w, map_h):
        """현재 '보이는 방'을 완전히 벗어났을 때만 이웃 방으로 슬라이드(히스테리시스). 슬라이드 중이면 True."""
        if not self.sliding:
            dox, doy = self._clamp_origin(self.col, self.row, map_w, map_h)
            cx, cy = rect.centerx, rect.centery
            inside = (dox <= cx < dox + self.view_w) and (doy <= cy < doy + self.view_h)
            if not inside:                              # 현재 표시 영역 밖 → 이웃 방으로 전환
                self.col = int(cx // self.view_w)
                self.row = int(cy // self.view_h)
                nox, noy = self._clamp_origin(self.col, self.row, map_w, map_h)
                self._target = (nox, noy)
                self.sliding = (nox, noy) != (self.x, self.y)  # 같은 원점(가장자리 클램프)이면 생략
        if self.sliding:
            tx, ty = self._target
            self.x += (tx - self.x) * S.ROOM_SLIDE_LERP
            self.y += (ty - self.y) * S.ROOM_SLIDE_LERP
            if abs(tx - self.x) < 0.5 and abs(ty - self.y) < 0.5:  # 도달 → 스냅 종료
                self.x, self.y = tx, ty
                self.sliding = False
        return self.sliding
