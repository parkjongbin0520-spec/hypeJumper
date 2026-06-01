# ASSETS.md — 에셋 명세서 (스프라이트 / 사운드)

> hypeJumper 비주얼·오디오 에셋 목록 + 폴더 규칙 + 인테이크 워크플로우.
> 방향성: `design.md` (Bright & Peaceful Bio-Cyberpunk / Organic Eco-Hologram).
> **핵심 원칙: 에셋이 없으면 기존 색 사각형/무음으로 폴백.** 파일을 넣을 때마다 점진적으로 채워진다.
> ⚠️ 물리/히트박스는 에셋과 무관 — 스프라이트는 히트박스보다 커도 됨(발-중앙 정렬).

---

## 1. 폴더 구조

```
assets/
├── sprites/
│   ├── player/      # player_*.png
│   ├── entities/    # 적/NTT/스프링/패드/골/가시/발판
│   ├── tiles/       # 바닥·벽 타일셋
│   ├── bg/          # 배경 패럴럭스 레이어
│   └── ui/          # HUD/게이지
├── sounds/
│   ├── sfx/         # 효과음 sfx_*
│   └── bgm/         # 배경음 bgm_*
└── tilemaps/        # map_*.txt (텍스트 맵)
```

---

## 2. 인테이크 워크플로우 (ROOT에 드롭 → 자동 정리)

1. 비니가 **프로젝트 루트**(또는 `_inbox/` 폴더)에 에셋 파일을 아래 **파일명 규칙**대로 넣는다.

2. 정리 실행:
   
   ```
   python tools/organize_assets.py --dry-run   # 미리보기
   python tools/organize_assets.py             # 실제 이동
   ```

3. 규칙에 맞는 파일만 알맞은 폴더로 이동. **규칙에 안 맞는 파일은 건드리지 않음(보호)** — `map_preview*.png` 등 안전.

### 파일명 규칙 (분류 기준)

| 접두사 / 이름                | 가는 곳             | 예                            |
| ----------------------- | ---------------- | ---------------------------- |
| `player_*.png`          | sprites/player   | `player_run.png`             |
| `tile_*.png`            | sprites/tiles    | `tile_ground.png`            |
| `bg_*.png`              | sprites/bg       | `bg_bamboo_far.png`          |
| `ui_*.png`              | sprites/ui       | `ui_dash_gauge.png`          |
| `sfx_*.(wav\|ogg\|mp3)` | sounds/sfx       | `sfx_jump.wav`               |
| `bgm_*.(ogg\|mp3\|wav)` | sounds/bgm       | `bgm_stage1.ogg`             |
| `map_*.txt`             | tilemaps         | `map_stage2.txt`             |
| 화이트리스트 엔티티명 `.png`      | sprites/entities | `enemy.png`, `spring_up.png` |

> 엔티티 화이트리스트: enemy, enemy_armored, ntt, rope_ntt, spring_up, spring_wall, jumppad, checkpoint, checkpoint_on, goal, spike, platform

---

## 3. 스프라이트 목록

> 형식: PNG + 투명 배경. **히트박스**=물리 크기(고정) / **캔버스**=권장 그림 크기(더 커도 됨).
> 스타일: 게임은 16px 타일 도트 스케일 → 캐릭터/오브젝트는 도트(또는 AI생성 후 축소·정리). 배경/UI는 고해상 콘셉트 아트 OK.

### 플레이어 (히트박스 8×16, 웅크림 8×8) ★우선

> **애니메이션**: `player_run.png` 1장이면 정지, `player_run_0.png`·`player_run_1.png`…(번호) 여러 장이면 순환 재생(≈10fps). 모든 상태 동일(idle/jump/fall/dash/duck도 `_0,_1…` 가능).

| 파일                     | 캔버스            | 용도         | 상태  |
| ---------------------- | -------------- | ---------- | --- |
| `player_idle.png`      | 16×24          | 정지         | ⬜   |
| `player_run.png`       | 16×24 (스트립 가능) | 달리기        | ⬜   |
| `player_jump.png`      | 16×24          | 상승         | ⬜   |
| `player_fall.png`      | 16×24          | 하강         | ⬜   |
| `player_dash.png`      | 24×24          | 대시         | ⬜   |
| `player_duck.png`      | 16×16          | 웅크림        | ⬜   |
| `player_wallslide.png` | 16×24          | 월슬라이드 (선택) | ⬜   |
| `player_grab.png`      | 16×24          | 잡기 (선택)    | ⬜   |

### 엔티티 / 오브젝트

| 파일                  | 히트박스  | 캔버스   | 비고                 | 상태  |
| ------------------- | ----- | ----- | ------------------ | --- |
| `enemy.png`         | 16×16 | 24×24 | 일반 적(HP1)          | ⬜   |
| `enemy_armored.png` | 16×16 | 24×24 | 강화 적(HP2, 별색/갑옷)   | ⬜   |
| `ntt.png`           | 14×14 | 16×16 | 잡기 대상              | ⬜   |
| `rope_ntt.png`      | 14×14 | 16×16 | 샹들리에 본체(줄은 선으로 렌더) | ⬜   |
| `spring_up.png`     | 16×16 | 16×16 | 위 발사 스프링           | ⬜   |
| `spring_wall.png`   | 6×32  | 8×32  | 벽 부착(세로 길쭉)        | ⬜   |
| `jumppad.png`       | 16×16 | 16×16 | 점프패드               | ⬜   |
| `checkpoint.png`    | 16×16 | 16×32 | 체크포인트(비활성)         | ⬜   |
| `checkpoint_on.png` | 16×16 | 16×32 | 체크포인트(활성)          | ⬜   |
| `goal.png`          | 16×16 | 16×32 | 레벨 종료(깃발/포탈)       | ⬜   |
| `spike.png`         | 16×16 | 16×16 | 가시(위험)             | ⬜   |

### 타일 / 배경

| 파일                       | 크기                        | 비고                    | 상태  |
| ------------------------ | ------------------------- | --------------------- | --- |
| `tile_ground.png` (+ 변형) | 16×16                     | 바닥/벽 타일셋(모서리 변형 권장)   | ⬜   |
| `platform.png`           | 16×16 반복 또는 48×14 9-slice | 발판 폭 가변 → 타일링/9-slice | ⬜   |
| `bg_sky.png`             | 960×540+                  | 야간 하늘(현 그라데이션 대체/보강)  | ⬜   |
| `bg_bamboo_far.png`      | 1920×540                  | 먼 대나무숲(느린 패럴럭스)       | ⬜   |
| `bg_bamboo_near.png`     | 1920×540                  | 가까운 대나무(빠른 패럴럭스)      | ⬜   |

---

## 4. 사운드 목록

> 효과음 `.wav`/`.ogg`(짧게), 배경음 `.ogg`(루프). 파일 없으면 무음 폴백.

### 효과음 (코드 이벤트)

| 파일                    | 발동 시점        | 우선  | 상태  |
| --------------------- | ------------ | --- | --- |
| `sfx_jump.wav`        | 일반 점프        | ★   | ⬜   |
| `sfx_dash.wav`        | 대시 시작        | ★   | ⬜   |
| `sfx_land.wav`        | 착지           | ★   | ⬜   |
| `sfx_death.wav`       | 사망/리스폰       | ★   | ⬜   |
| `sfx_goal.wav`        | 레벨 클리어(골 도달) | ★   | ⬜   |
| `sfx_walljump.wav`    | 월 점프         | 권장  | ⬜   |
| `sfx_wallbounce.wav`  | 월바운스         | 권장  | ⬜   |
| `sfx_super.wav`       | 슈퍼 대시 점프     | 권장  | ⬜   |
| `sfx_hyper.wav`       | 하이퍼 대시 점프    | 권장  | ⬜   |
| `sfx_grab.wav`        | 잡기(순간이동)     | 권장  | ⬜   |
| `sfx_release.wav`     | 릴리즈          | 권장  | ⬜   |
| `sfx_enemy_hit.wav`   | 적 피격(HP 잔존)  | 권장  | ⬜   |
| `sfx_enemy_break.wav` | 적 파괴(HP0)    | 권장  | ⬜   |
| `sfx_spring.wav`      | 스프링 발사       | 권장  | ⬜   |
| `sfx_jumppad.wav`     | 점프패드 발사      | 권장  | ⬜   |
| `sfx_checkpoint.wav`  | 체크포인트 활성     | 권장  | ⬜   |
| `sfx_footstep.wav`    | 지상 이동(발소리)   | 선택  | ⬜   |
| `sfx_duck.wav`        | 웅크림          | 선택  | ⬜   |

### 배경음

| 파일               | 용도           | 상태  |
| ---------------- | ------------ | --- |
| `bgm_stage1.ogg` | 대나무숲 평화로운 루프 | ⬜   |

---

## 5. 진행 단계

- [x] **6-1 자산 구조 + 인테이크** — 폴더 생성, `tools/organize_assets.py`, 이 문서
- [x] **6-2 스프라이트 로더** — `src/assets.py` 캐시 + 전 엔티티 draw "있으면 blit/타일, 없으면 사각형"
- [x] **6-3 사운드** — `src/audio.py` SFX/BGM 로더 + 이벤트 지점 재생(없으면 무음)
- [x] **6-4 패럴럭스 배경** — bg 3레이어 카메라 속도차 스크롤 (그라데이션 base 위, 없으면 폴백)
- [x] **6-5 플레이어 애니메이션** — 번호 프레임(`player_run_0.png`…) 순환, 없으면 단일/사각형 폴백

> 상태 표기: ⬜ 미제작 / ✅ 적용됨. 에셋이 들어오고 로더가 붙으면 ✅로 갱신.
