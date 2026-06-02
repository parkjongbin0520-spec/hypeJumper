"""게임 전역 상수/설정값 모음 — 매직 넘버 금지, 튜닝 값은 전부 여기서 관리."""

# ══════════════════════════════════════════════
#  디스플레이 / 루프  [추가] (CLAUDE.md 스펙 외, 실행에 필요)
# ══════════════════════════════════════════════
SCREEN_WIDTH = 960          # 창 너비 (픽셀)
SCREEN_HEIGHT = 540         # 창 높이 (픽셀)
FPS = 60                    # 목표 프레임레이트
TILE_SIZE = 16              # 타일 한 칸 크기 (픽셀)
RENDER_SCALE = 1            # 렌더 배율 (도트 확대용)
TITLE = "hypeJumper"        # 창 제목

# ── 팔레트  [추가] (design.md "Organic Eco-Hologram" — Bright & Peaceful Bio-Cyberpunk) ──
#   Main 70%: 비취/민트 (생명+기술) / Sub 20%: 유백·안개그레이 (가독성) / Point 10%: 황금 (전통·아늑)
JADE = (78, 206, 170)           # 맑은 비취색 (주색)
JADE_DEEP = (40, 130, 120)      # 짙은 비취 (그림자/지형)
MINT = (158, 240, 212)          # 청량 민트 녹색
MILKY = (232, 246, 240)         # 유백색 (밝은 대비)
MIST_GRAY = (120, 150, 152)     # 안개빛 그레이
GOLD = (255, 208, 120)          # 따뜻한 반딧불 황금색 (포인트)
CORAL = (236, 96, 92)           # 경고 코랄레드 (위험/긴장)
MAGENTA = (212, 92, 150)        # 자홍 (강화 적 대비)
VIOLET = (176, 158, 232)        # 부드러운 보라 (NTT)

# 배경 — 비취빛 야간 그라데이션 (밤이지만 어둡지 않게) + 정적 반딧불
COLOR_BG = (14, 30, 36)         # 배경 기본색 (그라데이션 폴백)
COLOR_BG_TOP = (12, 26, 34)     # [추가] 그라데이션 상단 (깊은 비취-네이비)
COLOR_BG_BOTTOM = (22, 48, 52)  # [추가] 그라데이션 하단 (은은한 비취 발광)
COLOR_FIREFLY = GOLD            # [추가] 반딧불 점 색 (생체발광 포인트)
FIREFLY_COUNT = 36              # [추가] 반딧불 개수 (정적 배치)

# 렌더 색상 — 팔레트 기반 재매핑 (가독성: 플레이어/적/가시는 배경과 강한 대비)
COLOR_PLAYER = MILKY            # 플레이어 (유백 — 최대 가독성)
COLOR_SOLID = (58, 84, 86)      # 바닥/벽/천장 (안개+비취 틴트, 어둡게 해 캐릭터 부각)
COLOR_PLATFORM = MINT           # 움직이는 발판 (유백 비취/민트 발광)
COLOR_HAZARD = CORAL            # [Phase 3] 가시/위험 (경고 코랄레드)
COLOR_CHECKPOINT = JADE_DEEP         # [Phase 3] 체크포인트(비활성, 짙은 비취)
COLOR_CHECKPOINT_ON = GOLD             # [Phase 3] 체크포인트(활성, 황금)
COLOR_JUMP_PAD = (110, 220, 228)       # [Phase 3] 점프패드 (밝은 청록)
COLOR_SPRING = MAGENTA                 # [Phase 3] 스프링(방향 발사)
COLOR_NTT = VIOLET                     # [Phase 3] 잡기 대상 NTT
COLOR_GRAB_OK = (108, 240, 168)        # [Phase 3] 잡기 가능(장애물 없음, 민트그린)
COLOR_GRAB_NO = CORAL                  # [Phase 3] 잡기 불가(장애물/범위밖, 코랄)
COLOR_ENEMY = (236, 128, 92)           # [Phase 3] 일반 적 (HP1, 주황코랄)
COLOR_ENEMY_ARMORED = MAGENTA          # [Phase 3] 강화 적 (HP2+)
COLOR_ENEMY_HIT = MIST_GRAY            # [Phase 3] 피격 무적 중(투명 느낌) 색
COLOR_ROPE_NTT = (118, 212, 220)       # [Phase 3] 줄 NTT (진자, 청록)
COLOR_ROPE_LINE = (84, 110, 112)       # [Phase 3] 줄 NTT 줄(피벗~NTT 선)

# ── 플레이어 히트박스 크기  [추가] (높이만 스펙에 있음, 너비 보강) ──
PLAYER_WIDTH = 8                # 플레이어 히트박스 너비

# ══════════════════════════════════════════════
#  Phase 1: 이동
# ══════════════════════════════════════════════
PLAYER_MAX_SPEED = 4           # 최대 수평 속도 (맵 크기 대비 하향 튜닝)
NORMAL_HITBOX_HEIGHT = 16      # 일반 히트박스 높이
DUCK_HITBOX_HEIGHT = 8         # 웅크리기 시 히트박스 높이
GROUND_ACCEL = 1.2             # 지상 가속도
GROUND_DECEL = 1.2             # 지상 감속도
AIR_MULT = 0.65                # 공중 제어 배율
AIR_FRICTION = 0.85            # 공중 외적 속도 감쇠
SPEED_REDUCE = 0.8             # 초과 속도 감쇠
PLATFORM_INERTIA_X = 1.0       # 수평 발판 관성 전달 비율
PLATFORM_INERTIA_Y = 1.0       # 수직 발판 관성 전달 비율
WALL_SLIDE_GRAVITY = 0.3       # 월 슬라이드 중력 감소 계수
WALL_SLIDE_MAX_FALL = 3        # [추가] 월 슬라이드 종단 낙하 속도 (느린 하강 cap)

# ══════════════════════════════════════════════
#  Phase 1: 점프
# ══════════════════════════════════════════════
JUMP_SPEED = -6.5              # [추가] 점프 시작 수직 속도 (정점 ≈3.1칸/50px)
GRAVITY_UP = 0.4               # 점프 버튼 누르는 동안 상승 중력
GRAVITY_UP_RELEASE = 1.2       # 점프 버튼 뗀 후 상승 중력
GRAVITY_DOWN = 0.8             # 하강 중력
HALF_GRAV_THRESHOLD = 2.0      # 정점 체공 속도 기준
MAX_FALL_SPEED = 12            # 최대 낙하 속도
FAST_FALL_GRAVITY = 1.6        # 패스트 폴 중력
FAST_MAX_FALL = 20             # 패스트 폴 최대 낙하 속도

# ── 월 점프  [추가] (기본 월점프 속도, 스펙엔 동작만 명시) ──
WALL_JUMP_H = 6               # 월 점프 수평 반발 속도 (벽 반대 방향, 1.5배 ↑)
WALL_JUMP_V = -6.5           # 월 점프 수직 속도 (위 방향, 점프와 동일 높이로 복구)
# 월 점프 후 '벽 쪽' 입력은 정점(vy>=0)까지 무시 → 확실히 떼짐, 정점 후 재부착 가능(클라임)

# ══════════════════════════════════════════════
#  Phase 1: 버퍼 / 코요테
# ══════════════════════════════════════════════
COYOTE_TIME = 6                # 지상 코요테 타임 프레임
WALL_COYOTE_TIME = 4           # 벽 코요테 타임 프레임 (타이트하게 하향)
JUMP_BUFFER = 6                # 점프 입력 버퍼 프레임
WALL_JUMP_BUFFER = 4           # 월 점프 입력 버퍼 프레임 (타이트하게 하향)

# ══════════════════════════════════════════════
#  Phase 1: 충돌
# ══════════════════════════════════════════════
CEILING_STICK_DIVISOR = 2      # 천장 밀착 프레임 계산 제수
MAX_CEILING_STICK_FRAMES = 10  # 천장 밀착 프레임 상한선
NEAR_GROUND_DISTANCE = 6       # [추가] 이 거리 안에 바닥 있으면 점프=일반점프 (지상 근처 월점프 방지)

# ══════════════════════════════════════════════
#  Phase 2: 대시
# ══════════════════════════════════════════════
DASH_SPEED = 8.0               # 대시 속도
END_DASH_SPEED = 5.0           # 대시 종료 후 수평 속도
END_DASH_UP_MULT = 0.75        # 위쪽 대시 종료 수직 배율
DASH_TIME = 9                  # 대시 지속 프레임 (0.15s @60fps)
MAX_DASHES = 1                 # [추가] 대시 충전 최대치 (대시 횟수 1회 제한)

# ══════════════════════════════════════════════
#  Phase 2.5: 고급 무브먼트
# ══════════════════════════════════════════════
AUTO_JUMP_BUFFER = 9           # 슈퍼/하이퍼/월바운스 점프 허용 프레임 (0.15s @60fps — 대시 후 창 완화)
SUPER_JUMP_H = 9.5             # 슈퍼 대시 수평 속도 (순간 힘 상향)
DUCK_SUPER_JUMP_X_MULT = 1.25  # 하이퍼 대시 수평 배율
DUCK_SUPER_JUMP_Y_MULT = 1.0   # 하이퍼 대시 수직 배율 (일반 점프 높이와 동일)
SUPER_HANG_TIME = 18           # [추가] 슈퍼 후 체공(부유) 프레임 (월점프와 구분되는 손맛)
SUPER_HANG_GRAV = 0.4          # [추가] 슈퍼 체공 중 중력 배율 (하강 부유 — 높이는 일반점프 유지)
HYPER_HANG_TIME = 22           # [추가] 하이퍼 후 체공(부유) 프레임 (낮은 아크로 멀리)
HYPER_HANG_GRAV = 0.4          # [추가] 하이퍼 체공 중 중력 배율 (더 부유)
SUPER_WALL_JUMP_H = 9.0        # 월바운스 수평 속도
SUPER_WALL_JUMP_SPEED = -7.0   # 기본 월바운스 수직 속도 (월점프 -6.5보다 약간 높게, 과한 높이 하향)
SUPER_WALL_JUMP_FORCE_TIME = 12  # 월바운스 강제 이동 프레임
WALL_BOUNCE_DIAG_Y = 3.0       # 대각선 월바운스 수직 속도
WALL_BOUNCE_RANGE = 16         # [추가] 월바운스 벽 감지 버퍼 (이 거리 안에 벽 있으면 발동)
WALL_BOUNCE_BUFFER = 9         # [추가] 월바운스 입력 버퍼 (수직대시 점프 후 N프레임 내 벽 닿으면 자동 발동)
WALL_BOUNCE_HANG_TIME = 20     # [추가] 월바운스 후 체공(부유) 프레임 (체공 하향)
WALL_BOUNCE_HANG_GRAV = 0.6    # [추가] 체공 중 중력 배율 (부유감 — 정점 체류 연장)

# ══════════════════════════════════════════════
#  Phase 3: 잡기
# ══════════════════════════════════════════════
GRAB_RANGE = 110               # 잡기 탐색 범위 (픽셀)
DASH_STRIKE_RANGE = 120        # 대시 중 잡기 탐색 범위
DASH_STRIKE_BUFFER = 8         # 대시 전후 Z키 버퍼 프레임
MAX_GRAB_TIME = 150            # 최대 잡기 유지 프레임 (2.5초)
GRAB_READY_TIMEOUT = 180       # GRAB_READY 유지 최대 프레임
GRAB_AIM_TIME = 12             # [추가] Z 누름 시 조준 윈도우 프레임 (만료 시 자동 취소, 홀드 무관)
GRAB_SLOW_FACTOR = 3           # [추가] 조준 윈도우 동안 슬로우 배율 (N프레임마다 1번 갱신=1/N 속도)
NTT_WIDTH = 14                 # [추가] 잡기 대상 NTT 너비
NTT_HEIGHT = 14                # [추가] 잡기 대상 NTT 높이
ENEMY_WIDTH = 16               # [추가] 적 히트박스 너비
ENEMY_HEIGHT = 16              # [추가] 적 히트박스 높이
ARMORED_ENEMY_HP = 2           # [추가] 강화 적 기본 HP

# ══════════════════════════════════════════════
#  Phase 3: 적 / NTT
# ══════════════════════════════════════════════
INVINCIBLE_TIME = 60           # 피격 후 무적 프레임 (1초)
RESPAWN_TIME = 180             # 적 파괴 후 재생성 프레임 (3초)
ROPE_LENGTH = 80               # 줄 NTT 줄 길이 (픽셀)
ROPE_GRAVITY = 0.3             # 줄 NTT 진자 중력 계수
ROPE_START_ANGLE = 0.5         # [추가] 줄 NTT 초기 각도(라디안) — 상시 진자 진폭
PUSH_SPEED = 6.0               # 릴리즈 시 밀쳐지는 속도
PUSH_RETURN_TIME = 120         # 밀쳐진 후 원위치 복귀 프레임 (2초)
PUSH_RETURN_SPEED = 1.5        # 원위치 복귀 속도
ENEMY_PATROL_RADIUS = 80       # 적 활동 반경 (픽셀, 생성 위치 기준)
ENEMY_DETECT_RANGE = 120       # 플레이어 감지 범위 (픽셀)
PROJECTILE_SPEED = 5.0         # 투사체 속도
PROJECTILE_LIFETIME = 180      # 투사체 최대 생존 프레임 (3초)

# ══════════════════════════════════════════════
#  Phase 3: 오브젝트
# ══════════════════════════════════════════════
JUMP_PAD_SPEED = -15.0         # 점프패드 수직 속도
SPRING_SPEED = 10.0            # 스프링 발사 속도 (수평 성분)
SPRING_LAUNCH_V = -12.5        # [추가] 위 스프링 수직 발사 속도 (점프보다 높게)
SPRING_WALL_V = -14.5          # [추가] 벽 스프링 전용 수직 발사 속도 (더 높이 튕김, 위 스프링과 분리)
SPRING_FORCE_TIME = 5          # [추가] 스프링 발사 후 입력 잠금 프레임 (짧게 — 발사 직후 수평 제어 빨리 복구)
JUMP_PAD_COOLDOWN = 12         # [추가] 점프패드 재발동 방지 프레임 (겹침 중첩 폭점프 방지)
SPRING_COOLDOWN = 12           # [추가] 스프링 재발동 방지 프레임
PLATFORM_RETURN_SPEED = 1.0    # 발판 원위치 복귀 속도 (서서히)

# ══════════════════════════════════════════════
#  Phase 4: 카메라 / 레벨  [추가] (화면보다 큰 맵 스크롤 + 레벨 전환)
# ══════════════════════════════════════════════
CAMERA_LERP = 0.12             # (구) 추적 보간 — 현재 미사용(방 고정 카메라로 대체)
CAMERA_ZOOM = 2                # 화면 확대 배율 (내부 저해상 렌더 → 화면으로 약 N배 확대)
# 방(=내부 렌더) 크기를 타일 정수배로 정의 → 방 격자선이 항상 타일 경계에 떨어짐.
# 540/2=270은 16타일에 안 나눠떨어짐(16.875) → 세로를 17타일=272로 잡아 정렬.
# 내부 surface(480×272)는 main에서 화면 960×540으로 scale → 가로 ×2.0 / 세로 ×1.985(0.74% 차, 무시).
ROOM_W_TILES = 30              # 한 방 가로 타일 수 (30×16=480)
ROOM_H_TILES = 17             # 한 방 세로 타일 수 (17×16=272, 화면 절반 270에 가장 근접한 타일 정수)
INTERNAL_W = ROOM_W_TILES * TILE_SIZE   # 내부 렌더(=한 방) 너비 (480 = 30타일)
INTERNAL_H = ROOM_H_TILES * TILE_SIZE   # 내부 렌더(=한 방) 높이 (272 = 17타일)
ROOM_SLIDE_LERP = 0.18         # 방 전환 슬라이드 보간 계수 (빠른 슬라이드)
COLOR_GOAL = GOLD              # 레벨 종료(Goal) 트리거 색 (황금 — 도착 지점)
# 패럴럭스 배경 — 각 레이어가 카메라 오프셋의 일부만큼만 스크롤(멀수록 적게 = 깊이감)
PARALLAX_SKY = 0.10            # bg_sky 스크롤 비율 (가장 먼 하늘, 거의 고정)
PARALLAX_FAR = 0.30           # bg_bamboo_far (먼 대나무)
PARALLAX_NEAR = 0.60          # bg_bamboo_near (가까운 대나무, 가장 빠름)
ANIM_FRAME_DUR = 6             # 애니메이션 프레임당 게임프레임 수 (6=10fps 애니 @60fps)
LEVEL_FILES = [                # 레벨 시퀀스 — 순서대로 클리어 시 다음으로 전환 (스테이지 1~5)
    "assets/tilemaps/level1.txt",
    "assets/tilemaps/level2.txt",
    "assets/tilemaps/level3.txt",
    "assets/tilemaps/level4.txt",
    "assets/tilemaps/level5.txt",
]

# ══════════════════════════════════════════════
#  메뉴 / 타이틀 / 일시정지  [추가] (게임 상태 화면)
# ══════════════════════════════════════════════
MENU_OVERLAY_ALPHA = 170          # 일시정지 시 게임 화면 위 어둡게 덮는 반투명 알파 (0~255)
MENU_OVERLAY_COLOR = (8, 18, 22)  # 오버레이 색 (배경 톤과 맞춘 짙은 비취-네이비)
COLOR_MENU_TITLE = GOLD           # 타이틀/메뉴 제목 글자색 (포인트 황금)
COLOR_MENU_TEXT = MILKY           # 메뉴 항목 기본 글자색 (유백)
COLOR_MENU_SELECTED = JADE        # 선택된 메뉴 항목 글자색 (비취 강조)
COLOR_MENU_HINT = MIST_GRAY       # 하단 조작 안내 글자색 (안개 그레이)
TITLE_BIG_SIZE = 64               # 타이틀 큰 글자 폰트 크기
MENU_ITEM_SIZE = 28               # 메뉴 항목 폰트 크기
MENU_HINT_SIZE = 16               # 조작 안내 폰트 크기
TITLE_TEXT = "HYPE JUMPER"        # 타이틀 화면 대문 문구
TITLE_HINT = "아무 키나 눌러 시작"   # 타이틀 화면 시작 안내
PAUSE_TITLE = "일시정지"           # 일시정지 메뉴 제목
PAUSE_ITEMS = ("계속 진행", "끝내기")  # 일시정지 메뉴 항목 (위→아래 순서)
MENU_ITEM_GAP = 44                # 메뉴 항목 간 세로 간격 (픽셀)
