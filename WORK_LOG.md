# WORK_LOG

> Claude Code 자동 작업 내역. 최신 항목이 위로.

---

## 2026-06-02 (18)

### [Phase 4] 6-5 플레이어 애니메이션 — 상태별 번호 프레임 순환 (3단 폴백)

- **수정 파일**: settings.py, src/assets.py, src/player.py
- **승인**: 비니 "ㄱㄱ". 렌더 한정(player.draw + assets + settings), 물리 무수정.
- **변경 내용**:
  - settings.py: `ANIM_FRAME_DUR=6`(프레임당 게임프레임, ≈10fps 애니).
  - assets.py: `frame_names(base)` — `base_0,base_1,…` 연속 번호 프레임 존재분 리스트 반환(캐시), 없으면 빈 리스트.
  - player.py: `_anim_t`(draw마다 증가, 렌더 클럭). `draw`가 `_animated(self._sprite_names())` 사용. `_animated`: 첫 상태 base에 번호 프레임이 있으면 `(_anim_t//ANIM_FRAME_DUR)%N` 현재 프레임을 앞에 끼움, 없으면 bases 그대로. **3단 폴백**: 번호프레임 → 단일 스프라이트(6-2) → 사각형. **물리 수식 무수정**.
- **검증(자동)**: 프레임0개→bases 불변·frame_names 빈리스트 / 단일 player_idle→애니 없음 / player_run_0~2→frame_names 3개·0→1→2 순환·3에서 0 래핑 / Game 120f 애니 draw 예외0 — **7/7 PASS**. 테스트 스프라이트 삭제.
- **원본 파일 업데이트 여부**: 없음. (ASSETS.md 6-5. player_<state>_<n>.png 번호 프레임 규칙은 신규 — organize는 player_ 접두사로 이미 처리.)
- **다음 작업**: 비니가 `player_run_0.png`,`player_run_1.png`… 드롭→organize→자동 애니. **비주얼 기초(6-1~6-5) 완료.** 이후: UI 폴리싱 / 데드존·카메라룩 / 투사체 적(3C-5).

## 2026-06-02 (17)

### [Phase 4] 6-4 패럴럭스 배경 — bg 3레이어 카메라 속도차 스크롤 (폴백: 그라데이션)

- **수정 파일**: settings.py, src/scene.py
- **승인**: 비니 "6-4 시작하자". 렌더 한정(scene.py + settings).
- **변경 내용**:
  - settings.py: `PARALLAX_SKY=0.10`/`PARALLAX_FAR=0.30`/`PARALLAX_NEAR=0.60` (카메라 x의 일부만 스크롤 → 깊이감).
  - scene.py: `_draw_background`가 기존 그라데이션+반딧불 base 위에 `bg_sky`→`bg_bamboo_far`→`bg_bamboo_near` 순으로 패럴럭스 레이어를 깊이순 렌더. 신규 `_draw_parallax_layer`: 카메라 x×factor만큼 **가로 무한 타일링**(`%w` 래핑), 세로는 바닥 정렬(위 빈 곳은 base 그라데이션이 비침 → 투명 bg와 자연 합성). **bg 스프라이트 없으면 스킵 → 기존과 100% 동일**.
- **검증(자동)**: (1) bg 0개 → base 그라데이션만(상단=BG_TOP) = 기존 동일 (2) bg_sky 투입 → 상단이 sky색으로 덮임(레이어 렌더) (3) 카메라 x 0 vs 200 → 스트라이프 bg 행이 달라짐(패럴럭스 스크롤) (4) Game 90f 예외0 — **4/4 PASS**. 테스트 bg 삭제.
- **원본 파일 업데이트 여부**: 없음. (ASSETS.md 6-4 항목, bg 레이어는 design.md 콘셉트.)
- **다음 작업**: 비니가 `bg_sky.png`/`bg_bamboo_far.png`/`bg_bamboo_near.png`(1920×540 권장, 투명 PNG) 드롭→organize→자동 패럴럭스. 또는 6-5 플레이어 애니메이션. 커밋 대기.

## 2026-06-02 (16)

### [Phase 4] 6-3 사운드 — audio.py(SFX/BGM 로더) + 이벤트 지점 재생, 폴백 무음

- **생성/수정 파일**: src/audio.py(신규), src/player.py, src/entities/{enemy,spring,jump_pad}.py, src/scene.py, main.py
- **승인**: 구조 평가 보고서 승인(비니 "전체"). 물리 수식 무수정·소리 side-effect만.
- **변경 내용**:
  - `audio.py`: `init()`(mixer 초기화, 실패=전체 무음), `play(name)`(=`sfx_<name>` 탐색·캐시·재생, 없으면 무음), `play_music(name)`(=`bgm_<name>` 루프, 같은 곡 중복 무시). 미존재 None 캐시.
  - player.py: `audio.play` 호출 — jump/walljump/dash + `_flash_tech` 한 곳에서 super/hyper/wallbounce(대시테크·릴리즈 양쪽 커버) + grab/release + **착지(`_prev_on_ground` 엣지)**. **물리 연산 수식 무수정**(소리 1줄 + 접지 엣지 플래그만).
  - enemy.py `_take_hit`: HP0=enemy_break / 잔존=enemy_hit. spring/jump_pad on_enter: spring/jumppad. scene.py: set_checkpoint=checkpoint, respawn=death, request_advance=goal(1회), `_load_level`에 `play_music("stage1")`. main.py: `audio.init()`.
- **검증(자동, dummy 오디오)**: init 전 무no-op / mixer init OK / 없는 sfx None캐시·무음 / sfx 투입 시 Sound 로드 / bgm 등록·중복무시 / Game 120f 이벤트사운드 경로 예외0 — **7/7 PASS**. 테스트 음원 삭제.
- **발견**: 비니가 `assets/sounds/bgm/bgm_stage1.ogg` 실제 배경음 투입 → `play_music("stage1")`이 자동 루프(이번 로더로 바로 동작).
- **원본 파일 업데이트 여부**: 없음. (사운드는 ASSETS.md 6-3. src/audio.py 신규.)
- **다음 작업**: 비니가 `sfx_*.wav` 드롭→organize→자동 재생. 또는 6-4 패럴럭스 배경 / 6-5 애니메이션. 커밋 대기.

## 2026-06-01 (15)

### [Phase 4] 6-2 스프라이트 로더 — 중앙 캐시 + 폴백(없으면 사각형), 전 엔티티 배선

- **생성/수정 파일**: src/assets.py(신규), src/player.py, src/scene.py, src/tilemap.py, src/entities/{hazard,spring,jump_pad,platform,ntt,enemy}.py
- **승인**: 구조 평가 보고서 승인(비니 "전체"). 엔티티+플레이어+타일/발판 타일링 포함.
- **변경 내용**:
  - `assets.py`: `get_sprite`(name→Surface|None, 미존재도 캐시), `first_sprite`(이름 리스트 중 첫 존재), `blit_or_rect`(스프라이트 있으면 **발-중앙 정렬 blit**, 없으면 사각형), `tile_fill`(타일 반복+clip, 없으면 사각형). sprites/{player,entities,tiles,bg,ui} 검색. convert_alpha 실패(헤드리스)는 raw 폴백.
  - 각 draw의 `pygame.draw.rect` → `assets.blit_or_rect`/`tile_fill`로 교체: hazard="spike", spring=방향별("spring_up"/"spring_wall"), jump_pad="jumppad", platform=tile_fill("platform"), ntt="ntt", rope_ntt="rope_ntt"(줄 선은 유지), enemy="enemy"/"enemy_armored"(무적 깜빡임은 빈 names로 사각형 유지), Checkpoint="checkpoint(_on)", Goal="goal", tilemap solids=tile_fill("tile_ground").
  - player.py: draw 본체만 `blit_or_rect(self._sprite_names()…)`, 신규 `_sprite_names()`(상태별 idle/run/jump/fall/dash/duck 우선순위, 미존재 시 idle→사각형). **물리/충돌/상태 로직 전부 무수정**(렌더 한정).
- **검증(자동)**: (1) 에셋 0개 → 폴백 사각형 렌더(바닥 COLOR_SOLID·적 COLOR_ENEMY 픽셀 존재) = **기존과 동일** (2) enemy.png(고유색) 투입+캐시클리어 → 적 사각형색 감소·스프라이트색 출현(교체 확인) (3) tile_ground.png → 바닥 타일링·COLOR_SOLID 감소 (4) Game 90f 예외0. **6/6 PASS**. 테스트 스프라이트 삭제(dir엔 .gitkeep만).
- **원본 파일 업데이트 여부**: 없음. (스프라이트 로더는 ASSETS.md 6-2 항목. src/assets.py 신규.)
- **다음 작업**: 비니가 ASSETS.md 규칙대로 PNG를 ROOT에 드롭→organize→화면에 자동 표시. 또는 6-3 사운드(`audio.py`), 6-4 패럴럭스, 6-5 애니메이션. 커밋 대기.

## 2026-06-01 (14)

### [Phase 4] 6-1 자산 구조 + 인테이크 워크플로우 + ASSETS.md 박제

- **생성 파일**: assets/sprites/{player,entities,tiles,bg,ui}/.gitkeep, assets/sounds/{sfx,bgm}/.gitkeep, tools/organize_assets.py, ASSETS.md
- **승인**: 비니 "6-1 단계를 하자 + asset.md 만들어 목록 박제". 인테이크 방식 = 자동 스크립트(B) 채택.
- **변경 내용**:
  - 폴더 구조: CLAUDE.md assets/ 구조 + 세분화(sprites/player·entities·tiles·bg·ui, sounds/sfx·bgm). 빈 폴더 git 추적용 .gitkeep.
  - `tools/organize_assets.py`: ROOT(또는 _inbox/) **최상위만** 스캔(assets/ 정리분 보호) → 파일명 접두사(player_/tile_/bg_/ui_/sfx_/bgm_/map_) + 엔티티 화이트리스트(enemy/ntt/spring_up…)로 분류 이동. `--dry-run` 미리보기. **모르는 파일은 스킵**(map_preview*.png 등 프로젝트 파일 보호). 같은 위치/덮어쓰기 처리.
  - `ASSETS.md`: 폴더 구조·인테이크 워크플로우·파일명 규칙·스프라이트 목록(히트박스/캔버스/우선순위·상태⬜)·사운드 목록(이벤트/우선/상태)·진행 6-1~6-5. 폴백 원칙(에셋 없으면 사각형/무음) 명시.
  - **게임 코드 무변경** — 폴더/툴/문서만. main/player/물리 무관.
- **검증(자동)**: 더미 7개(sfx_/player_/엔티티/bg_/map_ + 모르는 2개) → dry-run==real, 5개 정확 라우팅, random_thing.png·notes.txt·map_preview_visual.png **미이동(보호)** 확인. 더미 정리 후 clean 루트 dry-run 0건. organize_assets import/실행 정상.
- **원본 파일 업데이트 여부**: 없음. (ASSETS.md 신규 문서, 폴더/툴 신규. CLAUDE.md assets/ 구조와 일치.)
- **다음 작업**: 비니가 에셋을 규칙대로 ROOT에 드롭 → `python tools/organize_assets.py`로 정리. 또는 6-2(스프라이트 로더 `assets.py` — 있으면 blit/없으면 사각형) 착수. 커밋 여부 대기.

## 2026-06-01 (13)

### [Phase 4] 비주얼 기초 — design.md 팔레트 재매핑 + 비취 야간 그라데이션 배경 + 정적 반딧불

- **수정/생성 파일**: settings.py, src/scene.py, main.py, map_preview_visual.png(육안용)
- **승인**: 비니 "비주얼 기초로 깔아보자" + 구조 평가 보고서 승인("승인 — 팔레트+배경+반딧불"). design.md "Organic Eco-Hologram"(Bright & Peaceful Bio-Cyberpunk) 방향.
- **변경 내용**:
  - settings.py: 팔레트 상수 블록 신설 — JADE/JADE_DEEP/MINT/MILKY/MIST_GRAY/GOLD/CORAL/MAGENTA/VIOLET (design.md Main70/Sub20/Point10). 기존 COLOR_* **값만 팔레트 기반 재매핑**(이름·구조 유지): 플레이어=유백(최대 대비), 바닥=안개비취(어둡게), 발판=민트, 가시/잡기불가=코랄, 체크포인트=짙은비취/활성=황금, 적=주황코랄/강화=자홍, NTT=보라, 골=황금. 배경상수 COLOR_BG_TOP/BOTTOM·COLOR_FIREFLY·FIREFLY_COUNT 추가.
  - scene.py: `_build_background(size)`(세로 그라데이션 보간 + 고정시드 random으로 반딧불 정적 배치) + `_draw_background`(1회 베이킹 후 blit, `_bg_surface` 캐시). `draw` 맨 앞에서 배경 그림. `__init__`에 `_bg_surface=None`.
  - main.py: 중복 `screen.fill` 제거 — 배경은 Scene 소유.
  - **player.py·물리·엔티티 로직 무관**(색/배경 한정).
- **검증(자동)**: 팔레트 import OK / 그라데이션 베이킹 surface top=BG_TOP·bottom=BG_BOTTOM 정확 / 반딧불 픽셀 존재 / 배경 1회 캐싱(재빌드 없음) / 지형이 배경 위 정상 합성 / Scene 180f + Game 60f 예외0 — **PASS**. (1차 'bottom FAIL' 2건은 샘플 픽셀이 반딧불/바닥지형과 겹친 테스트 아티팩트 — 베이킹 surface 직접 검증으로 정상 확인.) map_preview_visual.png 육안: 비취 야간 그라데이션+반딧불, 캐릭터/위험 대비 양호.
- **원본 파일 업데이트 여부**: 없음. (비주얼/팔레트는 design.md 방향, 색값 재매핑·배경은 코드/튜닝 범위.)
- **다음 작업**: 비니 직접 플레이 — `python main.py`로 동화풍 야간 톤·반딧불·가독성 체감. 승인 시 다음(스프라이트 파이프라인 / 파티클·UI / 투사체 적). 커밋 여부 비니 지시 대기.

## 2026-06-01 (12)

### [Phase 4] 카메라 시스템 + 다중 레벨 로드/전환 ("게임에 살 붙이기" 5-1·5-2)

- **수정/생성 파일**: src/camera.py(신규), assets/tilemaps/level1.txt·level2.txt(신규), settings.py, src/tilemap.py, src/scene.py, main.py
- **승인**: TASK.md "게임에 살 붙이기" 지시 + 구조 평가 보고서 제출. 비니 선택 = **A안(기존 offset 시그니처 유지)** + **5-1·5-2 한 세션**. 제약: player.py 물리 무수정 / 카메라 camera.py 캡슐화·Scene 제어 / 맵 경계 클램프 / pygame만.
- **변경 내용**:
  - `camera.py` `Camera`(신규): `offset`(round된 정수 튜플), `update(target_rect, map_w, map_h)`=대상 중심으로 CAMERA_LERP 보간 후 경계 클램프, `snap`=즉시 중앙(로드/리스폰 패닝 방지), `_clamp`=[0, map-view] (맵<화면이면 0). float 내부 보관, draw용 정수 노출.
  - `settings.py`: Phase 4 섹션 추가 — `CAMERA_LERP=0.12`, `COLOR_GOAL`, `LEVEL_FILES=[level1,level2]` (추가만).
  - `tilemap.py`: `__init__`에 `goal_rects`·`width`/`height`(맵 픽셀크기) 추가. `load_text`가 그리드 폭·행수로 width/height 계산 + `'G'`(골) 기호 파싱. `_reset_collections`에 goal_rects/크기 리셋.
  - `scene.py`: `Goal(Trigger)` 신설(Checkpoint와 동형, on_enter→`request_advance`). `__init__`→Camera 생성+`_load_level(0)`. 신규 `_load_level(index)`(LEVEL_FILES에서 맵·체크포인트·골·플레이어·카메라snap), `request_advance`/`next_level`(마지막이면 유지). `update`: 골 예약 시 `next_level` 후 return / 낙사 임계 `SCREEN_HEIGHT`→`tilemap.height` / 끝에 `camera.update`. `_check_triggers`에 goals 루프. `respawn`에 camera.snap. `draw(surface)`(offset 인자 제거, 내부 `camera.offset` 사용)에 골 렌더 추가. **A안이라 엔티티 draw 시그니처는 전부 무수정**.
  - `main.py`: HUD에 `level=i/N cam=offset map=WxH` 줄 추가(검수 가시성). `scene.draw(screen)` 호출은 새 시그니처와 일치(무변경).
  - 큰 맵 2개: level1(1920×720, 완만 계단+골 도달 가능, 가로/세로 스크롤), level2(1600×608, 가로 위주). 코드 생성(유효 그리드).
- **검증(자동)**: (1) 카메라 — 초기 클램프(x≥0·y≤map-view·경계내) / 우측 추적 offset.x 증가 / 좌·우 끝 클램프 = **11/11 PASS** (2) 가로 스크롤 33구간 단조증가·끝 960클램프 (3) 레벨 전환 0→1(level2 1600 로드·새 스폰·카메라 스냅)·마지막 레벨 유지 (4) Surface draw 300f 예외0 / 실제 Game 객체 120f 예외0(HUD 렌더 포함). **player.py git diff 무변경 확인**(제약ⓐ).
- **원본 파일 업데이트 여부**: 없음. (카메라/레벨전환은 CLAUDE.md Phase 4·구조(camera.py)에 명세. 텍스트맵 'G' 기호·LEVEL_FILES·Goal 트리거는 신규 — 문서화 필요 시 위치 명시 후 승인.)
- **다음 작업**: 비니 직접 플레이 — `python main.py` → level1에서 우측·위로 이동 시 카메라 부드럽게 추적·맵 경계 밖 여백 안 보임·골(노란칸) 도달 시 level2 전환·새 스폰 확인. 승인 시 (선택) 데드존/룩(Spelunky) / 투사체 적(3C-5) / 비주얼.

## 2026-06-01 (11)

### [Phase 3] 타일맵 파일 로더 — 2섹션 텍스트([MAP]/[OBJECTS]) 외부 파일 로드

- **수정/생성 파일**: assets/tilemaps/test_map.txt(신규), src/tilemap.py
- **승인**: 구조 평가 보고서 승인(비니 "승인 + TileMap만"). 연결 방식 = **TileMap만 파일 지원 추가, scene.py는 기존 하드코딩 유지(비파괴)**. 새 디렉터리 assets/tilemaps/ + 파일 1개 생성 승인.
- **포맷**: 2섹션 텍스트. `[MAP]` = 60칸 ASCII 그리드(# 막힘 / ^ 가시 / C 체크포인트 / P 스폰 / J 점프패드, 기존 load_text 그대로 재사용). `[OBJECTS]` = 파라미터형 객체 줄단위(픽셀좌표): platform x y w h axis dist speed / spring x y w h dir / ntt x y / ropentt 피벗x y / enemy x y / armored x y.
- **변경 내용**:
  - test_map.txt: 기존 하드코딩 맵(`_build_test_text` 그리드 + `_build_platforms`/`_build_objects` 객체)을 코드로 덤프해 **바이트 단위 동일** 생성(전사 오류 방지).
  - tilemap.py: `__init__(text=None, map_file=None)` — map_file 주면 `_try_load_file`, 실패/없으면 기존 하드코딩 경로(비파괴). 신규 `_try_load_file`(try/except → OSError/ValueError 시 `_reset_collections` 후 폴백), `load_file`(섹션 분리 → load_text + _parse_objects), `_parse_objects`(주석'#'/빈줄 무시), `_spawn_object`(타입 디스패치), `_reset_collections`(부분 로드분 폐기), 모듈 `_num`(int/float/문자 변환). load_text/_build_* 전부 무변경(폴백 경로로 보존).
  - scene.py: **무변경** — `TileMap()` 무인자 → 하드코딩 동일 동작.
- **검증(자동)**: (1) 파일 로드 결과 == 하드코딩 9개 항목(solids/hazards/cp/pad/spawn/plat/spring/ntt/enemy) **9/9 PASS** (2) 없는 파일 → 폴백 == 하드코딩 PASS (3) 잘못된 객체 타입 → ValueError → 폴백 == 하드코딩 PASS (부분 로드분 reset 검증) (4) 파일맵 update 180f 예외0(발판 이동·진자). **버그 2건 발견·수정**: ① 폴백 print의 em-dash가 Windows cp949 콘솔 인코딩 크래시 → ASCII로 교체 ② 파싱 중 예외 시 부분 로드분이 폴백 위에 중복 누적 → `_reset_collections`로 초기화.
- **원본 파일 업데이트 여부**: 없음. (타일맵 파일 로더는 CLAUDE.md Phase 3 명세 항목. 2섹션 텍스트 포맷은 신규 — 문서화 필요 시 위치 명시 후 승인.)
- **다음 작업**: 비니 직접 플레이/확인 — `python main.py`는 기존과 동일(하드코딩). 파일 로더 체감하려면 scene 연결이 별도 세션 필요(이번엔 TileMap만). 승인 시 (선택) scene.py를 test_map.txt 로드로 연결 / 또는 Phase 3C-5(투사체 적) / 또는 Phase 4 카메라.

## 2026-06-01 (10)

### [전환] Unity/C# 재구성 결정 + DASH_STRIKE 제거 + 핸드오프 정리 + 유니티 코어 이식

- **수정/생성 파일**: src/player.py, src/entities/enemy.py(제거), PLANNING.md(추가), TASK.md(갱신), unity_port/*(신규), 메모리 unity-port-pivot
- **결정(비니)**: hypeJumper를 Pygame/Python → **Unity/C#로 재구성**. 파이썬은 레퍼런스로 동결. DASH_STRIKE는 기능 불필요 → 제거.
- **A. DASH_STRIKE 제거**: player.py에서 dispatch 분기·_update_dash 스트라이크 트리거·_find_strike_target/_start/_update/_end_dash_strike·init 필드(dash_strike_buffer/strike_target/strike_timer)·_tick_buffers 항목·_find_grab_target range 파라미터 전부 되돌림. enemy.py `strike()` 제거. PlayerState.DASH_STRIKE enum 값은 예약만 유지. (스모크 검증은 비니가 중단 — 유니티 전환으로 파이썬 추가검증 보류)
- **B. 핸드오프 정리**:
  - PLANNING.md: 잡기 섹션에 ⚠️ 최신사양 안내 + 끝에 `### [재설계] 잡기 시스템 최신 사양` 서브섹션 **추가만**(기존 초기안 미변경, 규칙#8). Z누름 윈도우+슬로우·가시차단·아래=대시·줄NTT·DASH_STRIKE제거 명시.
  - TASK.md: 죽어있던 내용(Phase1 세션1) → 현재 실제 진행도 + 유니티 전환으로 전면 갱신.
- **C. 유니티 포팅 산출물 `unity_port/`**:
  - `README_SETUP.md`: 유니티 초보용 — Hub 설치→2D프로젝트→스크립트 임포트→Solid 레이어→씬 구성(바닥+플레이어)→FixedTimestep 1/60→Play(A/D·C)→전체화면→프로젝트 구조(Title/Tutorial/1-1, Tilemap, Cinemachine)→다음 이식 순서.
  - `Assets/Scripts/` C# 5종: GameSettings(상수, **y축 반전**=점프 양수·중력 빼기, PPU16), PlayerState(enum), InputBuffer, PlayerInputState, **PlayerController**(MonoBehaviour, Rigidbody 미사용 커스텀 AABB, FixedUpdate 60Hz, Phase1 이동=Approach 수평·가변점프·코요테·점프버퍼·벽/천장 충돌·월점프·월슬라이드).
  - 포팅 원칙: 로직층 엔진무관, Time.timeScale로 슬로우, 잡기는 PLANNING 아닌 WORK_LOG 사양 기준.
- **검증**: 유니티 빌드/실행은 이 환경(파이썬) 밖이라 불가 → C# 코드 검토만. 비니가 유니티에서 실행 확인 예정.
- **원본 파일 업데이트**: PLANNING.md = 잡기 섹션에 안내+재설계 서브섹션 추가(추가만). TASK.md = 작업파일이라 현행화. CLAUDE.md 미변경.
- **다음 작업**: 비니 유니티 설치 후 README 따라 Phase1 이동 손맛 확인 → 대시(Phase2) C# 이식. (선택) 파이썬 tests/는 전환으로 가치 하락, 보류 권장.

## 2026-06-01 (9)

### [Phase 3C-4] DASH_STRIKE (벽력일섬) — 대시 중 Z로 적 돌진 처치

- **수정 파일**: src/entities/enemy.py, src/player.py
- **승인**: 3C-4 구조 평가 보고서 승인(비니 "승인 — 이대로"). 비니 관찰("대시 중 그랩 안 됨")이 곧 이 기능(3C-1에서 의도적 제외분).
- **변경 내용**:
  - enemy.py: `strike()` 추가 — 밀쳐냄 없이 `_take_hit`(돌진 통과 처치).
  - player.py: `dash_strike_buffer`(InputBuffer DASH_STRIKE_BUFFER=8, 통합 버퍼 5번째). `_update_dash`에 Z 입력 시 버퍼 set → `DASH_STRIKE_RANGE`(120)+시야 통과 '처치가능 적'(strike 보유) 있으면 `_start_dash_strike`. 신규 `_find_strike_target`(strike 가진 대상만, range 파라미터), `_start_dash_strike`(대상 방향 등속 돌진), `_update_dash_strike`(겹치면 strike→모멘텀 유지·대시충전·NORMAL, 벽막힘/타임아웃 종료), `_end_dash_strike`. `_find_grab_target`에 grab_range 파라미터 추가(기본 GRAB_RANGE). dispatch에 DASH_STRIKE 분기. `_tick_buffers`에 dash_strike_buffer.
  - scene/tilemap: 무변경 — 기존 grabbables(NTT+적)·hazards가 이미 player.update로 전달, 기존 적으로 테스트.
- **검증(자동)**: 대시 중 Z→DASH_STRIKE→적 처치·대시충전·모멘텀 유지·NORMAL / 강화적 1타 생존+무적(2타 파괴) / 대상없음 DASH 유지 / NTT는 대상 아님(strike 없음) / 벽 뒤 적 차단(LOS) / 회귀 일반 대시 정상 종료 — **10/10 PASS**. 씬 300f(잡기+대시+스트라이크 혼합) 예외0, DASH_STRIKE 발동 확인.
- **원본 파일 업데이트 여부**: 없음. PLANNING 잡기 신설계 갱신(승인 대기)에 DASH_STRIKE도 포함 예정.
- **다음 작업**: 비니 직접 플레이 — 대시로 적 향해 Z 눌러 꿰뚫고 처치+가속 유지되는지. 승인 시 Phase 3C-5(투사체 쏘는 적) = 3C 마지막 / PLANNING 갱신.

## 2026-06-01 (8)

### [Phase 3C-3] 줄 NTT (RopeNTT) — 상시 진자 + 잡기 동행 + 릴리즈 진자속도 가산

- **수정/생성 파일**: settings.py, src/entities/ntt.py(RopeNTT 추가), src/player.py, src/tilemap.py
- **승인**: 3C-3 구조 평가 보고서 승인(비니 "승인 — 이대로"). RopeNTT는 NTT 상속.
- **변경 내용**:
  - settings: `ROPE_START_ANGLE=0.5`(상시 진폭), 색 2개(COLOR_ROPE_NTT, COLOR_ROPE_LINE). ROPE_LENGTH/GRAVITY 기존.
  - `ntt.py` `RopeNTT(NTT)`: 천장 pivot에 매달려 PLANNING 공식대로 상시 진자(`update`가 잡혀도 `_swing` 계속). `on_release`=잡힘만 해제(밀쳐냄·파괴 없음, 영구). `release_velocity()`=접선속도(av*L*cosθ, av*L*-sinθ). draw=피벗~NTT 줄 + 본체.
  - `player.py` `_release_grab`: `getattr(target,'release_velocity',…)`로 진자 접선속도 받아 테크 적용 후 `vx_external+=rvx; vy+=rvy` 가산(고정 NTT/적은 0이라 무영향). down→대시 분기는 가산 제외(대시 우선).
  - `tilemap.py`: 중앙 선반 위 `RopeNTT(520,320)` 1개(선반에서 잡기 테스트).
  - scene.py: 무변경 — RopeNTT가 ntts 리스트에 들어가 update/draw/grabbables 제네릭 처리.
- **검증(자동)**: 진자 위치/각 변함·release_velocity 비0 / Z누름→ACTIVE·잡은 채 플레이어 진자 따라 이동 / 릴리즈 vx·vy에 진자 가산(위=월바운스+진자) / 줄 파괴X·잡힘만 해제 / 회귀 고정NTT 릴리즈 가산0 — **8/8 PASS**. 씬 240f 예외0(진자 지속). map_preview_3c3.png 샹들리에 줄+스윙 육안 확인.
- **원본 파일 업데이트 여부**: 없음. PLANNING 잡기 신설계 갱신(승인 대기) 시 줄 NTT 동작도 포함.
- **다음 작업**: 비니 직접 플레이 — 중앙 선반 위 샹들리에 Z로 잡고 흔들리다 릴리즈 시 진자 방향으로 날아가는지 확인. 승인 시 Phase 3C-4(DASH_STRIKE 벽력일섬 + 투사체 적) / PLANNING 갱신.

## 2026-06-01 (7)

### [Phase 3C-1][피드백] 잡기 = Z 누름 윈도우(허공 자동취소) + Z 누름 시 슬로우모션

- **수정 파일**: settings.py, src/player.py, main.py
- **피드백(비니)**: 1) Z 계속 홀드로 조준 유지 = 개사기 → 허공(대상없음)에서 Z면 잡기 취소(바로 해제+원형 잠깐 표시). 2) Z 누르면 게임시간 잠깐 슬로우.
- **변경 내용**:
  - settings: `GRAB_AIM_TIME=12`(Z 누름 후 조준 윈도우, 만료 시 자동취소·홀드무관), `GRAB_SLOW_FACTOR=3`(윈도우 동안 1/3 속도).
  - player.py: 잡기 진입을 `grab_held`→**`grab_pressed`(엣지)**로 변경. 진입 시 `aim_timer=GRAB_AIM_TIME`+`aim_slow=True`. `_update_grabbing` SEEKING은 윈도우 카운트다운 → 대상 확보 시 즉시 ACTIVE(슬로우 종료), 미확보 시 만료되면 NORMAL 취소(슬로우 해제). **홀드로는 재진입 불가(엣지 필요)** → 캠핑 방지. ACTIVE는 기존대로 grab_held 유지·뗌 시 릴리즈.
  - main.py: `aim_slow` 동안 `_should_step`이 GRAB_SLOW_FACTOR 프레임마다 1번만 scene.update → 슬로우모션(렌더는 매프레임). 엣지 입력은 스킵 프레임 동안 보존.
- **검증(자동)**: 허공 Z→SEEKING+슬로우 / 홀드해도 윈도우 만료 시 취소·슬로우해제 / 홀드만으론 재진입X(엣지 필요) / 대상 있으면 즉시 ACTIVE+슬로우종료 / 윈도우 중 범위내 잡힘 / 릴리즈 처치 — 8/8. main 슬로우 1/3 정확(9프레임→3스텝) + 240f 슬로우포함 예외0 — **합계 PASS**.
- **원본 파일 업데이트 여부**: 없음. PLANNING 잡기 신설계 갱신(승인 대기)에 'Z 누름 윈도우·허공 취소·슬로우'도 포함 예정.
- **다음 작업**: 비니 재플레이 — Z 탭 시 슬로우+원형 잠깐, 허공이면 취소(못 버팀), 대상 근처면 잡힘 확인. 승인 시 Phase 3C-3(줄 NTT) / PLANNING 갱신.

## 2026-06-01 (6)

### [Phase 3C-2] 적 — Enemy(HP1) / ArmoredEnemy(HP2+) 잡기 처치·무적·리스폰

- **수정/생성 파일**: settings.py, src/entities/enemy.py(신규), src/scene.py, src/tilemap.py
- **승인**: 3C-2 구조 평가 보고서 승인(비니 "승인 — 이대로"). enemy.py 신규, player 무수정, 이동 AI 제외.
- **변경 내용**:
  - settings: 색 3개(COLOR_ENEMY/ARMORED/HIT) + 크기(ENEMY_WIDTH/HEIGHT=16) + ARMORED_ENEMY_HP=2. INVINCIBLE_TIME/RESPAWN_TIME/PUSH_* 기존 재사용.
  - `enemy.py`: `Enemy(Actor)` GRABABLE, **NTT와 동일 잡기 인터페이스**(grabbed/on_grab/on_release/center/rect) + `grabbable()`(파괴·무적 중 제외). on_release→밀쳐짐+`_take_hit`(HP--). HP0=파괴(destroyed·RESPAWN_TIME 후 origin 재생성), HP잔여=INVINCIBLE_TIME 무적(투명 깜빡임)+밀쳐짐+복귀. `ArmoredEnemy(Enemy)` HP2.
  - `scene.py`: 잡기 대상 = NTT + 살아있는 적(`grabbable()` 필터)로 합쳐 player.update 전달. 적 update/draw 추가. 사망 리스폰 시 잡힌 대상 `grabbed=False`(피격 없이 해제).
  - `tilemap.py`: 일반 적(col27)·강화 적(col34) 바닥 위 배치(가시/구덩이 회피).
  - **player.py: 무수정** — 잡기 인터페이스 동일, 처치 시 대시충전은 기존 `_release_grab`(dashes=MAX)이 이미 처리.
- **검증(자동)**: 일반적 잡기→파괴+대시충전+리스폰타이머 / RESPAWN_TIME 후 부활(hp복구) / 강화적 1타 생존+무적·재잡기불가 → 무적해제 후 재잡기 → 2타 파괴 / 무적 중 잡기 불가 / NTT는 파괴X 복귀(회귀) — **11/11 PASS**. 씬 240f 잡기스팸(처치·리스폰 포함) 예외0. map_preview_3c2.png 적 배치·강화적 잡기 육안 확인.
- **원본 파일 업데이트 여부**: 없음(Enemy/ArmoredEnemy는 CLAUDE/PLANNING에 명세, settings 신규 추가). PLANNING 잡기 신설계 갱신은 여전히 승인 대기.
- **다음 작업**: 비니 직접 플레이 — 일반적 1타 처치·강화적 2타·무적 깜빡임·리스폰·처치 대시충전 확인. 승인 시 Phase 3C-3(줄 NTT 진자) 또는 PLANNING 갱신 반영.

## 2026-06-01 (5)

### [Phase 3C-1][피드백] 얼음 범위 축소 — 조준(SEEKING) 중엔 이동/중력 유지, 잡았을 때(ACTIVE)만 정지

- **수정 파일**: src/player.py
- **피드백(비니)**: "왜 홀드할 때 이동 못 하지?" → 잡았을 때만 멈추게(조준만 할 땐 움직이게).
- **원인**: 직전 (3) 재설계에서 `_update_grabbing`이 Z홀드 동안 무조건 `vx/vy=0`(얼음) → 대상 없는 조준 중에도 정지·중력무시였음.
- **변경 내용**: `_update_grabbing` 분기 변경 — GRAB_ACTIVE(대상 잡은 상태)에서만 얼음+anchor, 그 외(SEEKING 조준)는 `_normal_movement`로 걷기/점프/대시·중력 그대로 유지하며 탐색. 대상 확보 시 `_start_grab_active`에서 속도 정리 후 얼음 진입.
- **검증(자동)**: SEEKING 중 우이동 됨·중력 적용 / 근처면 즉시 ACTIVE·얼음(vy0)·이동불가 / 조준 중 점프 가능 / 릴리즈 월바운스 정상 — **8/8 PASS**. 씬 180f(조준 중 이동) 예외0.
- **원본 파일 업데이트 여부**: 없음. PLANNING 갱신(승인 대기) 시 "ACTIVE만 정지, 조준 중 이동 유지"로 기술.
- **다음 작업**: 비니 재플레이 — 조준 중 자유 이동·잡으면 정지 확인. 승인 시 PLANNING 반영 + Phase 3C-2(적).

## 2026-06-01 (4)

### [Phase 3C-1][피드백] 잡기 범위↑ + 가시 레이캐스트 차단 + 최근접 일직선 표시

- **수정 파일**: settings.py, src/player.py, src/scene.py
- **피드백(비니)**: 1) 레이캐스트(잡기) 범위 조금 늘리기. 2) NTT와 플레이어 사이에 가시가 있으면 잡기 안 돼야 함. 3) 원형 범위와 별개로, 잡을 수 있을 때 플레이어↔가장 가까운 NTT를 일직선으로 표시. 4) 여러 NTT 범위 내면 가장 가까운 것만.
- **변경 내용**:
  - settings: `GRAB_RANGE 80→110`.
  - player.py: `update(inp, solids, grabbables, hazards=None)` — 가시 rect 인자 추가(기본 [] 호환). `_update_grabbing`에서 `blockers = solids + hazards`로 레이캐스트 → 벽뿐 아니라 **가시도 시야 차단**(가시 사이면 잡기 불가). `_has_los`/`_find_grab_target` 파라미터를 blockers로 일반화(임의 rect 차단). 최근접 선택은 기존 로직 그대로(거리순 정렬 → LOS 통과 첫 대상 1개). draw: `_draw_grab_aim`에 **잡기 가능 시 플레이어 중심↔대상 중심 일직선**(초록) 추가, 대상은 가장 가까운 1개만(기존).
  - scene.py: player.update에 `[hz.rect for hz in tilemap.hazards]` 전달.
- **검증(자동)**: 범위↑(95px 잡힘) / 가시 직선 위 → ACTIVE 안됨·grab_ok=False / 가시 비켜있으면 정상 잡기 / 여러 NTT 중 최근접 선택 / 일직선 draw 예외0 / hazards 생략 호환 — **7/7 PASS**. 씬 180f 예외0. map_preview_3c1.png 최근접 바닥 NTT에 초록 일직선·공중 NTT 미선택 육안 확인.
- **원본 파일 업데이트 여부**: 없음(코드/튜닝). 직전 (3) 항목의 **PLANNING 갱신(승인 대기)** 여전히 유효 — 신설계 일괄 반영 시 가시 차단·일직선도 함께 기술.
- **다음 작업**: 비니 직접 플레이 — 범위 체감·가시 사이 잡기 차단·최근접 일직선 표시 확인. 승인 시 PLANNING 갱신 + Phase 3C-2(적).

## 2026-06-01 (3)

### [Phase 3C-1][피드백] 잡기 재설계 — Z홀드 즉시 순간이동·얼음 + 버그2 수정 + 아래=대시

- **수정 파일**: src/player.py, src/scene.py, src/tilemap.py
- **피드백(비니)**: 1) Z홀드 시 (READY/재입력 없이) 즉시 순간이동, 대상 없으면 범위만 표시. 2) 홀드 동안 이동 불가 + 중력 무시(얼음). 3) 버그: 엔티티 바닥에 있을 때 릴리즈 시 사망. 4) 버그: 엔티티가 점프패드와 겹치면 무한 잡기. 5) 아래/아래+좌우 릴리즈 → (하이퍼 대신) 실제 대시 발동.
- **변경 내용**:
  - player.py: GRAB_READY 흐름 폐기. `_update_grabbing` 통합 — Z홀드 동안 `vx_input/vx_external/vy=0`(얼음·중력무시), 대상 확보 시 `_start_grab_active` 즉시 순간이동, 없으면 SEEKING(범위표시 제자리). `_end_grab`(Z뗌/시간초과 → 잡았으면 릴리즈, 아니면 NORMAL). `_anchor_to(target, solids)` — 대상 중심 정렬 후 **솔리드 겹치면 발을 솔리드 위로 스냅**(바닥 박힘 사망 방지=버그3). `_release_grab` 방향분기 변경: 아래(±좌우)면 `_start_dash(inp)`로 DASH 진입(=버그5/하이퍼 제거), 위=월바운스·좌우=슈퍼·무입력=점프 유지. draw 오버레이 조건 READY→ACTIVE.
  - scene.py: `_check_triggers`에서 잡기(SEEKING/ACTIVE) 중 **점프패드/스프링 트리거 스킵**(NTT 겹침 무한 잡기 방지=버그4). 위험/체크포인트는 유지.
  - tilemap.py: 바닥 위에 올라탄 NTT 1개 추가(바닥 케이스 검수용) — 총 2개(공중·바닥).
- **검증(자동)**: 단위 12/12(Z홀드 즉시 ACTIVE·순간이동 / 대상없음 SEEKING 얼음·이동불가·vy0 / 위=월바운스·좌우=슈퍼·아래=대시(0,1)·아래우=대각대시(1,1)·무입력=점프 / 바닥박힘 안전텔레포트) + 씬 6/6(바닥 엔티티 잡기·잡는중 바닥안겹침·릴리즈 사망없음·아래대시 릴리즈 사망없음 / 패드겹침 무한잡기없음·바운스없음 / 잡기스팸 180f 예외0·draw 예외0) — **18/18 PASS**. map_preview_3c1.png 바닥 NTT 잡기 육안 확인.
- **원본 파일 업데이트 여부**: 없음(코드 한정). **PLANNING 갱신 필요(승인 대기)**: PLANNING.md '잡기 시스템'(498~535행)이 구 흐름(Z뗌→READY→재입력, 릴리즈 하이퍼)을 서술 → 신설계와 불일치. 규칙#8(추가만/위치명시·승인)에 따라 '잡기 시스템' 섹션 뒤에 `### [3C-1 재설계] Z홀드 즉시 순간이동·얼음·아래=대시` 서브섹션 추가 제안 — 비니 승인 후 반영.
- **다음 작업**: 비니 직접 플레이 — Z홀드 즉시 잡힘·홀드 중 멈춤(공중 정지)·바닥 NTT 사망 없음·패드 겹침 정상·아래 릴리즈 대시 확인. 승인 시 PLANNING 갱신 반영 + Phase 3C-2(적).

## 2026-06-01 (2)

### [Phase 3C-1] 잡기 시스템 — 대상 NTT + 풀 루프(탐색→READY→ACTIVE→릴리즈)

- **수정/생성 파일**: settings.py, src/entities/ntt.py(신규), src/player.py, src/scene.py, src/tilemap.py, main.py (한 파일씩 순차)
- **승인**: Phase 3C 분할안 + 3C-1 구조 평가 보고서 승인(비니 "승인 — 풀 루프"). ntt.py 신규 생성 허용.
- **변경 내용**:
  - settings: NTT_WIDTH/HEIGHT(14) + 색 3개(COLOR_NTT, COLOR_GRAB_OK 초록, COLOR_GRAB_NO 빨강) 추가.
  - `ntt.py`: `NTT(Actor)` GRABABLE 레이어, 고정형(중력없음). `on_grab`(잡힘=정지·플레이어가 anchor), `on_release(push_x,push_y)`(발사 반대로 밀쳐짐+복귀 타이머), update=밀쳐짐(공기저항 감속·벽막힘 정지)→PUSH_RETURN_TIME 후 origin 복귀.
  - `player.py`: PlayerInput에 grab_pressed/grab_held 추가. 잡기 필드(grab_target/ok/timer/ready_timer). update 디스패치에 GRAB_SEEKING/READY/ACTIVE 추가. `_update_normal`에서 Z홀드 시 SEEKING 진입 + `_normal_movement`(이동 본체) 분리(SEEKING/READY가 일반 이동 유지하며 조준). 신규: `_update_grab_seeking/ready/active`, `_start_grab_active`(순간이동), `_release_grab`(방향키→점프/슈퍼/하이퍼/월바운스, 대시 소모X·충전O, 대상 밀쳐냄), `_find_grab_target`(원형 GRAB_RANGE 최근접), `_has_los`(Rect.clipline 레이캐스트, WALL만 막힘), `_in_range`. draw에 조준 범위원+대상 하이라이트(초록/빨강) `_draw_grab_aim`.
  - `scene.py`: player.update에 ntts 전달, NTT update 루프, draw에 NTT 렌더, respawn 시 잡힌 NTT 해제.
  - `tilemap.py`: ntts 리스트 + NTT 1개 코드배치(256,416 — 스폰 우측 바닥 위 공중, 시야 트임).
  - `main.py`: Z키(잡기) 엣지/홀드 수집 + 매프레임 리셋, HUD에 grab 상태/조작 안내.
- **3C-1 범위(제외=후속)**: DASH 중 Z홀드→SEEKING(3C-4 DASH_STRIKE), 적 HP/리스폰(3C-2), RopeNTT 진자(3C-3), 이동형 NTT. 기본 NTT는 고정이라 ACTIVE 중 플레이어 anchor(드리프트 없음) — 스펙 vx=vx_input+ntt.vx는 이동형 NTT에서 의미, 고정형은 anchor로 단순화.
- **검증(자동)**: 풀루프(SEEKING→탐색초록→READY→ACTIVE순간이동·NTT잡힘→타이머감소→릴리즈) / 릴리즈 방향별(위=월바운스·우=슈퍼·아래우=하이퍼·무입력=일반점프, 전부 대시충전) / NTT 밀쳐짐(down) / LOS막힘→빨강→READY불가 / 범위밖→대상None / READY 타임아웃→NORMAL / 회귀 일반점프 — **20/20 PASS**. 씬 통합 180f 무입력·잡기스팸 예외0 + 조준 오버레이 draw 예외0. map_preview_3c1.png 배치 육안 확인.
- **원본 파일 업데이트 여부**: 없음 (잡기 시스템·NTT·GRAB 상태 모두 CLAUDE.md/PLANNING에 이미 명세. settings 상수/색은 신규 추가, ACTIVE anchor 단순화는 3C-1 기본형 한정).
- **다음 작업**: 비니 직접 플레이 — `python main.py`, 스폰 우측 NTT에 접근→Z홀드(범위 초록 확인)→뗌→Z재입력(순간이동)→방향키 릴리즈(위 월바운스/좌우 슈퍼/아래좌우 하이퍼/무입력 점프) 손맛 확인. 승인 시 Phase 3C-2(적 Enemy/ArmoredEnemy).

### [Phase 3B][피드백] 벽 스프링 손맛 — 입력잠금 짧게 + 튕기는 높이 ↑

- **수정 파일**: settings.py, src/entities/spring.py
- **승인**: 3B 재플레이 통과(대시 테크 버퍼창·하강 발판·패드 폭점프 전부 OK). 남은 3B = 스프링 손맛 튜닝.
- **피드백(비니)**: 좌우 벽 스프링만 — 입력잠금 짧게, 튕기는 높이 올리기.
- **변경 내용**:
  - settings: `SPRING_FORCE_TIME 10→5`(발사 직후 수평 제어 빨리 복구). 신규 `SPRING_WALL_V=-14.5`(벽 스프링 전용 수직 — 기존 `SPRING_LAUNCH_V=-12.5`는 **위 스프링 전용**으로 분리, 위 스프링 손맛 안 건드림).
  - spring.py: 좌/우 분기 vy를 `SPRING_LAUNCH_V`→`SPRING_WALL_V`로 교체. 위 스프링은 그대로 `SPRING_LAUNCH_V`.
- **검증(자동)**: 위(vy-12.5·잠금0·대시충전) / 우(vx10·vy-14.5·잠금5·ldir+1·충전) / 좌(vx-10·vy-14.5·잠금5·ldir-1) / 잠금 7f 후 해제(반대입력 무시 종료) / 풀루프 180f 예외0(스프링 up+right 로드) — **전부 PASS**.
- **원본 파일 업데이트 여부**: 없음 (스프링 튜닝값 수정 + 상수 1개 추가. SPRING_WALL_V는 신규 상수, 기존 SPRING_LAUNCH_V 의미만 '공통→위 전용'으로 주석 명확화).
- **다음 작업**: 비니 재플레이 — 좌우 벽 스프링 1) 발사 직후 수평 제어 빨리 돌아오는지, 2) 더 높이 튕기는지 손맛 확인. 확정 시 Phase 3C(잡기 대상 NTT) 착수.

## 2026-05-31 (12)

### [Phase 3B][피드백] 대시 테크 버퍼창 실수정 + 캐리 끼임 샌드위치 모델 + 패드 버퍼해제

- **수정 파일**: settings.py, src/player.py, src/scene.py
- **피드백(비니)**: 1) 월바운스·슈퍼·하이퍼 버퍼 0.15초 — 대시 후 벽에서만 되는 게 비친화적. 2) 움직이는 발판에서 내려갈 때 지형과 겹치면 죽는 오판정. (이전 3B: 패드+슈퍼 폭점프 버그 → 패드 닿으면 버퍼 해제 제안)
- **승인**: 끼임 모델 = 샌드위치만 사망(아래 착지·옆 벽슬라이드는 탈출=생존, 천장/진짜 끼임만 사망). 밀기/비탑승 끼임은 기존 유지.
- **A. 대시 테크 버퍼창 (실제 원인 발견)**:
  - 원인: `dash_jump_buffer`가 대시 중 매 프레임 set→tick이라 **대시 종료 시점에 0** → 대시 후 창이 사실상 없었음(테크가 대시 중에만 발동 = "벽에서만" 체감). 맵-밖 클린 검증으로 djb_after=0 확인.
  - 수정: `_end_dash`에서 `dash_jump_buffer.set()` 추가 → 대시 종료 직후 AUTO_JUMP_BUFFER 프레임간 창 보장. `AUTO_JUMP_BUFFER 6→9`(0.15s).
  - 검증(맵-밖 Player 단독): 대시 종료 후 djb=9 유지 → 무입력 점프에 **tech='SUPER' 발동**(이전 ''=일반점프). 
- **B. 캐리 끼임 샌드위치 모델 (scene.py)**:
  - `_carry_and_push` 탑승 분기 교체: 캐리 후 다른 솔리드와 겹치면 `_resolve_carry`로 **발판 진행 반대 방향으로 밀어내 탈출** 시도. 탈출 후에도 발판 본체와 겹치면(=발판↔솔리드 샌드위치) 사망. 신규 헬퍼 `_resolve_carry`, `_overlapping_solid`.
  - 결과: 하강 발판→바닥 착지=생존, 상승 발판→천장=사망, 옆 벽=탈출 생존. 밀기(비탑승)→벽 끼임은 기존 경로 그대로 사망.
- **패드/스프링 버퍼 해제 (player.py launch)**: `launch()`에 `dash_jump_buffer.consume()` + `last_dash_dir=(0,0)` 추가 → 점프패드/스프링 닿을 때 대시 점프창 해제 → 패드+슈퍼/하이퍼 속도합산 폭점프 방지.
- **검증(자동)**: 맵-밖 버퍼창(djb9·슈퍼발동) / 캐리 B1하강생존·B2천장사망·B3밀기회귀사망·B4실제맵 수직발판 400f 거짓끼임0 / 패드 발사 시 버퍼0 / 풀루프 180f 예외0 — 핵심 전부 PASS. (맵-내 슈퍼 테스트는 대시방향이 가시로 향해 죽던 셋업오류 → 좌대시로 교정.)
- **원본 파일 업데이트 여부**: 없음 (버퍼창 수정·끼임 모델·launch 보강 모두 기존 메카닉 버그수정/튜닝 범위. AUTO_JUMP_BUFFER는 값 변경).
- **다음 작업**: 비니 재플레이 — 1) 대시 후(벽 아니어도) 슈퍼/하이퍼/월바운스 0.15초 창 발동 확인, 2) 하강 발판 착지 시 안 죽는지, 3) 패드+슈퍼 폭점프 사라졌는지. **남은 3B**: 스프링 "밀어내는 느낌" 손맛 튜닝(SPRING_SPEED/입력잠금). 확정 후 Phase 3C(잡기 대상 NTT).

## 2026-05-31 (11)

### [Phase 3B] 점프패드 + 스프링 (Trigger 발사 오브젝트)

- **수정/생성 파일**: src/player.py(launch 헬퍼 추가), settings.py(색상 2개 추가), src/entities/jump_pad.py(신규), src/entities/spring.py(신규), src/tilemap.py(기호 'J'·스프링 코드배치·리스트·draw), src/scene.py(트리거 루프)
- **승인**: Phase 3A 검수 통과(비니 직접 플레이 — 가시/체크포인트 정상). 3B 구조 평가 보고서 승인(launch 헬퍼 경유, 신규 파일 2개).
- **변경 내용**:
  - `player.py`: `launch(vx_external=None, vy=None, refill_dash=True)` 신규 — 외력 발사 규칙을 한 곳에 캡슐화. state=NORMAL 강제(대시 중이면 종료), dash_timer/ceiling_stick/hang_timer 리셋, 지정 축만 덮어쓰기(vx None이면 관성 유지), 대시 충전.
  - `jump_pad.py`: `JumpPad(Trigger)` target=[PLAYER]. on_enter → launch(vx=None 유지, vy=JUMP_PAD_SPEED, 충전).
  - `spring.py`: `Spring(Trigger)` direction='up'/'left'/'right'. up=수직(-SPRING_SPEED), 좌우 벽스프링=대각(수평±SPRING_SPEED + 위 -SPRING_SPEED), 충전. (벽스프링 수직성분은 SPRING_SPEED 재사용 — 추후 튜닝 가능)
  - `tilemap.py`: 텍스트 기호 'J'(점프패드) 파싱 추가. `_build_objects`로 스프링 코드배치(우측 선반 위 up 1개 + 좌측 벽 부착 right 1개). jump_pads/springs 리스트 노출, draw에 렌더 추가. 3B 맵에 점프패드 스폰 근처(col9) 배치.
  - `scene.py`: `_check_triggers`에 jump_pads/springs 루프 추가(hazard/checkpoint와 동일 패턴, 플레이어 이동 후 발동).
  - `settings.py`: COLOR_JUMP_PAD, COLOR_SPRING 추가(디버그 렌더용).
- **검증(자동, _qa_3b.py 임시)**: 점프패드(vy=JUMP_PAD_SPEED·vx_external 유지·대시충전·state) / 대시 중 닿음→대시 종료 / 스프링 up·left·right 발사+충전 / launch refill_dash=False / 맵 로드(패드1·스프링2) / 풀루프 180f 예외0 / 통합 점프패드 접촉 발사+충전 — **17/17 PASS**. map_preview.png 배치 육안 확인. 임시 QA파일 삭제.
- **원본 파일 업데이트 여부**: 없음 (JumpPad/Spring 모두 CLAUDE.md/PLANNING 구조에 이미 명시. launch 헬퍼·색상·기호 'J'는 신규 — 문서화 필요 시 위치 명시 후 승인).
- **다음 작업**: 비니 직접 플레이 — 점프패드 위 발사+대시충전, 스프링(위/오른쪽) 발사 손맛 확인 → 승인 시 Phase 3C(잡기 시스템 또는 적/NTT).

## 2026-05-31 (10)

### [Phase 3A] 기반 아키텍처(Layer/Trigger/Scene) + 위험요소/체크포인트 + 텍스트맵 로더

- **수정/생성 파일**: src/layer.py(신규), src/entities/trigger.py(신규), src/entities/hazard.py(신규), src/scene.py(신규), src/tilemap.py(텍스트 로더로 재작성), main.py(Scene 위임으로 축소), settings.py(색상 3개 추가)
- **승인**: Phase 3 분할안 + 3A 구조 평가 보고서 승인. 타일맵 = 간단 텍스트맵 로더 선택.
- **변경 내용**:
  - `layer.py`: `Layer` enum(WALL/HAZARD/GRABABLE/PLAYER) + `interaction(src,dst)` 충돌규칙 테이블(block/death/pass/ignore).
  - `trigger.py`: `Trigger(Entity)` — `target_layers` 필터, `try_trigger(actor, layer, scene)` 범위 겹침 시 `on_enter` 호출.
  - `hazard.py`: `Hazard(Trigger)` — target=[PLAYER], on_enter → `scene.kill()`.
  - `tilemap.py`: 제너릭 `load_text` 로더('#' solid 연속 병합 / '^' Hazard / 'C' 체크포인트 좌표 / 'P' 스폰). 3A 테스트맵을 헬퍼로 그리드 생성(낙사 구덩이·가시·선반·체크포인트). 움직이는 발판은 코드 배치 유지. 순환참조 피해 체크포인트는 좌표(Rect)만 넘김.
  - `scene.py`: `Scene` — 타일맵/플레이어/체크포인트 보유, 업데이트 순서(발판→캐리/밀기/끼임→플레이어→트리거→사망/리스폰) 조율. main에 있던 `_riding_platform/_carry_and_push/_push_player/_pinned` 전부 이관. `Checkpoint(Trigger)`(닿으면 리스폰 지점 갱신, 활성색 변경). `kill/set_checkpoint/respawn`.
  - `main.py`: Scene에 위임하는 얇은 진입점으로 축소(입력 수집·HUD만). R=전체 리셋.
- **검증(자동)**: 맵 로드(solid68/가시8/체크포인트2/발판2) / 스폰 착지 / 가시 닿으면 사망→리스폰 / 체크포인트 활성화→낙사 후 그 지점 부활 / 발판 끼임 사망 회귀 / 풀루프 180f 예외0 — **7/7 PASS**. map_preview.png 레이아웃 육안 확인.
- **원본 파일 업데이트 여부**: 없음 (Layer/Trigger/Scene/Hazard/Checkpoint 모두 CLAUDE.md/PLANNING 구조에 이미 명시. 텍스트맵 기호 포맷은 신규 — 문서화 필요 시 위치 명시 후 승인).
- **다음 작업**: 비니 직접 플레이 — 가시/낙사 사망, 체크포인트 부활, 발판 회귀 확인 → 승인 시 3B(점프패드·스프링).

## 2026-05-31 (9)

### [Phase 2.5][피드백] 슈퍼/하이퍼 정점 높이를 일반 점프와 동일하게 (하강 부유로 구분 유지)

- **수정 파일**: settings.py, src/player.py
- **피드백(비니)**: 슈퍼/하이퍼 점프 높이를 그냥 일반 점프 높이로.
- **원인**: 기존 체공이 **상승 중에도** 중력을 감소시켜 정점이 일반보다 높았음(슈퍼). 하이퍼는 발사속도 자체가 낮았음(×0.7).
- **변경 내용**:
  - settings: `DUCK_SUPER_JUMP_Y_MULT 0.7→1.0`(하이퍼 발사=일반 점프). `SUPER_HANG_GRAV 0.5→0.4`(하강 부유 강화).
  - player.py: hang에 `descend_only` 옵션 추가 — True면 **하강(vy>=0)에만** 중력 감소(상승은 일반 중력 → 정점=일반 점프 높이). `_apply_vertical`이 실제 적용 프레임에만 hang_timer 소진(상승 동안 안 닳아서 하강 부유가 온전히 유지). 슈퍼/하이퍼는 `descend_only=True`, 월바운스는 기본 False(상승 포함, 높이 유지).
- **측정**: 정점 — 일반44px·슈퍼50px·하이퍼50px(±6px, 트리거 프레임 중력스킵 탓·미미). 체공 — 일반27f·슈퍼34f·하이퍼34f(하강 부유로 +7f, 구분 유지). 월바운스 95px 회귀 유지.
- **검증(자동)**: 슈퍼/하이퍼 정점~일반(±8) / 체공>일반(+7) / 월바운스 높이 유지 / 풀루프 150f 예외0 — **6/6 PASS**.
- **원본 파일 업데이트 여부**: 없음 (hang descend_only 옵션 추가, settings 튜닝).
- **다음 작업**: 비니 재플레이 — 슈퍼/하이퍼가 일반 점프 높이로 뜨되 하강에서 살짝 부유해 구분되는지 확인 → 확정 후 Phase 3.

## 2026-05-31 (8)

### [Phase 2.5][피드백] 슈퍼/하이퍼에 체공(부유) 추가 — 일반 점프와 구분

- **수정 파일**: settings.py, src/player.py
- **피드백(비니)**: 슈퍼·하이퍼가 체공시간이 없어 일반 점프와 구분이 안 됐던 것이 원인. (월바운스만 체공이 있어 구분됐음)
- **변경 내용**:
  - player.py: `wall_bounce_hang`(월바운스 전용)을 **범용 hang**(`hang_timer` + `hang_grav`)으로 일반화. `_set_hang(time, grav)` 헬퍼 신설. `_gravity_value`가 hang 중 `self.hang_grav` 적용, `_apply_vertical`가 hang_timer 감소, 착지 시 리셋. 슈퍼/하이퍼/월바운스가 각자 `_set_hang` 호출.
  - settings.py: 추가 `SUPER_HANG_TIME=18`/`SUPER_HANG_GRAV=0.5`, `HYPER_HANG_TIME=22`/`HYPER_HANG_GRAV=0.4`.
- **튜닝 측정(체공 프레임)**: 일반점프 27f / 슈퍼 40f / 하이퍼 36f / 월바운스 41f → 4종 테크 모두 일반 점프보다 확실히 길어 손맛으로 구분. 하이퍼는 낮은 아크 유지하며 멀리(우 입력 시 ~181px).
- **검증(자동)**: 슈퍼 체공>일반 / 하이퍼 체공>일반 / 슈퍼 vx9.5 유지 / 월바운스 체공 41f 유지 / 풀루프 150f 예외0 — **5/5 PASS**.
- **원본 파일 업데이트 여부**: 없음 (hang 메커니즘은 기존 월바운스 체공을 일반화, settings 상수 추가).
- **다음 작업**: 비니 재플레이 — 슈퍼/하이퍼 체공으로 구분되는지 확인 → 확정 후 Phase 3.

## 2026-05-31 (7)

### [Phase 2.5][튜닝] 월바운스 하향 + 슈퍼/하이퍼 순간 힘 상향 + 하이퍼 점프 상향

- **수정 파일**: settings.py (튜닝값만)
- **피드백(비니)**: 1) 월바운스 높이/체공 너무 큼. 2) 슈퍼·하이퍼 순간 힘 조금 더. 3) 하이퍼 점프 높이 너무 낮음.
- **변경 내용**:
  - 월바운스 하향: `SUPER_WALL_JUMP_SPEED -8.0→-7.0`, `WALL_BOUNCE_HANG_TIME 30→20`, `WALL_BOUNCE_HANG_GRAV 0.5→0.6`. → 최고 150px/55f에서 **94px/41f**로 (월점프 45px/26f의 2.1배 — 여전히 구분).
  - 슈퍼/하이퍼 순간 힘: `SUPER_JUMP_H 8.5→9.5`. → 슈퍼 수평 9.5, 하이퍼 수평 9.5×1.25=11.88.
  - 하이퍼 점프 상향: `DUCK_SUPER_JUMP_Y_MULT 0.5→0.7`. → vy -3.25→**-4.55**.
- **검증(자동)**: 슈퍼 vx9.5 / 하이퍼 vx11.88 / 하이퍼 vy-4.55 / 월바운스 94px·41f / 월바운스>월점프 구분 — **6/6 PASS**.
- **원본 파일 업데이트 여부**: 없음 (settings 튜닝값 수정만).
- **다음 작업**: 비니 재플레이 손맛 확인 → 추가 튜닝 또는 확정 → Phase 3.

## 2026-05-31 (6)

### [Phase 2.5][피드백] 월바운스 버퍼/체공·월점프 구분 + 슈퍼/하이퍼 HUD 피드백

- **수정 파일**: settings.py, src/player.py, main.py (순차)
- **피드백(비니)**: 1) 월바운스 버퍼 필요(벽에 안 딱 붙어도 발동), 월점프와 구분, 월바운스가 월점프보다 높고 체공 +0.5초. 2) 슈퍼/하이퍼 발동되는지 안 보임.
- **변경 내용**:
  - settings.py: `SUPER_WALL_JUMP_SPEED -5.5→-8.0`(월점프 -6.5보다 높게 — 기존엔 거꾸로 더 낮았음). 추가: `WALL_BOUNCE_RANGE=16`(벽 감지 버퍼), `WALL_BOUNCE_HANG_TIME=30`(체공 0.5초), `WALL_BOUNCE_HANG_GRAV=0.5`(체공 중 중력 배율).
  - player.py: `_probe_wall_near`(WALL_BOUNCE_RANGE 범위 좌우 zone Rect로 벽 근접 감지) → `wall_near_dir`. `_try_dash_tech` 월바운스 조건을 `on_wall`→`wall_near_dir!=0`(버퍼)로 교체, 슈퍼/하이퍼에 `near_ground`도 허용. `_apply_vertical`/`_gravity_value`에 `wall_bounce_hang>0` 동안 중력×HANG_GRAV(부유). `_do_wall_bounce`가 기본 월바운스 시 `wall_bounce_hang` 세팅. 착지 시 hang 리셋. 테크 발동 시 `_flash_tech`로 `last_tech`/`tech_flash`(0.5초) 기록.
  - main.py: HUD에 `tech_flash>0`이면 화면 중앙 상단에 "SUPER!/HYPER!/WALLBOUNCE!" 큰 글자 표시.
- **튜닝 측정**: 월점프=체공26f·최고45px. 월바운스(launch-8.0/hang0.5)=체공55f(+29≈0.5초)·최고150px(월점프 3.3배). 6개 조합 측정 후 선택(과한 -9.0/0.45=199px는 기각).
- **검증(자동)**: 월바운스 버퍼(벽 12px 떨어져도 발동) / 월바운스>월점프 높이 / 체공 +29f / 슈퍼·하이퍼 플래시 표시 / 회귀(슈퍼·월점프·끼임사망) — **전부 PASS**. main.py 풀루프 180f 예외0.
- **원본 파일 업데이트 여부**: 없음 (settings 튜닝값 수정 + 상수 3개 추가, 코드 보강).
- **다음 작업**: 비니 재플레이 — 월바운스 vs 월점프 체감 구분, HUD로 슈퍼/하이퍼 발동 확인. 손맛 튜닝(launch/hang) 피드백 → 확정 후 Phase 3.

## 2026-05-31 (5)

### [Phase 2.5] 고급 무브먼트 5종 — 슈퍼/하이퍼/코너부스트/월바운스(기본·대각)

- **수정 파일**: src/player.py (단일 파일)
- **승인**: 구조 평가 보고서 승인 — dash_jump_buffer(6f) 창, 월바운스는 위 성분 대시 중만, vx_external 재사용.
- **변경 내용**:
  - 필드: `dash_jump_buffer`(InputBuffer, AUTO_JUMP_BUFFER=6), `last_dash_dir`(대시 종료 후에도 유지).
  - `_start_dash`에 last_dash_dir 기록. `_update_dash`에 매 프레임 `dash_jump_buffer.set()` + 대시 중 점프 시 `_try_dash_tech` → NORMAL 전환. `_tick_buffers`에 dash_jump_buffer tick. `_handle_jump`에 테크 우선순위(대시창 활성 시 일반 점프보다 먼저).
  - 신규: `_try_dash_tech`(조건 분기), `_boost_sign`(코너 부스트=점프 시점 방향키 우선), `_do_super_jump`, `_do_hyper_jump`, `_do_wall_bounce`.
- **테크별 동작**:
  - 슈퍼: 수평 대시+지상 점프 → vx=SUPER_JUMP_H(8.5), vy=JUMP_SPEED(-6.5)
  - 하이퍼: 대각 아래 대시+지상 점프 → vx=SUPER_JUMP_H×1.25(10.6), vy=JUMP_SPEED×0.5(-3.25)
  - 코너 부스트: 슈퍼/하이퍼 수평 방향 = 점프 시점 방향키(없으면 대시 부호) — 대시 반대 입력도 적용
  - 월바운스 기본: 위 성분 대시 중 벽 + 점프 → vx=∓SUPER_WALL_JUMP_H(9.0, 벽반대), vy=SUPER_WALL_JUMP_SPEED(-5.5)
  - 월바운스 대각: 위 대시 중 벽 + 아래입력 → vx 동일, vy=WALL_BOUNCE_DIAG_Y(3.0, 수평 위주)
- **검증(자동)**: 슈퍼(vx8.5/vy-6.5) / 코너부스트(좌입력→vx-8.5) / 하이퍼(vx10.62/vy-3.25) / 월바운스기본(vx-9.0/vy-5.5) / 월바운스대각(vx-9.0/vy3.0) / 회귀(대시 일반종료 vx컷5.0·월점프·끼임사망) — **8/8 + 회귀 3/3 PASS**. main.py 풀루프 180f 예외0.
  - (월바운스 1차 실패는 테스트에서 플레이어가 벽과 4px 떨어져 on_wall 미감지 — 위 대시는 x 불변. 벽에 붙여(x112) 재검증 통과, 로직 정상.)
- **원본 파일 업데이트 여부**: 없음 (Phase 2.5는 CLAUDE.md에 이미 명세, 상수도 기존).
- **다음 작업**: 비니 직접 플레이로 5종 테크 손맛/타이밍(AUTO_JUMP_BUFFER 창) 검수 → 승인 시 Phase 2.5 종료 / Phase 3(레벨·퍼즐·잡기·적).

## 2026-05-31 (4)

### [Phase 2] 대시 구현 + 조작 변경 (C 점프 / X 대시)

- **수정 파일**: settings.py, src/player.py, main.py (순차, 한 파일씩)
- **승인**: 구조 평가 보고서 승인 — vx_external 재사용, 대각선 정규화, 엣지 트리거(누른 순간만).
- **변경 내용**:
  - settings.py: `MAX_DASHES=1` 추가 (Phase 2 섹션, 추가만).
  - player.py: PlayerInput에 `dash_pressed` 추가. Player에 대시 필드(`dashes/dash_timer/dash_dir/dash_vx/dash_vy`). `update`에 DASH 상태 분기 추가. NORMAL 첫머리에 대시 트리거(`_dash_triggered`) 체크 → 발동 시 그 프레임에 대시 시작. 신규: `_start_dash`(8방향 정규화·중력무시 진입), `_update_dash`(등속 이동·충돌·접촉·버퍼), `_dash_resolve_collisions`(막힌 축 속도0), `_end_dash`(방향별 종료처리). `_update_contacts` 착지 시 `dashes` 재충전.
  - main.py: 점프키 `SPACE/Z`→`C`, 대시키 `X`(엣지). `dash_pressed` 매 프레임 리셋. HUD에 `dashes/dash_t` + 조작 안내 갱신.
- **대시 스펙 구현**: 8방향(방향키+X, 방향없으면 미발동), 대시 중 중력 완전 무시, 1회 제한, 착지/지상 재충전. 종료 처리 — 수평: vx를 `END_DASH_SPEED`(5)로 컷 / 위: vy×`END_DASH_UP_MULT`(0.75) / 대각 아래: 속도 유지(하이퍼 트리거용, Phase 2.5).
- **검증(자동)**: 8방향 등속(정규화 8.0) / 방향없음 미발동 / 중력무시(수평대시 vy=0) / 공중 1회제한(충전0) / 착지 충전 / 수평종료 vx컷=5.0 / 위종료 vy=-6.0(×0.75) / 대각아래 종료 속도유지(5.66>5) / 회귀 NORMAL점프 3칸 — **9/9 PASS**. main.py 풀루프 120f 예외0 + 지상 대시 즉시 재충전 통합 확인.
- **원본 파일 업데이트 여부**: 없음 (대시는 CLAUDE.md Phase 2에 이미 명세됨, settings는 상수 추가만). 조작 변경(C/X)은 코드/HUD 반영, 문서 명세 항목 아님.
- **다음 작업**: 비니 직접 플레이로 대시 손맛(속도/거리/8방향/충전) 검수 → 승인 시 Phase 2 종료 / Phase 2.5(슈퍼·하이퍼·월바운스).

## 2026-05-31 (3)

### [Phase 1] 끼임 4방향 검증 + 신규 메카닉 문서화

- **수정 파일**: PLANNING.md, CLAUDE.md (문서만, 코드 변경 없음)
- **질문(비니)**: 옆으로/아래로 끼어 죽는 로직도 구현됐나?
- **답·검증**: `_push_player`가 dx/dy 4부호 전부 처리, `_pinned`이 반대편 솔리드 핀 감지. 4방향 합성 씬 자동검증:
  - 위(dy<0, 상승 발판→천장 머리 핀, 카레) 사망@172 / 아래(dy>0, 하강 발판→바닥 핀, 밀기) 사망@7 / 좌(dx<0, 좌측벽 핀) 사망@105 / 우(dx>0, 우측벽 핀) 사망@56 — **4/4 PASS**
  - (좌/위 1차 실패는 테스트 셋업 오류 — 바닥 없어 낙하·발판 사거리/프레임 부족. 셋업 교정 후 통과, 로직 정상)
- **문서화 (추가만, 위치 명시 후 반영)**:
  - PLANNING.md: "발판 관성 전달" 블록 뒤에 `### 발판 끼임 사망 (Crush Death)` 서브섹션 추가 (캐리/밀기 케이스·4방향·판정시점).
  - CLAUDE.md: Phase 1 "발판 관성 전달" 항목 뒤에 `[ ] 발판 끼임 사망 (Crush Death)` 체크리스트 항목 추가.
- **원본 파일 업데이트 여부**: PLANNING.md "발판 관성 전달" 섹션 뒤에 추가 / CLAUDE.md Phase 1 발판 항목 뒤에 추가 (둘 다 기존 내용 변경 없이 추가만).
- **다음 작업**: 비니 직접 플레이 검수 → Phase 1 종료 / Phase 2(대시).

## 2026-05-31 (2)

### [Phase 1] exampleMap 참고 맵 재구성 + 끼임(crush) 사망 메카닉

- **수정 파일**: src/player.py, src/tilemap.py, main.py (순차, 한 파일씩)
- **요청(비니)**: exampleMap.png 참고로 상하/좌우 움직이는 발판 테스트 맵 구성 + 움직이는 발판에 끼이면 사망.
- **구조 평가 보고서 승인**: 끼임 모델 = 옵션 B(셀레스트식 밀기+핀), 발판 = 좌우1·상하1.
- **변경 내용**:
  - player.py: `crushed` 플래그 추가(발판에 솔리드로 밀려 핀당했는지 = 사망 신호). 초기화는 main이 매 프레임 담당.
  - tilemap.py: `_build_test_map` 지형 재구성 — 좌하단 계단형(3단), 중앙 세로 기둥(끼임 대상), 우측-중앙 바닥, 우하단 떠있는 발판, 우측 알코브(상단 오버행 bottom=244 + 하단 선반, 상하 발판 끼임 대상), 좌측 천장 오버행. `_build_platforms` — 좌우 발판 `(360,300,90,14,'x',220,1.4)`(왼쪽 끝에서 기둥과 겹침), 상하 발판 `(730,250,80,14,'y',190,1.2)`(정점 y=250에서 오버행에 머리 핀).
  - main.py: update를 `_carry_and_push`로 재구성 — 탑승 발판은 dx/dy 캐리+관성, 비탑승 겹침 발판은 `_push_player`로 진행방향 밀어냄. 밀기/캐리 후 `_pinned`(exclude한 발판 제외 솔리드와 겹치면 핀)이면 `crushed=True`. update 끝에서 `crushed or 낙사` 시 `_respawn`.
- **끼임 판정 핵심**: 속도 비의존 — 발판 이동 직후(플레이어 자체 move 전)에 핀 검사 → 밀어낼 공간 있으면 push로 회피(비사망), 반대편 솔리드에 막히면 즉시 핀(사망). "발판↔벽 사이 핀"만 정확히 잡음.
- **검증(자동)**:
  - 끼임: T1 상하발판 정점→오버행 끼임 사망(respawn@150) / T2 오버행 제거 시 비사망 / T3 좌우발판 좌측끝 탑승→기둥 끼임 사망(respawn@147) / T4 스폰 후 120프레임 비사망 / T5 좌우발판 **중앙** 탑승 왕복은 밀기로 회피=비사망 — **5/5 PASS**
  - main.py 풀루프 300프레임 예외0, 스폰 착지 — SMOKE PASS
  - map_preview.png 렌더로 레이아웃 육안 확인 (exampleMap 지형 반영)
  - (디버그 중 py=440 점프는 crush→respawn(스폰60,440) 정상 동작이었음, 테스트 신호 오해였고 respawn 카운터로 재검증 통과)
- **원본 파일 업데이트 여부**: 없음. (끼임 사망은 CLAUDE.md Phase 목록에 없는 신규 메카닉 — 문서화 필요 시 위치 명시 후 승인받아 추가 예정)
- **다음 작업**: 비니 직접 플레이로 맵 손맛 + 끼임 사망 체감 확인 → 승인 시 Phase 1 종료 / Phase 2(대시).

### [Phase 1][기록보강+QA] 움직이는 발판 작업 로그 누락분 기록 + Phase 1 전체 QA

- **수정 파일**: 없음 (코드 변경 없음 — 로그/QA만)
- **상황**: 직전 세션에서 `src/entities/solid.py`, `src/entities/platform.py` 생성 + main.py 탑승/관성 연결이 완료됐으나 WORK_LOG 기록이 누락됨(파일 타임스탬프 12:17~12:19). 이번 세션에서 기록 보강 + 자동 QA 수행.
- **누락분 구현 요약**:
  - `Solid(Entity)` — 속도(vx/vy)·실제 이동 델타(dx/dy) 기록 베이스. 탑승 캐리/관성 전달용.
  - `MovingPlatform(Solid)` — 한 축(x/y) `distance` 왕복, `_bounce`로 끝에서 방향 전환, 실제 델타를 vx/vy로 기록.
  - main.py: `_riding_platform()`(발밑 1px 발판 탐색, 상승 중 제외) → 발판 이동 전 탑승 판정 → `map.update()` 후 rider.dx/dy로 플레이어 캐리 + `ride_vx/ride_vy` 세팅. 비탑승 시 0. `_do_jump`가 `ride_v*` × `PLATFORM_INERTIA_*` 관성 전달.
- **검증(자동)**:
  - T1 수평발판 캐리: 플레이어 x가 발판과 동일 이동(px_d=30.0=plat_d) / 탑승 중 on_ground=True — PASS
  - T2 수평 점프 관성: vx_external에 발판 속도(1.50) 전달, 점프 발동 — PASS
  - T3 수직 관성(재검증): 위 발판 점프 vy=-7.30 > base -6.10(더 높음), 아래 발판 -4.90 < base(더 낮음) — PASS
  - T4 비탑승 시 ride_vx/vy=0 — PASS
  - T5 회귀 일반점프 정점 ~3칸(49.6px) — PASS
  - main.py 풀루프 헤드리스 120프레임 예외0, 스폰 착지(y=484.8) — SMOKE PASS
  - **합계 7/7 PASS** (T3 1차 실패는 테스트 셋업 오류였고 재검증 통과, 로직 정상)
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: 아래 Phase 1 QA 보고서 제출 → 비니 직접 플레이 최종 검수 → 승인 시 Phase 2(대시) 착수.

### QA 보고서 — Phase 1

- **테스트 항목**: 좌우이동(지상/공중 Approach·외적초과 처리), 가변점프(GRAVITY_UP/RELEASE/DOWN), 코요테/점프버퍼, 월슬라이드, 월점프(셀레스트식 입력잠금·클라임), 천장밀착, 패스트폴, 웅크리기, 타일맵 충돌(바닥/벽/천장/절벽), 움직이는 발판 탑승·관성 — 누적 자동검증 다수 세션
- **통과**: 직전 세션들 8/8·10/10·9/9·6/6 + 이번 발판 7/7 = 전부 PASS
- **실패/보완 필요**: 0 (T3 셋업 오류 1건은 재검증 통과)
- **발견된 버그**: 이번 세션 신규 버그 없음 (직전 세션 월점프 버그 2건은 수정 완료)
- **보완 완료 여부**: ✅
- **최종 검수 요청**: 비니님 직접 플레이 후 승인 요청 — `python main.py` 실행, 테스트 맵에서 5개 항목(바닥/벽/천장/절벽/움직이는발판) 손맛 확인 부탁드립니다.

---

## 2026-05-30 10:45

### [Phase 1][버그수정] 한 번 누름=한 점프 / 벽쪽 수평속도 0

- **수정 파일**: src/player.py
- **피드백(비니)**: 1) 지상에서 벽 옆 점프 시 아직도 옆튐. 4) 벽 슬라이드→공중 전환 시 좌우 속도 유지/벽쪽 가속 누적되어 물리 이상.
- **원인**:
  - #1: 점프 누름이 `jump_buffer`+`wall_jump_buffer`를 둘 다 세팅. 지상점프가 `jump_buffer`만 소비 → 다음 프레임(상승 중 벽 접촉)에 남은 `wall_jump_buffer`로 월점프 자동 발동(옆튐). 한 번 누름이 일반점프+월점프 연속.
  - #4: 수평 충돌 시 속도를 0으로 안 만들어 벽쪽 vx_input이 계속 누적 → 벽 벗어날 때 튀어나옴.
- **수정**:
  - 지상점프 분기에서 `wall_jump_buffer.consume()` 추가(월점프 분기엔 `jump_buffer.consume()`) → 한 번 누름은 한 점프만.
  - `_resolve_landing`에 벽 충돌 시 벽쪽 수평속도 0 처리 추가(left→음수속도 0, right→양수속도 0, vx_input·vx_external 모두).
- **검증(자동)**: 지상점프 후 자동월점프 없음 / 벽쪽 vx_input=0(누적X)·벽뗀 후 잔여속도 없음 / 회귀(공중월점프·스킬클라임·더블점프방지·점프3칸·바닥근처일반점프) = 8/8 PASS.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: 비니 재플레이 확정 → 움직이는 발판(Phase 1 마지막).

---

## 2026-05-30 10:30

### [Phase 1][버그수정+재설계] 지상근처 일반점프 / 월점프 입력잠금 셀레스트 모델

- **수정 파일**: settings.py, src/player.py
- **피드백(비니)**: 1) 지상에서 벽 붙고 점프 시 월점프(옆튐)가 나감. 2) 월점프 후 벽쪽 키 계속 누르면 다시 붙음.
- **재현**: 월슬라이드로 바닥 3px 위에서 점프 → on_ground=False라 월점프 발동(=#1 원인).
- **변경 내용**:
  - settings: `NEAR_GROUND_DISTANCE=6` 추가. 안 쓰는 `WALL_JUMP_LOCK_TIME` 제거.
  - player: 신규 `near_ground`(하강 중 바닥 6px 이내 프로브) — 점프 시 일반점프 우선, 월점프는 `near_ground`면 차단 → 바닥 근처 옆튐 해결. 더블점프 방지 위해 vy>=0일 때만 near_ground 인정.
  - 월점프 입력 모델 전면 교체(기존 그립잠금 wj_locked 폐기): `wj_input_lock` — 월점프 후 **벽 쪽으로 '연속 홀드'한 입력만 정점(vy>=0)까지 무시** → 확실히 벽에서 떼짐. 단 **벽쪽 입력을 릴리즈하면 즉시 잠금 해제** → 재입력은 상승 중에도 허용 → 정점 근처 재그립 가능.
  - 결과: 연속홀드=떼짐(클라임 불가), 릴리즈+타이밍 재입력=클라임 상승(+48px/사이클). 셀레스트식 "기술적 무한 클라임" 모방.
- **검증(자동)**: #1 바닥근처 일반점프 / 지상+벽 일반점프 / 공중 월점프 발동 / 연속홀드 클라임X / 릴리즈+재입력 클라임O / 더블점프 방지 / 회귀(점프3칸·코요테·패스트폴·천장) = 10/10 PASS.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: 비니 재플레이 확정 → 움직이는 발판(Phase 1 마지막).

---

## 2026-05-30 10:05

### [Phase 1] 3차 피드백 — 점프 높이 상향, 월점프 수평 1.5배·높이 복구

- **수정 파일**: settings.py
- **피드백(비니 플레이)**: 1) 점프 너무 낮음(2.19칸). 2) 월점프 수평거리 1.5배. 3) 월점프 높이도 같이 낮아진 것 복구.
- **변경 내용**: `JUMP_SPEED -5.5→-6.5`(정점 측정 3.10칸/50px), `WALL_JUMP_H 4→6`(1.5배), `WALL_JUMP_V -6→-6.5`(점프와 동일 높이로 복구).
- **검증(자동)**: 점프 3.10칸 / 월점프 중립비행 수평 38.7px(H4 때 ~25px 대비 1.5배) / 월점프 높이=점프 동일 / 클라임 통제 유지(상승 45.6px·손실 5.6px) / 회귀(낙하상한·지상벽=일반점프) = 6/6 PASS.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: 비니 재플레이 손맛 확정 → 움직이는 발판.

---

## 2026-05-30 09:55

### [Phase 1] 2차 피드백 — 점프 2칸 / 월점프 방향요구 철회 / 셀레스트식 클라임

- **수정 파일**: settings.py, src/player.py
- **피드백(비니 플레이)**:
  1. 점프가 너무 높음 → 정점 2칸으로.
  2. 월점프 방향키 요구가 불편(키 없이가 나았음). 본 의도는 "지상에서 벽 붙고 점프 시 월점프 금지"였음(접지 jitter 수정으로 이미 해결).
  3. 월점프 후 벽쪽 키 유지 시 재부착되는데, 재그립이 정점보다 조금 낮아야 무한 클라임 방지. 셀레스트식 클라임 모방.
- **변경 내용**:
  - settings: `JUMP_SPEED -9→-5.5`(정점 측정으로 2.19칸=35px), `WALL_JUMP_V -9→-6`, `WALL_COYOTE_TIME 6→4`, `WALL_JUMP_BUFFER 6→4`. `WALL_JUMP_FORCE_TIME(입력잠금)` 폐기 → `WALL_JUMP_LOCK_TIME=20`(재부착 잠금) 신설.
  - player: 수평 입력잠금 제거(벽쪽 키로 재부착 허용). 월점프 방향 요구 철회(키 무관). 신규 재부착 잠금 = `wj_locked`: 월점프 후 벽 그립 차단, **최소 프레임 경과 + vy>0(하강 시작)** 둘 다 충족해야 해제 → 정점 지난 뒤 재그립 → 정점보다 낮게 잡힘. 착지 시 잠금 해제.
  - 튜닝 측정: 손실은 LOCK vs 정점도달(~15~18f)로 결정. LOCK=20 → 정점보다 9.5px 낮게 재그립, 순상승 34px/사이클(통제된 클라임). LOCK↑ = 손실↑.
- **검증(자동)**: 점프2칸/키없이월점프/지상=일반점프/클라임(재그립<정점·상승가능)/회귀(천장·낙하상한) = 7/7 PASS.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: 비니 재플레이로 클라임 손실량(LOCK_TIME) 손맛 튜닝 → 확정 후 움직이는 발판.

---

## 2026-05-30 09:40

### [Phase 1] 플레이 피드백 반영 — 속도/점프 하향, 월점프 조건·입력잠금

- **수정 파일**: settings.py (튜닝값 3 + 상수 1), src/player.py (월점프 로직)
- **피드백(비니 직접 플레이)**:
  1. 맵 대비 이동속도/점프 너무 높음.
  2. 월점프는 "벽 쪽 방향키 입력 + 공중 벽 접촉"일 때만. 지상에서 벽 붙고 점프는 일반 점프.
  3. 월점프 직후 수평 입력을 잠가야 벽에서 떼지는 느낌(안 그러면 벽쪽 키 힘이 관성 이김).
- **변경 내용**:
  - settings.py: `PLAYER_MAX_SPEED 8→4`, `JUMP_SPEED -11→-9`, `GROUND_ACCEL 2.0→1.2`/`DECEL 1.5→1.2`, `WALL_JUMP_H 6→4`/`V -10→-9`, 신규 `WALL_JUMP_FORCE_TIME=9`.
  - player.py: `_wall_jump_ready(inp)` 신설 — 벽 쪽 방향키 입력 + (on_wall 또는 wall_coyote)일 때만 월점프. 지상은 ground 분기가 먼저라 일반 점프. `_do_wall_jump`가 `wall_jump_force` 설정, `_update_horizontal`이 force 동안 수평 입력 무시(관성 유지).
- **검증(자동)**: 월점프 벽쪽입력O 발동 / 방향입력X 미발동 / 지상+벽 일반점프 / 입력잠금 후 벽반대로 이동(dx=-12.6) + 회귀(낙하상한/가변점프/천장밀착/웅크리기) = 전부 PASS.
- **원본 파일 업데이트 여부**: 없음 (settings 튜닝값 수정 + 상수 추가)
- **다음 작업**: 비니 재플레이로 손맛 재확인 → OK 시 움직이는 발판(Phase 1 마지막).

---

## 2026-05-30 09:26

### [Phase 1][버그수정] 접지 판정 rest jitter 해결 (프로브 방식)

- **수정 파일**: src/player.py
- **버그**: 자가 시나리오 테스트 중 발견. 정지 상태에서 `on_ground`이 매 프레임 True/False로 깜빡임. 웅크리기 발동 실패의 원인.
- **원인**: 플레이어가 정지 시 중력으로 0.8px만 가라앉는데, 충돌 Rect가 `int(y)`로 잘려 격프레임만 바닥과 겹침 → 접지 플래그 진동(고전 sub-pixel rest jitter).
- **수정**: 접지/벽 접촉 판정을 충돌 플래그 의존에서 **1px 프로브**로 분리. `_read_state`→`_pre_state`(슬라이드/패스트폴만), 신규 `_update_contacts(solids)`가 프레임 끝에서 rect를 ±1px 밀어 `collidelist`로 접촉 판정 + 코요테 충전. 서브픽셀에 안 흔들림.
- **검증(자동)**: 정지 10프레임 on_ground 전부 True(깜빡임 제거). 실제맵 시나리오 7종(좌우이동/코요테/천장밀착/월슬라이드/월점프/발판착지/웅크리기+패스트폴) + 회귀 2종 = **9/9 PASS**.
- **학습 포인트**: 정수 Rect 충돌은 서브픽셀 정지에서 접촉이 진동함 → 접지 판정은 속도/충돌결과가 아니라 별도 프로브로 하는 게 견고. (DaFluffyPotato/Celeste도 유사)
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: 손맛/튜닝 항목 비니님 직접 플레이 검수 → 이후 움직이는 발판.

---

## 2026-05-30 09:18

### [Phase 1] src/tilemap.py 생성 + main.py 연결 — 플레이 가능 상태

- **수정 파일**: src/tilemap.py (신규), main.py (입력/맵/플레이어 연결로 확장)
- **변경 내용**:
  - `TileMap` — Phase 1 테스트 맵 하드코딩. solid 8개: 좌우 경계벽, 바닥(절벽 gap 360~560 분할), 천장 오버행(y=330), 프리스탠딩 벽(x=640 기둥), 공중 발판 2개. `spawn=(60,440)`, `draw()`.
  - `main.py` — 키 입력(A/D/방향키 이동, SPACE/Z 점프, S 웅크림/패스트폴, R 리셋, ESC 종료) → `PlayerInput` → `Player.update(solids)`. 점프 엣지는 KEYDOWN으로 감지. 낙사 시 자동 리스폰. 디버그 HUD(상태/속도/접지) 출력.
- **검증(자동)**: 헤드리스(SDL dummy) 60프레임 구동 → 런타임 예외 0, 스폰에서 바닥 착지(y=484.8, on_ground=True) 확인. SMOKE PASS.
- **이유**: 비니님 직접 플레이 검수(수동 QA) 가능하게 만들기 위함. 정적 맵으로 바닥/벽/천장/절벽/발판 거동 한 화면에서 확인.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: src/entities/solid.py + src/entities/platform.py(MovingPlatform) — Phase 1 마지막 항목 '움직이는 발판 탑승 + 관성 전달'. (승인 필요) 그 후 Phase 1 전체 QA 보고서.

---

## 2026-05-30 09:10

### [Phase 1] src/player.py 생성 + 검증 9/9 통과

- **수정 파일**: src/player.py (신규), settings.py (상수 1개 추가), src/player.py (월슬라이드 cap 수정)
- **변경 내용**:
  - `src/player.py` — 섹션 주석 구획. `approach()` 유틸, `PlayerInput`(입력 스냅샷, 테스트용), `PlayerState`(enum 6종 전체 정의, NORMAL만 구현), `InputBuffer`(set/tick/is_active/consume), `Player(Actor)` NORMAL 로직.
  - NORMAL 처리 순서: `_read_state`→`_update_duck`→`_update_horizontal`→`_handle_jump`→`_apply_vertical`→`move`→`_resolve_landing`→`_tick_buffers`.
  - 구현: Approach 수평이동(지상/공중 AIR_MULT, 외적 초과 시 같은방향 추가가속 차단), 가변 점프(GRAVITY_UP/RELEASE/DOWN 분기), 코요테/점프버퍼, 월슬라이드, 월점프, 웅크리기(발밑 고정 높이변경), 패스트폴, 천장 밀착 프레임(vy 비례).
- **검증(자동)**: 인메모리 시뮬 9종 테스트 → 1차 7/9. 발견:
  - (테스트 버그) 천장 테스트 시작 위치가 천장에 안 닿음 → y=40으로 수정 후 통과.
  - (코드 갭) 월슬라이드가 진입 전 vy를 유지하고 가속만 늦춤 → 느린 하강 아님.
- **수정**: `settings.py`에 `WALL_SLIDE_MAX_FALL=3` 추가, 슬라이드 시 `min(vy+WALL_SLIDE_GRAVITY, WALL_SLIDE_MAX_FALL)`로 cap. 재검증 **9/9 PASS**.
- **이유**: 검증 절차로 디버깅 사전 예방(비니님 요청). 속도 3분할·축분리 충돌은 Actor 재사용.
- **원본 파일 업데이트 여부**: 없음 (settings.py는 상수 추가 — 신규 상수, 기존 내용 변경 없음)
- **다음 작업**: src/tilemap.py — 하드코딩 Phase 1 테스트 맵(바닥/벽/천장/절벽/발판) + solids rect 제공. 그 후 main.py에 씬 연결해 실제 플레이 검증. (승인 필요)

---

## 2026-05-30 09:01

### [Phase 1] src/entities/actor.py 생성 — 이동/충돌 공통 로직

- **수정 파일**: src/entities/actor.py (신규)
- **변경 내용**: `Actor(Entity)`. 속도 3분할(`vx_input`/`vx_external`/`vy`), `vx` 프로퍼티(=입력+외적). `move(solids)`=축분리 충돌(수평→수직), `_collide_axis`/`_resolve_horizontal`/`_resolve_vertical`로 분리(각 30줄 이하). `collisions` 상/하/좌/우 플래그.
- **검증**: 낙하착지 y=84·down=True, 우측 벽막힘 x=42·right=True 통과.
- **이유**: CLAUDE.md 속도 구조(입력/외적 분리) + 축분리 충돌 = Celeste/DaFluffyPotato 표준. 수평 먼저 해소해야 모서리 끼임 방지.
- **알려진 한계**: vx==0인데 외부(발판)에 밀려 겹치는 케이스 미처리 → 움직이는 발판 구현 시 보강 예정.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: src/player.py — PlayerState enum(6종 전체) + InputBuffer + NORMAL 로직(이동/점프/중력/월슬라이드/웅크리기/패스트폴). (승인 필요)

---

## 2026-05-30 08:58

### [Phase 1] src/entities/entity.py 생성 — 최상위 베이스 클래스

- **수정 파일**: src/entities/entity.py (신규, src/entities/ 폴더 신규)
- **변경 내용**: `Entity` 베이스 클래스. 위치 x/y(float 서브픽셀), 크기 width/height. `rect`(정수 Rect, 충돌용)·`center` 프로퍼티, `update`/`draw` 빈 메서드(하위 구현). namespace package(`__init__.py` 없음)로 `from src.entities.entity import Entity` 임포트 확인.
- **이유**: CLAUDE.md 아키텍처 `Entity→Actor→Player` 준수. 위치는 float로 들고 충돌만 int Rect로 변환 → 서브픽셀 물리 + 정확한 충돌 양립.
- **구조 평가 보고서**: Player 시스템 보고서 작성·승인 완료 (Actor 베이스 스펙대로 구현 결정).
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: src/entities/actor.py — Actor(Entity 상속): 속도 vx_input/vx_external/vy, 중력, 축분리 충돌 공통 로직. (승인 필요)

---

## 2026-05-30 08:55

### [Phase 1] main.py 생성 — pygame 창 + 메인 루프 뼈대

- **수정 파일**: main.py (신규 생성)
- **변경 내용**: `Game` 클래스(창/클럭/루프). `handle_events`(QUIT·ESC 종료) / `update`(빈 함수, 씬 연결 예정) / `draw`(배경 fill + flip) / `run`(고정 FPS 루프). py_compile 통과, pygame-ce 2.5.7 확인.
- **이유**: 플레이어 로직 붙이기 전 실행 가능한 최소 골격 먼저. 루프 4단계(이벤트→갱신→렌더→tick) 분리해 이후 씬/플레이어 삽입 지점 명확화.
- **원본 파일 업데이트 여부**: 없음
- **다음 작업**: src/player.py 생성 — PlayerState enum 전체 정의 + InputBuffer + NORMAL 로직. (src/ 폴더 신규 생성 포함, 승인 필요)

---

## 2026-05-30 08:53

### [Phase 1] settings.py 생성 — 전 페이즈 튜닝 상수 정의

- **수정 파일**: settings.py (신규 생성)
- **변경 내용**: CLAUDE.md 상수 스펙을 페이즈별 그룹으로 전부 정의 (Phase 1~3). 상수 69개. import 검증 통과(구문 에러 없음).
- **이유**: "매직 넘버 금지, 튜닝 값은 settings.py에 정의" 규칙 준수. 전 페이즈 값을 미리 박아두면 이후엔 추가가 아닌 튜닝(기존 값 수정)만 하면 됨.
- **스펙 외 [추가] 항목** (CLAUDE.md에 없어 보강, 검토 대상):
  - `JUMP_SPEED = -11` — 점프 시작 수직 속도 (스펙엔 동작만 명시, 값 없음)
  - `WALL_JUMP_H = 6`, `WALL_JUMP_V = -10` — 기본 월점프 속도
  - 디스플레이/루프 (`SCREEN_WIDTH/HEIGHT`, `FPS`, `TILE_SIZE`, `RENDER_SCALE`, `TITLE`)
  - 디버그 색상 (`COLOR_*`), `PLAYER_WIDTH`
- **원본 파일 업데이트 여부**: 없음 (settings.py는 신규 파일, PLANNING.md/CLAUDE.md 변경 안 함)
- **다음 작업**: main.py 생성 (pygame 창/게임 루프) — 승인 후 진행

---

## 2026-06-01 14:30

### [정리] 유니티 포팅 철회 — Python 단일 구조 복원

- **수정 파일**: `My project/` 삭제, `unity_port/` 삭제, TASK.md 갱신
- **변경 내용**:
  - `My project/`(유니티 프로젝트 전체, 추적 60파일 + 미추적 CameraFollow.cs/SceneTemplateSettings.json) `git rm -rf` 후 디스크 삭제
  - `unity_port/`(C# 코어 5종 + README_SETUP.md, 추적 6파일) `git rm -rf` 후 디스크 삭제
  - TASK.md: "Pygame→Unity 재구성" 피벗 내용을 "Python+Pygame 단일 유지"로 교체, 유니티 포팅 섹션 제거, 현재 세션 작업을 정리 작업으로 기록
- **이유**: 비니 지시 — 사용 언어를 Python만 남기고 유니티 C# 구조를 제거. 한 코드베이스만 유지해 관리 단순화.
- **원본 파일 업데이트 여부**: PLANNING.md 변경 없음(유니티 언급 원래 없었음). CLAUDE.md 변경 없음(이미 Python/Pygame 명시). TASK.md는 세션별 재작성 파일이라 규칙 #8 대상 아님.
- **복구 경로**: 삭제분은 git 히스토리(`ebf8d98` Unity 프로젝트 추가, `bd2d17d` Input Handling)에 보존됨.
- **다음 작업**: 비니 확인 후 파이썬 Phase 3 미완 항목(투사체 적 등) 재개.
