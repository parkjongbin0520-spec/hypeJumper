"""타일맵 — 텍스트 그리드 맵 로더 + Phase 3 테스트 맵. 충돌 solids/위험/체크포인트 제공."""

import pygame

import settings as S
from src import assets
from src.entities.platform import MovingPlatform
from src.entities.hazard import Hazard
from src.entities.jump_pad import JumpPad
from src.entities.spring import Spring
from src.entities.ntt import NTT, RopeNTT
from src.entities.enemy import Enemy, ArmoredEnemy


def _num(s):
    """토큰을 int/float로 변환, 숫자가 아니면 문자열 그대로 (axis 'x'·dir 'up' 등)."""
    try:
        return int(s)
    except ValueError:
        try:
            return float(s)
        except ValueError:
            return s


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

    def __init__(self, text=None, map_file=None):
        """맵을 로드: map_file 주면 파일에서, 아니면 기존 하드코딩 맵."""
        self.solids = []           # 정적 충돌용 pygame.Rect
        self.hazards = []          # Hazard 객체 (닿으면 사망)
        self.checkpoint_rects = [] # 체크포인트 위치 Rect (Scene이 객체로 만듦)
        self.goal_rects = []       # 레벨 종료(Goal) 위치 Rect (Scene이 객체로 만듦)
        self.platforms = []        # 움직이는 발판
        self.jump_pads = []        # 점프패드 (JumpPad)
        self.springs = []          # 스프링 (Spring)
        self.ntts = []             # 잡기 대상 NTT (Phase 3C)
        self.enemies = []          # 적 Enemy/ArmoredEnemy (Phase 3C-2)
        self.spawn = (S.TILE_SIZE * 2, S.TILE_SIZE * 2)
        self.width = S.SCREEN_WIDTH    # 맵 픽셀 너비 (load_text가 그리드 기준으로 갱신)
        self.height = S.SCREEN_HEIGHT  # 맵 픽셀 높이 (카메라 경계 클램프용)
        if map_file is not None and self._try_load_file(map_file):
            return                                         # 파일 로드 성공 → 종료
        self.load_text(text if text is not None else TEST_MAP)
        self._build_platforms()
        self._build_objects()

    def _try_load_file(self, path):
        """파일에서 맵 로드 시도 — 실패 시 False 반환(하드코딩 폴백)."""
        try:
            with open(path, encoding="utf-8") as f:
                self.load_file(f.read())
            return True
        except (OSError, ValueError) as e:
            print(f"[TileMap] map load failed ({path}): {e} -> fallback to hardcoded map")
            self._reset_collections()      # 부분 로드분 폐기 — 폴백이 깨끗하게 재구성
            return False

    def _reset_collections(self):
        """파싱 중 채워진 수집 리스트/스폰을 초기 상태로 되돌림 (폴백용)."""
        self.solids.clear()
        self.hazards.clear()
        self.checkpoint_rects.clear()
        self.goal_rects.clear()
        self.platforms.clear()
        self.jump_pads.clear()
        self.springs.clear()
        self.ntts.clear()
        self.enemies.clear()
        self.spawn = (S.TILE_SIZE * 2, S.TILE_SIZE * 2)
        self.width = S.SCREEN_WIDTH
        self.height = S.SCREEN_HEIGHT

    def load_file(self, text):
        """2섹션 텍스트([MAP]/[OBJECTS])를 파싱 — 그리드는 load_text, 객체는 _parse_objects."""
        map_lines, obj_lines, target = [], [], None
        for line in text.splitlines():
            head = line.strip()
            if head == "[MAP]":
                target = map_lines
            elif head == "[OBJECTS]":
                target = obj_lines
            elif target is not None:
                target.append(line)
        self.load_text("\n".join(map_lines))
        self._parse_objects(obj_lines)

    def _parse_objects(self, lines):
        """[OBJECTS] 줄들을 타입별 엔티티로 생성 (주석 '#'/빈 줄 무시)."""
        for line in lines:
            tok = line.split()
            if not tok or tok[0].startswith("#"):
                continue
            self._spawn_object(tok[0], [_num(v) for v in tok[1:]])

    def _spawn_object(self, kind, a):
        """타입 토큰 + 숫자/문자 인자 리스트로 해당 엔티티를 리스트에 추가."""
        if kind == "platform":     # x y w h axis dist speed
            self.platforms.append(MovingPlatform(a[0], a[1], a[2], a[3], a[4], a[5], a[6]))
        elif kind == "spring":     # x y w h dir
            self.springs.append(Spring(a[0], a[1], a[2], a[3], a[4]))
        elif kind == "ntt":
            self.ntts.append(NTT(a[0], a[1]))
        elif kind == "ropentt":    # 천장 피벗 좌표
            self.ntts.append(RopeNTT(a[0], a[1]))
        elif kind == "enemy":
            self.enemies.append(Enemy(a[0], a[1]))
        elif kind == "armored":
            self.enemies.append(ArmoredEnemy(a[0], a[1]))
        else:
            raise ValueError(f"알 수 없는 객체 타입: {kind}")

    def load_text(self, text):
        """글자 그리드를 파싱해 solids/hazards/checkpoint/goal/spawn을 채우고 맵 크기를 계산."""
        t = S.TILE_SIZE
        lines = text.splitlines()
        if lines:                                          # 맵 픽셀 크기 = 그리드 폭/높이 (카메라 클램프용)
            self.width = max(len(line) for line in lines) * t
            self.height = len(lines) * t
        for r, line in enumerate(lines):
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
                elif ch == 'G':
                    self.goal_rects.append(pygame.Rect(x, y, t, t))
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
        """카메라 오프셋을 적용해 solid(타일)·가시·발판을 렌더."""
        for r in self.solids:
            assets.tile_fill(surface, "tile_ground", r, S.COLOR_SOLID, offset)
        for hz in self.hazards:
            hz.draw(surface, offset)
        for plat in self.platforms:
            plat.draw(surface, offset)
        for jp in self.jump_pads:
            jp.draw(surface, offset)
        for sp in self.springs:
            sp.draw(surface, offset)
