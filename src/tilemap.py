"""타일맵 — 텍스트 그리드 맵 로더 + Phase 3 테스트 맵. 충돌 solids/위험/체크포인트 제공."""

import pygame

import settings as S
from src.entities.platform import MovingPlatform
from src.entities.hazard import Hazard
from src.entities.jump_pad import JumpPad
from src.entities.spring import Spring
from src.entities.ntt import NTT, RopeNTT
from src.entities.enemy import Enemy, ArmoredEnemy


# 텍스트 맵 기호 — 한 글자 = TILE_SIZE 칸
#   '#' 막힘(solid) / '^' 가시(Hazard) / 'C' 체크포인트 / 'P' 스폰 / 그 외 빈칸
def _build_test_text():
    """3A 테스트 맵을 글자 그리드로 구성 (헬퍼로 안전하게, 파싱은 load_text)."""
    cols = S.SCREEN_WIDTH // S.TILE_SIZE      # 60
    rows = S.SCREEN_HEIGHT // S.TILE_SIZE     # 33
    g = [[' '] * cols for _ in range(rows)]

    def box():
        """바깥 경계 벽."""
        for c in range(cols):
            g[0][c] = g[rows - 1][c] = '#'
        for r in range(rows):
            g[r][0] = g[r][cols - 1] = '#'

    def hline(r, c0, c1, ch='#'):
        """행 r의 c0~c1을 ch로 채움."""
        for c in range(c0, c1 + 1):
            g[r][c] = ch

    box()
    # ── 바닥 (아래 두 줄) ──
    hline(rows - 2, 1, cols - 2, '#')
    hline(rows - 3, 1, cols - 2, '#')
    # ── 낙사 구덩이 (바닥/경계 일부 제거) cols 40~45 ──
    for r in (rows - 3, rows - 2, rows - 1):
        for c in range(40, 46):
            g[r][c] = ' '
    # ── 가시: 바닥 위(rows-4)에 cols 20~25 ──
    hline(rows - 4, 20, 25, '^')
    # ── 좌측 낮은 발판(점프 착지) + 그 위 가시 함정 ──
    hline(rows - 9, 8, 14, '#')
    hline(rows - 10, 11, 12, '^')          # 발판 위 가시
    # ── 중앙 체크포인트 선반 + 체크포인트(2칸 높이) ──
    hline(rows - 8, 30, 36, '#')
    g[rows - 9][33] = 'C'
    g[rows - 10][33] = 'C'
    # ── 우측 상단 선반 (구덩이 건너편 도달용) ──
    hline(rows - 7, 48, 56, '#')
    # ── 점프패드 (스폰 근처 바닥 위) ──
    g[rows - 4][9] = 'J'
    # ── 스폰 (좌측 바닥 위) ──
    g[rows - 4][3] = 'P'
    return '\n'.join(''.join(row) for row in g)


TEST_MAP = _build_test_text()


class TileMap:
    """텍스트 그리드에서 로드한 정적 지오메트리/위험/체크포인트 + 코드 배치 발판."""

    def __init__(self, text=None):
        """텍스트 맵을 파싱하고 움직이는 발판을 배치."""
        self.solids = []           # 정적 충돌용 pygame.Rect
        self.hazards = []          # Hazard 객체 (닿으면 사망)
        self.checkpoint_rects = [] # 체크포인트 위치 Rect (Scene이 객체로 만듦)
        self.platforms = []        # 움직이는 발판
        self.jump_pads = []        # 점프패드 (JumpPad)
        self.springs = []          # 스프링 (Spring)
        self.ntts = []             # 잡기 대상 NTT (Phase 3C)
        self.enemies = []          # 적 Enemy/ArmoredEnemy (Phase 3C-2)
        self.spawn = (S.TILE_SIZE * 2, S.TILE_SIZE * 2)
        self.load_text(text if text is not None else TEST_MAP)
        self._build_platforms()
        self._build_objects()

    def load_text(self, text):
        """글자 그리드를 파싱해 solids/hazards/checkpoint/spawn을 채움 (제너릭 로더)."""
        t = S.TILE_SIZE
        merge = []  # 같은 행 연속 '#'을 하나의 Rect로 합칠 버퍼
        for r, line in enumerate(text.splitlines()):
            run_start = None
            for c, ch in enumerate(line):
                x, y = c * t, r * t
                if ch == '#':
                    run_start = c if run_start is None else run_start
                    continue
                if run_start is not None:                  # 연속 '#' 종료 → 한 Rect로
                    self.solids.append(pygame.Rect(run_start * t, y, (c - run_start) * t, t))
                    run_start = None
                if ch == '^':
                    self.hazards.append(Hazard(x, y, t, t))
                elif ch == 'C':
                    self.checkpoint_rects.append(pygame.Rect(x, y, t, t))
                elif ch == 'J':
                    self.jump_pads.append(JumpPad(x, y, t, t))
                elif ch == 'P':
                    self.spawn = (x, y)
            if run_start is not None:                      # 행 끝까지 '#'
                self.solids.append(pygame.Rect(run_start * t, r * t, (len(line) - run_start) * t, t))

    def _build_platforms(self):
        """움직이는 발판 배치 (탑승/관성/끼임 테스트 유지)."""
        # 좌우 발판 — 가시 구역 위를 왕복
        self.platforms.append(MovingPlatform(320, 360, 90, 14, "x", 160, 1.4))
        # 상하 발판 — 우측 선반 근처 상하 왕복
        self.platforms.append(MovingPlatform(800, 300, 64, 14, "y", 150, 1.2))

    def _build_objects(self):
        """스프링 코드 배치 — 위 발사(우측 선반 위) + 오른쪽 발사(좌측 벽 부착)."""
        t = S.TILE_SIZE
        rows = S.SCREEN_HEIGHT // t
        # 우측 선반(rows-7, 48~56) 위 — 위 발사 스프링
        self.springs.append(Spring(50 * t, (rows - 8) * t, t, t, "up"))
        # 좌측 경계벽(col0) 바로 우측 부착 — 오른쪽 발사 스프링
        self.springs.append(Spring(t, (rows - 12) * t, 6, 2 * t, "right"))
        # 잡기 대상 NTT — 공중(점프해서 범위 안) + 바닥 위에 올라탄 것(바닥 케이스 검증)
        self.ntts.append(NTT(16 * t, (rows - 7) * t))                 # 공중
        floor_top = (rows - 3) * t                                   # 바닥 윗면
        self.ntts.append(NTT(13 * t, floor_top - S.NTT_HEIGHT))      # 바닥 위 올라탐
        # 적 — 일반(HP1) + 강화(HP2), 가시(20~25)·구덩이(40~45) 피해 바닥 위 배치
        self.enemies.append(Enemy(27 * t, floor_top - S.ENEMY_HEIGHT))
        self.enemies.append(ArmoredEnemy(34 * t, floor_top - S.ENEMY_HEIGHT))
        # 줄 NTT(샹들리에) — 중앙 선반 위에 매달려 상시 진자 (선반에서 잡기 테스트)
        self.ntts.append(RopeNTT(520, 320))

    def update(self):
        """모든 움직이는 발판을 갱신."""
        for plat in self.platforms:
            plat.update()

    def solid_rects(self):
        """정적 solid + 발판의 현재 rect를 합쳐 충돌용 리스트로 반환."""
        return self.solids + [p.rect for p in self.platforms]

    def draw(self, surface, offset=(0, 0)):
        """카메라 오프셋을 적용해 solid·가시·발판을 렌더."""
        for r in self.solids:
            pygame.draw.rect(surface, S.COLOR_SOLID, r.move(-offset[0], -offset[1]))
        for hz in self.hazards:
            hz.draw(surface, offset)
        for plat in self.platforms:
            plat.draw(surface, offset)
        for jp in self.jump_pads:
            jp.draw(surface, offset)
        for sp in self.springs:
            sp.draw(surface, offset)
