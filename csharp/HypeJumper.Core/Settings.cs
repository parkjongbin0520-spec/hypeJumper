namespace HypeJumper.Core;

/// <summary>게임 전역 상수 — settings.py 1:1 이식(색상 제외, 색상은 Platform/Palette).</summary>
/// <remarks>
/// 파서티(parity) 원칙: 물리에 관여하는 값은 double(파이썬 float=double)로 둔다.
/// 순수 프레임 카운트/픽셀 크기/타일 수는 int. 이름은 감사 편의를 위해 settings.py와 동일하게 유지.
/// </remarks>
public static class Settings
{
    // ── 디스플레이 / 루프 ─────────────────────────────────────────
    public const int SCREEN_WIDTH = 960;       // 창 너비(픽셀)
    public const int SCREEN_HEIGHT = 540;      // 창 높이(픽셀)
    public const int FPS = 60;                 // 목표 프레임레이트
    public const int TILE_SIZE = 16;           // 타일 한 칸 크기(픽셀)
    public const int RENDER_SCALE = 1;         // 렌더 배율
    public const string TITLE = "hypeJumper";  // 창 제목
    public const int FIREFLY_COUNT = 36;       // 반딧불 개수(정적 배치, 렌더)
    public const int PLAYER_WIDTH = 8;         // 플레이어 히트박스 너비

    // ── Phase 1: 이동 ────────────────────────────────────────────
    public const double PLAYER_MAX_SPEED = 4;       // 최대 수평 속도
    public const int NORMAL_HITBOX_HEIGHT = 16;     // 일반 히트박스 높이
    public const int DUCK_HITBOX_HEIGHT = 8;        // 웅크리기 히트박스 높이
    public const double GROUND_ACCEL = 1.2;         // 지상 가속도
    public const double GROUND_DECEL = 1.2;         // 지상 감속도
    public const double AIR_MULT = 0.65;            // 공중 제어 배율
    public const double AIR_FRICTION = 0.85;        // 공중 외적 속도 감쇠
    public const double SPEED_REDUCE = 0.8;         // 초과 속도 감쇠
    public const double PLATFORM_INERTIA_X = 1.0;   // 수평 발판 관성 전달 비율
    public const double PLATFORM_INERTIA_Y = 1.0;   // 수직 발판 관성 전달 비율
    public const double WALL_SLIDE_GRAVITY = 0.3;   // 월 슬라이드 중력 감소 계수
    public const double WALL_SLIDE_MAX_FALL = 3;    // 월 슬라이드 종단 낙하 속도

    // ── Phase 1: 점프 ────────────────────────────────────────────
    public const double JUMP_SPEED = -6.5;          // 점프 시작 수직 속도
    public const double GRAVITY_UP = 0.4;           // 점프 버튼 유지 중 상승 중력
    public const double GRAVITY_UP_RELEASE = 1.2;   // 점프 버튼 뗀 후 상승 중력
    public const double GRAVITY_DOWN = 0.8;         // 하강 중력
    public const double HALF_GRAV_THRESHOLD = 2.0;  // 정점 체공 속도 기준
    public const double MAX_FALL_SPEED = 12;        // 최대 낙하 속도
    public const double FAST_FALL_GRAVITY = 1.6;    // 패스트 폴 중력
    public const double FAST_MAX_FALL = 20;         // 패스트 폴 최대 낙하 속도
    public const double WALL_JUMP_H = 6;            // 월 점프 수평 반발 속도
    public const double WALL_JUMP_V = -6.5;         // 월 점프 수직 속도

    // ── Phase 1: 버퍼 / 코요테 ───────────────────────────────────
    public const int COYOTE_TIME = 6;          // 지상 코요테 타임
    public const int WALL_COYOTE_TIME = 4;     // 벽 코요테 타임
    public const int JUMP_BUFFER = 6;          // 점프 입력 버퍼
    public const int WALL_JUMP_BUFFER = 4;     // 월 점프 입력 버퍼

    // ── Phase 1: 충돌 ────────────────────────────────────────────
    public const double CEILING_STICK_DIVISOR = 2;   // 천장 밀착 프레임 계산 제수
    public const int MAX_CEILING_STICK_FRAMES = 10;  // 천장 밀착 프레임 상한
    public const int NEAR_GROUND_DISTANCE = 6;       // 이 거리 안 바닥이면 일반 점프

    // ── Phase 2: 대시 ────────────────────────────────────────────
    public const double DASH_SPEED = 8.0;          // 대시 속도
    public const double END_DASH_SPEED = 5.0;      // 대시 종료 후 수평 속도
    public const double END_DASH_UP_MULT = 0.75;   // 위쪽 대시 종료 수직 배율
    public const int DASH_TIME = 9;                // 대시 지속 프레임
    public const int MAX_DASHES = 1;               // 대시 충전 최대치

    // ── Phase 2.5: 고급 무브먼트 ─────────────────────────────────
    public const int AUTO_JUMP_BUFFER = 9;             // 슈퍼/하이퍼/월바운스 점프 허용 프레임
    public const double SUPER_JUMP_H = 9.5;            // 슈퍼 대시 수평 속도
    public const double DUCK_SUPER_JUMP_X_MULT = 1.25; // 하이퍼 대시 수평 배율
    public const double DUCK_SUPER_JUMP_Y_MULT = 1.0;  // 하이퍼 대시 수직 배율
    public const int SUPER_HANG_TIME = 18;             // 슈퍼 후 체공 프레임
    public const double SUPER_HANG_GRAV = 0.4;         // 슈퍼 체공 중력 배율
    public const int HYPER_HANG_TIME = 22;             // 하이퍼 후 체공 프레임
    public const double HYPER_HANG_GRAV = 0.4;         // 하이퍼 체공 중력 배율
    public const double SUPER_WALL_JUMP_H = 9.0;       // 월바운스 수평 속도
    public const double SUPER_WALL_JUMP_SPEED = -7.0;  // 기본 월바운스 수직 속도
    public const int SUPER_WALL_JUMP_FORCE_TIME = 12;  // 월바운스 강제 이동 프레임
    public const double WALL_BOUNCE_DIAG_Y = 3.0;      // 대각선 월바운스 수직 속도
    public const int WALL_BOUNCE_RANGE = 16;           // 월바운스 벽 감지 버퍼
    public const int WALL_BOUNCE_BUFFER = 9;           // 월바운스 입력 버퍼
    public const int WALL_BOUNCE_HANG_TIME = 20;       // 월바운스 후 체공 프레임
    public const double WALL_BOUNCE_HANG_GRAV = 0.6;   // 월바운스 체공 중력 배율

    // ── Phase 3: 잡기 ────────────────────────────────────────────
    public const int GRAB_RANGE = 110;          // 잡기 탐색 범위(픽셀)
    public const int DASH_STRIKE_RANGE = 120;   // 대시 중 잡기 탐색 범위
    public const int DASH_STRIKE_BUFFER = 8;    // 대시 전후 Z키 버퍼
    public const int MAX_GRAB_TIME = 150;       // 최대 잡기 유지 프레임
    public const int GRAB_READY_TIMEOUT = 180;  // GRAB_READY 유지 최대 프레임
    public const int GRAB_AIM_TIME = 12;        // Z 누름 시 조준 윈도우 프레임
    public const int GRAB_SLOW_FACTOR = 3;      // 조준 슬로우 배율
    public const int NTT_WIDTH = 14;            // 잡기 대상 NTT 너비
    public const int NTT_HEIGHT = 14;           // 잡기 대상 NTT 높이
    public const int ENEMY_WIDTH = 16;          // 적 히트박스 너비
    public const int ENEMY_HEIGHT = 16;         // 적 히트박스 높이
    public const int ARMORED_ENEMY_HP = 2;      // 강화 적 기본 HP

    // ── Phase 3: 적 / NTT ────────────────────────────────────────
    public const int INVINCIBLE_TIME = 60;       // 피격 후 무적 프레임
    public const int RESPAWN_TIME = 180;         // 적 파괴 후 재생성 프레임
    public const double ROPE_LENGTH = 80;        // 줄 NTT 줄 길이(픽셀)
    public const double ROPE_GRAVITY = 0.3;      // 줄 NTT 진자 중력 계수
    public const double ROPE_START_ANGLE = 0.5;  // 줄 NTT 초기 각도(라디안)
    public const double PUSH_SPEED = 6.0;        // 릴리즈 시 밀쳐지는 속도
    public const int PUSH_RETURN_TIME = 120;     // 밀쳐진 후 복귀 시작까지 프레임
    public const double PUSH_RETURN_SPEED = 1.5; // 원위치 복귀 속도
    public const double ENEMY_PATROL_RADIUS = 80;  // 적 활동 반경(미사용)
    public const double ENEMY_DETECT_RANGE = 120;  // 플레이어 감지 범위(미사용)
    public const double PROJECTILE_SPEED = 5.0;    // 투사체 속도(미사용)
    public const int PROJECTILE_LIFETIME = 180;    // 투사체 최대 생존(미사용)

    // ── Phase 3: 오브젝트 ────────────────────────────────────────
    public const double JUMP_PAD_SPEED = -15.0;    // 점프패드 수직 속도
    public const double SPRING_SPEED = 10.0;       // 스프링 발사 속도(수평 성분)
    public const double SPRING_LAUNCH_V = -12.5;   // 위 스프링 수직 발사 속도
    public const double SPRING_WALL_V = -14.5;     // 벽 스프링 수직 발사 속도
    public const int SPRING_FORCE_TIME = 5;        // 스프링 발사 후 입력 잠금 프레임
    public const int JUMP_PAD_COOLDOWN = 12;       // 점프패드 재발동 방지 프레임
    public const int SPRING_COOLDOWN = 12;         // 스프링 재발동 방지 프레임
    public const double PLATFORM_RETURN_SPEED = 1.0; // 발판 원위치 복귀 속도

    // ── Phase 4: 카메라 / 레벨 ───────────────────────────────────
    public const double CAMERA_LERP = 0.12;        // (구) 추적 보간 — 미사용
    public const int CAMERA_ZOOM = 2;              // 화면 확대 배율
    public const int ROOM_W_TILES = 30;            // 한 방 가로 타일 수
    public const int ROOM_H_TILES = 17;            // 한 방 세로 타일 수
    public const int INTERNAL_W = ROOM_W_TILES * TILE_SIZE;  // 내부 렌더 너비(480)
    public const int INTERNAL_H = ROOM_H_TILES * TILE_SIZE;  // 내부 렌더 높이(272)
    public const double ROOM_SLIDE_LERP = 0.18;    // 방 전환 슬라이드 보간 계수
    public const double PARALLAX_SKY = 0.10;       // bg_sky 스크롤 비율
    public const double PARALLAX_FAR = 0.30;       // bg_bamboo_far 스크롤 비율
    public const double PARALLAX_NEAR = 0.60;      // bg_bamboo_near 스크롤 비율
    public const int ANIM_FRAME_DUR = 6;           // 애니 프레임당 게임프레임 수

    // 레벨 시퀀스 — 순서대로 클리어 시 다음으로 전환
    public static readonly string[] LEVEL_FILES =
    {
        "assets/tilemaps/level1.txt",
        "assets/tilemaps/level2.txt",
        "assets/tilemaps/level3.txt",
        "assets/tilemaps/level4.txt",
        "assets/tilemaps/level5.txt",
    };

    // ── 메뉴 / 타이틀 / 일시정지 (문자열·크기; 색상은 Palette) ────
    public const int MENU_OVERLAY_ALPHA = 170;     // 일시정지 오버레이 알파(0~255)
    public const int TITLE_BIG_SIZE = 64;          // 타이틀 큰 글자 크기
    public const int MENU_ITEM_SIZE = 28;          // 메뉴 항목 글자 크기
    public const int MENU_HINT_SIZE = 16;          // 조작 안내 글자 크기
    public const string TITLE_TEXT = "HYPE JUMPER";       // 타이틀 대문 문구
    public const string TITLE_HINT = "아무 키나 눌러 시작";  // 타이틀 시작 안내
    public const string PAUSE_TITLE = "일시정지";          // 일시정지 메뉴 제목
    public static readonly string[] PAUSE_ITEMS = { "계속 진행", "끝내기" };  // 일시정지 항목
    public const int MENU_ITEM_GAP = 44;           // 메뉴 항목 간 세로 간격
}
