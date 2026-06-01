# TASK.md — 현재 세션 작업 목록

> 이 파일은 한 세션에 한 가지 작업만 포함한다.
> Claude Code는 이 파일에 명시된 작업만 수행하며, 임의로 다음 단계로 넘어가지 않는다.

---

## 진행 규칙

1. 이 파일의 현재 세션 작업만 수행한다
2. 작업 완료 후 WORK_LOG.md에 기록한다
3. QA 자체 테스트 + QA 보고서 작성
4. 비니의 최종 검수 요청
5. 비니 승인 시 → 다음 세션 작업을 이 파일에 추가

---

## 현재 상태 (2026-06-01 기준)

**언어/프레임워크: Python + Pygame (단일 유지).**
유니티/C# 포팅은 **철회** — `My project/`(유니티 프로젝트)와 `unity_port/`(C# 코어) 전부 삭제.
복구 필요 시 git 히스토리(`ebf8d98`, `bd2d17d`)에서 되살릴 수 있음.

### 파이썬 본체 진행도

- Phase 1(이동) / Phase 2(대시) / Phase 2.5(슈퍼·하이퍼·월바운스) — 완료
- Phase 3A(가시/체크포인트/텍스트맵) / 3B(점프패드·스프링) — 완료
- Phase 3C 잡기 — 재설계 완료(Z누름 윈도우+슬로우, 아래=대시, 가시차단, 줄NTT). **최신 사양 = WORK_LOG (3)~(8) + PLANNING [재설계] 섹션**
- 3C-2 적(Enemy/ArmoredEnemy), 3C-3 줄NTT — 완료
- DASH_STRIKE — 구현 후 **제거**(비니 결정)
- 미완: 투사체 적, 타일맵 파일 로더, 카메라 추적, 폴리싱

---

## 현재 세션 작업

### [Phase 3] 타일맵 파일 로더 — 2섹션 텍스트 외부 파일 로드

- [x] 구조 평가 보고서 작성·승인 (비니 "승인 + TileMap만")
- [x] `assets/tilemaps/test_map.txt` 생성 (하드코딩 맵 바이트 동일 덤프)
- [x] `tilemap.py`에 `load_file`/`_parse_objects`/폴백 추가 (scene 무변경, 비파괴)
- [x] QA 자동 검증 9/9 동일성 + 폴백 2종 + update 180f / 버그 2건 수정
- [x] WORK_LOG.md 기록

**비니 확인 요청**: 이번엔 TileMap만 파일 지원 추가(`python main.py`는 기존과 동일 동작). 확인 후 다음 작업 지정.

---

## 다음 세션 예고 (참고)

- (선택) scene.py를 `test_map.txt` 로드로 연결 → 실제 파일 기반 실행
- 또는 Phase 3C-5(투사체 쏘는 적) — Phase 3 마지막 항목
- 또는 Phase 4 카메라 추적

### 게임에 살 붙이기

목표 (Goal)
핵심 작업: 현재 960x540 고정 해상도 화면을 탈피하기 위한 **카메라 시스템(Camera)**을 도입하고, 다중 레벨 로드 및 전환 구조를 구현합니다.
개발 범위:
camera.py 신설 및 Scene 업데이트/렌더링 파이프라인 연동
화면보다 큰 대형 맵 테스트용 타일맵 로드 및 스크롤 기능 검증
레벨 클리어 트리거 도달 시 다음 레벨로 전환되는 기초 흐름 연동 2. 맥락 (Context)
현재 아키텍처: Entity -> Actor/Solid/Trigger 구조. layer.py 기반 충돌 테이블 적용 중.
플레이어 물리 코어: vx_input과 vx_external이 분리된 상태 머신 기반 물리 가동 중 (작동 무결성 유지 필수).
렌더링 방식: 현재 pygame.draw.rect 디버그 사각형 사용 중이며, 카메라 오프셋(camera.offset) 값이 모든 그리기 연산에 감산 적용되어야 함.
이전 단계: 타일맵 파일 로더(TileMap.load_file) 구현 완료. 이제 이 로더를 대형 맵 스케일로 확장해야 함. 3. 제한 사항 (Constraints)
기존 물리 보존: 기존 8방향 대시, 하이퍼점프, 잡기 등 복잡한 이동 물리 연산 로직(Player.py)은 절대 수정하거나 간소화하지 마십시오.
파일 분리: 카메라 기능은 camera.py 내 별도 클래스로 캡슐화하고, Scene에서 인스턴스화하여 제어합니다.
카메라 동작 제한: 플레이어가 화면 밖으로 나가지 않도록 맵 경계(Map Boundary) 안에서 고정(Clamp)되어야 합니다.
의존성: 추가적인 외부 그래픽 라이브러리를 사용하지 않고 기본 pygame 라이브러리 범위 내에서 구현합니다. 4. 완료 기준 (Definition of Done)

카메라 스크롤: 플레이어가 중앙 부근(데드존 설정 가능)을 벗어날 때 화면이 부드럽게(Lerp 수치 적용 가능) 추적하는가?

경계 제한: 카메라 오프셋이 타일맵의 가로/세로 경계를 벗어나서 배경 여백을 보여주지 않는가?

좌표 변환: 모든 Entity와 디버그 사각형이 draw(surface, camera) 형태로 카메라 좌표계 기준 렌더링을 정상 수행하는가?

레벨 전환: 특정 트리거 구역 진입 시, 새로운 맵 데이터를 다시 읽어와 플레이어 위치를 초기화하고 다음 스테이지를 구동하는가?

---

### 진행 결과 (2026-06-01 — A안 / 5-1·5-2 한 세션)

- [x] **5-1 카메라 기초** — `src/camera.py` 신설(lerp 추적+경계 클램프), `TileMap.width/height`(맵 픽셀크기) 노출, Scene이 Camera 인스턴스화·`camera.offset`로 전 엔티티 렌더, 큰 맵 `level1.txt`(1920×720) 로드 연결
- [x] **5-2 레벨 전환** — `Scene._load_level()` 메서드화 + `LEVEL_FILES` 시퀀스, `Goal` 트리거('G' 기호) 신설, `level2.txt`(1600×608), 골 도달 시 다음 맵 로드+플레이어/카메라 초기화
- [x] **제약 준수** — player.py 물리 무수정(git diff 확인) / 카메라 camera.py 캡슐화 / 경계 클램프 / pygame만 / A안이라 엔티티 draw 무수정
- [x] QA 자동 11/11 + 스크롤/전환/렌더 스모크 + WORK_LOG (12) 기록

**QA 보고서 — Phase 4 카메라/레벨**
- 테스트 항목: 4 (카메라 클램프/추적 11항목 / 스크롤 단조·끝클램프 / 레벨전환 / draw 스모크)
- 통과: 4 (세부 11/11 포함) · 실패: 0
- 발견 버그: 없음 (낙사 임계를 화면→맵 높이로 보정, 골 도달성 위해 level1 계단 완만화)
- 보완 완료: ✅
- **최종 검수 요청**: `python main.py` — level1 우/상 이동 시 카메라 부드러운 추적·경계 밖 여백 없음·골(노란칸) 도달 시 level2 전환·새 스폰. R=레벨1 리셋.

> DoD: ① 부드러운 추적(lerp) ✅ ② 경계 클램프 ✅ ③ 전 엔티티 카메라 좌표 렌더(A안=offset 주입) ✅ ④ 트리거 진입 시 다음 맵 로드+위치 초기화 ✅
> 미적용(선택): 데드존(현재 중앙 lerp), 카메라 룩(Spelunky, Phase 4 별도 항목)

---

### 비주얼 기초 (2026-06-01 — design.md "Organic Eco-Hologram" 팔레트)

- [x] **팔레트** — design.md 비취/민트/유백/안개그레이/황금/코랄 상수 신설, 기존 COLOR_* 값 재매핑 (가독성: 플레이어=유백, 적/가시=코랄 대비)
- [x] **배경** — 비취빛 야간 수직 그라데이션 + 정적 반딧불(황금 점), `Scene._build_background`로 1회 베이킹·캐싱
- [x] main.py 중복 fill 제거(배경 Scene 소유) / player.py·물리 무관
- [x] QA 자동(그라데이션 정확·반딧불·캐싱·합성·180f/60f 예외0) + 프리뷰 map_preview_visual.png + WORK_LOG (13)

**최종 검수 요청**: `python main.py` — 동화풍 비취 야간 톤·은은한 반딧불·캐릭터/위험 가독성 체감.
**커밋**: 이번 세션 3작업(타일맵 로더+카메라/레벨+비주얼) 커밋 여부 비니 지시 대기.

---

### 6-1 자산 구조 + 인테이크 (2026-06-01)

- [x] 폴더 구조 생성 (assets/sprites/{player,entities,tiles,bg,ui}, assets/sounds/{sfx,bgm}, .gitkeep)
- [x] `tools/organize_assets.py` — ROOT 드롭 → 파일명 규칙 자동 분류, 모르는 파일 보호, `--dry-run`
- [x] `ASSETS.md` 박제 — 스프라이트/사운드 목록(크기·우선·상태) + 워크플로우
- [x] QA(더미 7개 라우팅·보호·dry=real) + WORK_LOG (14)

**사용법**: 에셋을 ROOT에 `규칙대로`(player_/sfx_/bg_/enemy.png …) 넣고 → `python tools/organize_assets.py`
**다음**: ~~6-2 스프라이트 로더~~ → 6-3 사운드 → 6-4 패럴럭스 → 6-5 애니메이션

---

### 6-2 스프라이트 로더 (2026-06-01)

- [x] `src/assets.py` — 이미지 캐시(`get_sprite`/`first_sprite`) + `blit_or_rect`(발-중앙 정렬) + `tile_fill`(타일 반복)
- [x] 전 엔티티 draw 배선 — player(상태별)/enemy/ntt/rope/hazard/spring/jumppad/checkpoint/goal + tilemap solids·platform 타일링
- [x] **폴백**: 에셋 없으면 기존 사각형 100% 동일 / **물리·상태 로직 무수정**(렌더 한정)
- [x] QA 6/6(폴백 동일·스프라이트 교체·타일링·Game 90f 예외0) + WORK_LOG (15)

**사용**: ASSETS.md 규칙대로 PNG를 ROOT 드롭 → organize → **화면에 자동 표시**(코드 추가 불필요).
**다음**: ~~6-3 사운드~~ / 6-4 패럴럭스 배경 / 6-5 플레이어 애니메이션

---

### 6-3 사운드 (2026-06-02)

- [x] `src/audio.py` — `init`/`play(sfx_<name>)`/`play_music(bgm_<name>)`, 미존재·장치없음 시 무음 폴백
- [x] 이벤트 배선 — jump/walljump/dash/super/hyper/wallbounce/grab/release/착지/적피격·파괴/스프링/패드/체크포인트/사망/골 + BGM 루프
- [x] **물리 수식 무수정**(소리 side-effect만) / main에 `audio.init()`
- [x] QA 7/7(폴백·로드·BGM·이벤트 120f 예외0) + WORK_LOG (16)
- 비니 `bgm_stage1.ogg` 투입 확인 → 자동 루프 동작

**사용**: `sfx_jump.wav` 등 ROOT 드롭 → organize → 자동 재생. (필수 ★: jump/dash/land/death/goal)
**다음**: ~~6-4 패럴럭스~~ / 6-5 플레이어 애니메이션

---

### 6-4 패럴럭스 배경 (2026-06-02)

- [x] settings 패럴럭스 계수(sky 0.10 / far 0.30 / near 0.60)
- [x] scene `_draw_background`에 3레이어(bg_sky/bamboo_far/bamboo_near) 깊이순 렌더 + `_draw_parallax_layer`(카메라 x×factor 가로 무한 타일링, 바닥 정렬)
- [x] **폴백**: bg 스프라이트 없으면 기존 그라데이션+반딧불만(100% 동일)
- [x] QA 4/4(폴백 동일·레이어 렌더·패럴럭스 스크롤·Game 90f 예외0) + WORK_LOG (17)

**사용**: `bg_sky.png`/`bg_bamboo_far.png`/`bg_bamboo_near.png`(1920×540 권장, 투명) ROOT 드롭 → organize → 자동 패럴럭스
**다음**: ~~6-5 애니메이션~~

---

### 6-5 플레이어 애니메이션 (2026-06-02) — 비주얼 기초 완료

- [x] settings `ANIM_FRAME_DUR=6` / assets `frame_names`(번호 프레임 탐색)
- [x] player draw: `_anim_t` 클럭 + `_animated`로 상태별 번호 프레임 순환, **물리 무수정**
- [x] 3단 폴백: 번호프레임(`player_run_0`…) → 단일(`player_run`) → 사각형
- [x] QA 7/7(폴백·단일·순환·래핑·Game 120f 예외0) + WORK_LOG (18)

**사용**: `player_run_0.png`,`player_run_1.png`… ROOT 드롭 → organize → 자동 순환 애니
**비주얼 기초(6-1~6-5) 전부 완료** — 이후: UI 폴리싱 / 데드존·카메라룩 / 투사체 적(3C-5)
