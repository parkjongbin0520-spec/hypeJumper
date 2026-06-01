# PLANNING.md — 아키텍처 설계 문서

## Celeste 원본 참고 범위

### 가져오는 것
- 이동 (Approach 기반 가속도)
- 점프 (가변 높이, 3단계 중력)
- 대시 (8방향)
- 슈퍼/하이퍼/월바운스
- 월 슬라이드/월 점프
- 코요테 타임, 점프 버퍼

### 제거하는 것
- Grab (잡기) 및 클라이밍 전체 (`Climb*` 상수 전부)

### 퍼즐로 재활용하는 것
- 잡기 로직 → Phase 3 퍼즐 메카닉으로 별도 설계

---

## Celeste 원본 상수값 (참고용)

> ⚠️ Celeste는 초당 픽셀(px/s) 단위, 우리는 프레임당 픽셀 단위
> 수치를 그대로 쓰지 말고 **비율과 구조만 참고**할 것
> 실제 수치는 settings.py에서 직접 튜닝

### 수평 이동
```
MaxRun      = 90f     # 최대 수평 속도
RunAccel    = 1000f   # 지상 가속도
RunReduce   = 400f    # 초과 속도 감속
AirMult     = 0.65f   # 공중 제어 배율 ← 이건 비율이라 그대로 사용 가능
```

### 점프
```
JumpSpeed         = -105f  # 점프 초기 속도
JumpHBoost        = 40f    # 점프 시 수평 부스트
VarJumpTime       = 0.2f   # 가변 점프 유지 시간 (초)
JumpGraceTime     = 0.1f   # 코요테 타임 (약 6프레임 @60fps)
HalfGravThreshold = 40f    # 이 속도 이하일 때 중력 절반 적용 (정점 근처 체공감)
Gravity           = 900f   # 기본 중력
MaxFall           = 160f   # 최대 낙하 속도
FastMaxFall       = 240f   # 빠른 낙하 최대 속도
```

### 월 슬라이드 / 월 점프
```
WallSlideStartMax      = 20f   # 월 슬라이드 시작 최대 속도
WallSlideTime          = 1.2f  # 월 슬라이드 최대 지속 시간 (초)
WallJumpHSpeed         = 130f  # 월 점프 수평 속도 (MaxRun + JumpHBoost)
WallJumpForceTime      = 0.16f # 월 점프 후 강제 이동 시간
WallJumpCheckDist      = 3     # 벽 판정 거리 (픽셀)
WallSpeedRetentionTime = 0.06f # 벽 속도 유지 시간 (코요테 유사)
```

### 대시
```
DashSpeed      = 240f  # 대시 속도
EndDashSpeed   = 160f  # 대시 종료 후 속도
EndDashUpMult  = 0.75f # 위쪽 대시 종료 시 속도 배율
DashTime       = 0.15f # 대시 지속 시간 (초)
DashAttackTime = 0.3f  # 대시 어택 판정 시간
```

### 슈퍼/하이퍼/월바운스
```
SuperJumpH             = 260f  # 슈퍼 대시 수평 속도
SuperWallJumpSpeed     = -160f # 월바운스 수직 속도
SuperWallJumpH         = 170f  # 월바운스 수평 속도 (MaxRun + JumpHBoost * 2)
SuperWallJumpVarTime   = 0.25f # 월바운스 가변 점프 시간
SuperWallJumpForceTime = 0.2f  # 월바운스 강제 이동 시간
```

---

## 전체 파일 구조

```
project/
├── CLAUDE.md
├── PLANNING.md
├── TASK.md
├── WORK_LOG.md        ← Claude Code 자동 작업 내역 기록
├── main.py
├── settings.py
├── src/
│   ├── scene.py       ← Scene 클래스 (엔티티 관리, 메인 루프)
│   ├── player.py      ← Player 클래스, PlayerState, InputBuffer
│   ├── tilemap.py     ← TileMap (타일맵 로드, 충돌 레이어)
│   ├── camera.py      ← Camera (플레이어 추적)
│   ├── layer.py       ← Layer enum, 레이어 간 충돌 규칙
│   └── entities/
│       ├── entity.py      ← Entity 기본 클래스
│       ├── actor.py       ← Actor (Entity 상속, 이동/충돌 공통)
│       ├── solid.py       ← Solid (Entity 상속, 막히는 오브젝트)
│       ├── trigger.py     ← Trigger (Entity 상속, 범위 기반 발동)
│       ├── platform.py    ← MovingPlatform (Solid 상속)
│       ├── enemy.py       ← Enemy, ArmoredEnemy (Actor 상속)
│       ├── ntt.py         ← NTT, RopeNTT (Actor 상속)
│       ├── projectile.py  ← Projectile (Entity 상속)
│       ├── jump_pad.py    ← JumpPad (Trigger 상속)
│       ├── spring.py      ← Spring (Trigger 상속)
│       └── hazard.py      ← Hazard (Trigger 상속, 가시/톱니)
└── assets/
    ├── sprites/
    ├── tilemaps/
    └── sounds/
```

---

## 클래스 상속 구조 (Celeste 방식)

```
Entity                         # 위치, 히트박스, 레이어, update/draw
  ├── Actor(Entity)            # 이동, 중력, 충돌 — 움직이는 것들
  │     ├── Player(Actor)      # 플레이어
  │     ├── Enemy(Actor)       # 적 (AI 이동, 투사체 발사)
  │     │     └── ArmoredEnemy(Enemy)  # 강화 적 (HP 2+)
  │     └── NTT(Actor)         # 잡기 가능 엔티티
  │           └── RopeNTT(NTT) # 줄 NTT (진자 운동)
  │
  ├── Solid(Entity)            # 막히는 오브젝트 — Actor 이동 차단
  │     └── MovingPlatform(Solid)  # 트리거 시 이동하는 발판
  │
  ├── Projectile(Entity)       # 투사체 — Actor 아님 (중력 없음, Solid 통과, 벽 소멸)
  │
  └── Trigger(Entity)          # 범위 기반 발동 — target_layers로 반응 대상 제한
        ├── JumpPad(Trigger)   # target_layers = [PLAYER]
        ├── Spring(Trigger)    # target_layers = [PLAYER]
        └── Hazard(Trigger)    # target_layers = [PLAYER]
        # 나중에 필요 시 target_layers에 GRABABLE 추가만 하면 확장 가능
```

### 각 클래스 역할
```python
class Entity:
    rect: pygame.Rect    # 히트박스 (충돌 판정용)
    sprite_rect: Rect    # 스프라이트 위치 (히트박스보다 약간 큼)
    layer: Layer         # 소속 레이어
    scene: Scene         # 소속 씬 참조
    def update(self): ...
    def draw(self, surface): ...

class Actor(Entity):
    vx: float
    vy: float
    on_ground: bool
    on_wall: bool
    is_ducking: bool     # 웅크리기 플래그 (NORMAL 안에서만 처리)
    def move_x(self, amount): ...
    def move_y(self, amount): ...

class Solid(Entity):
    vx: float
    vy: float
    def move(self): ...  # 이동 시 위에 있는 Actor도 같이 밀기

class Projectile(Entity):
    vx: float
    vy: float
    # Solid 충돌 시 소멸, PLAYER만 사망 판정, GRABABLE 통과

class Trigger(Entity):
    target_layers: list
    _prev_contacts: set  # 이전 프레임 접촉 목록

    def update(self):
        curr = {a for a in self.scene.get_actors()
                if a.layer in self.target_layers
                and self.rect.colliderect(a.rect)}
        for a in curr - self._prev_contacts: self.on_enter(a)
        for a in curr & self._prev_contacts: self.on_stay(a)
        for a in self._prev_contacts - curr: self.on_exit(a)
        self._prev_contacts = curr

    def on_enter(self, actor): ...
    def on_stay(self, actor): ...
    def on_exit(self, actor): ...
```

### 웅크리기 처리 (NORMAL 상태 안 플래그, 별도 상태 전환 없음)
```python
# NORMAL 상태 update() 안에서
is_ducking = on_ground and 아래_입력 and not 이동_입력

if is_ducking:
    self.rect.height = DUCK_HITBOX_HEIGHT
else:
    self.rect.height = NORMAL_HITBOX_HEIGHT

# 웅크리기 해제 조건
if is_ducking and (이동_입력 or 점프_입력):
    is_ducking = False
```

---

## 적 AI 시스템

### 적 행동 패턴
```
일반 적:
  - 생성 위치(origin)를 중심으로 ENEMY_PATROL_RADIUS 반경 내에서만 활동
  - 반경 내 플레이어 감지 시 → 플레이어 방향으로 이동
  - 반경 밖 이동 불가 (경계에서 멈춤)
  - 일정 간격으로 플레이어 위치 조준 후 투사체 발사

함정/맵 패턴 적:
  - AI 없음, 정해진 경로로만 이동
  - MovingPlatform과 동일한 트리거 구조 사용 가능
```

### 적 AI 상태
```python
class EnemyState(Enum):
    IDLE      # 대기 (플레이어 미감지)
    PATROL    # 반경 내 순찰
    CHASE     # 플레이어 추적 (반경 내)
    ATTACK    # 투사체 발사 준비/발사
    STUNNED   # 피격 후 무적 프레임 중
    DEAD      # 파괴 후 리스폰 대기
```

### 투사체 발사 로직
```python
# 적이 플레이어를 조준한 후 발사
direction = normalize(player.pos - enemy.pos)
projectile = Projectile(
    pos=enemy.pos,
    velocity=direction * PROJECTILE_SPEED,
    layer=Layer.HAZARD  # 플레이어만 맞춤
)
# 투사체는 WALL 레이어 충돌 시 소멸
# GRABABLE 레이어 통과 (NTT/적 통과)
```

### 활동 반경 제한
```python
# 매 프레임 적 이동 후 체크
dist = distance(enemy.pos, enemy.origin)
if dist > ENEMY_PATROL_RADIUS:
    # 경계로 밀어내기 (원점 방향으로)
    enemy.pos = enemy.origin + normalize(enemy.pos - enemy.origin) * ENEMY_PATROL_RADIUS
    enemy.vx = 0
```

---

## 플레이어 속도 구조

```
플레이어 최종 속도 = (vx_input + vx_external, vy)

vx_input    : 플레이어 입력 속도 (-PLAYER_MAX_SPEED ~ +PLAYER_MAX_SPEED)
vx_external : 외적 요인 속도 (대시, 발판 관성 등, 상한선 없음)
vy          : 수직 속도 (중력, 점프, 발판 관성)
```

### 지상에서의 수평 속도
- 입력 시 목표 속도(PLAYER_MAX_SPEED)를 향해 GROUND_ACCEL로 서서히 가속
- 입력 없을 때 GROUND_DECEL로 서서히 감속 → 0 수렴
- 과속 상태로 착지 시 SPEED_REDUCE로 빠르게 감속 (미끄럼 최소화)

### 공중에서의 수평 속도
- 지상과 동일한 가속도 방식이지만 AIR_MULT 배율 적용 (공중 제어력 감소)
- 외적 요인(대시, 발판 관성)으로 PLAYER_MAX_SPEED 초과 시:
  - 같은 방향 입력 → SPEED_REDUCE로 감속만 됨 (추가 가속 불가)
  - 반대 방향 입력 → AIR_MULT 배율로 감속 가능
- 입력 없을 때: AIR_FRICTION으로 서서히 감쇠

### 공중 수평 속도 감소 방식 (방식 B — Celeste 기반, 확정)
```python
# 지상
target_vx = input_dir * PLAYER_MAX_SPEED
vx = approach(vx, target_vx, GROUND_ACCEL)

# 공중
target_vx = input_dir * PLAYER_MAX_SPEED
vx = approach(vx, target_vx, GROUND_ACCEL * AIR_MULT)

# 외적 속도 초과 시 감속
if abs(vx) > PLAYER_MAX_SPEED:
    vx = approach(vx, PLAYER_MAX_SPEED * sign(vx), SPEED_REDUCE)

def approach(current, target, amount):
    if current < target:
        return min(current + amount, target)
    return max(current - amount, target)
```

---

## 물리 처리 순서 (매 프레임)

```
1. 입력 처리
   └─ 방향키 → vx 목표값 설정
   └─ 점프 입력 → JUMP_INPUT 버퍼 활성화
   └─ 대시 입력 → DASH 상태 전환 (횟수 > 0, 방향키 필수)
   └─ Z키 홀드 → GRAB_SEEKING 진입 (걷기/점프/대시 가능 유지)

2. 버퍼 틱
   └─ GROUND_COYOTE, WALL_COYOTE, JUMP_INPUT, WALL_JUMP_INPUT, DASH_STRIKE_BUFFER 카운트다운

3. 수평 속도 업데이트 (NORMAL 상태)
   └─ vx = approach(vx, target_vx, GROUND_ACCEL * (1.0 if 지상 else AIR_MULT))
   └─ vx 초과 시 approach(vx, MAX_SPEED * sign, SPEED_REDUCE)
   └─ 입력 없음: vx = approach(vx, 0, GROUND_DECEL * (1.0 if 지상 else AIR_FRICTION))

4. 중력 적용 (NORMAL 상태, 조건 분기)
   └─ vy < 0 and 점프버튼 홀드
      └─ abs(vy) < HALF_GRAV_THRESHOLD → vy += GRAVITY_UP * 0.5
      └─ 그 외 → vy += GRAVITY_UP
   └─ vy < 0 and 점프버튼 뗌 → vy += GRAVITY_UP_RELEASE
   └─ 벽 접촉 and vy > 0
      └─ 아래 입력 → vy += GRAVITY_DOWN (패스트 폴)
      └─ 그 외 → vy += GRAVITY_DOWN * WALL_SLIDE_GRAVITY
   └─ 그 외 (하강)
      └─ 아래 입력 → vy += FAST_FALL_GRAVITY, 상한 FAST_MAX_FALL
      └─ 그 외 → vy += GRAVITY_DOWN, 상한 MAX_FALL_SPEED

5. 발판 속도 합산
   └─ 발판 위: 위치에 platform.vx, platform.vy 직접 더함

6. 이동 적용 (Actor.move_x / move_y 사용)
   └─ x += vx + (platform.vx if on_platform)
   └─ y += vy + (platform.vy if on_platform)

7. 충돌 감지 및 해소
   └─ 바닥/발판(Solid): vy = 0, on_ground = True, GROUND_COYOTE 리셋, 대시 충전
   └─ 벽(Solid): vx = 0, on_wall = True, WALL_COYOTE 리셋
   └─ 천장(Solid): ceiling_stick_frames 계산, vy = 0
   └─ Hazard: 플레이어 사망 처리
   └─ Trigger(JumpPad/Spring): on_enter 발동

8. 버퍼 조건 확인 및 자동 실행
   └─ on_ground + JUMP_INPUT 활성 → 즉시 점프
   └─ on_wall + WALL_JUMP_INPUT 활성 → 즉시 월 점프
```

---

## 통합 버퍼 시스템

```python
class InputBuffer:
    """단일 버퍼 인스턴스. 활성화, 틱, 소비 기능 제공"""
    max_frames: int
    frames: int

# Player 클래스 내 버퍼 인스턴스
ground_coyote      = InputBuffer(COYOTE_TIME)
wall_coyote        = InputBuffer(WALL_COYOTE_TIME)
jump_input         = InputBuffer(JUMP_BUFFER)
wall_jump_input    = InputBuffer(WALL_JUMP_BUFFER)
dash_strike_buffer = InputBuffer(DASH_STRIKE_BUFFER)
```

---

## 상태머신 전환 조건

```
NORMAL
  → DASH          조건: 방향키 + 대시키 AND 대시 횟수 > 0
  → GRAB_SEEKING  조건: Z키 홀드 (NORMAL, DASH 상태 모두에서 가능)

DASH
  → NORMAL        조건: DashTime 만료
  → GRAB_SEEKING  조건: 대시 중 Z키 홀드 (탐색 범위 DASH_STRIKE_RANGE로 확대)
  → DASH_STRIKE   조건: GRAB_SEEKING 중 레이캐스트 통과 대상 존재 (Phase 3)

DASH_STRIKE
  → NORMAL        조건: 대상 처치 완료
                  결과: 돌진 방향 가속도 유지, 대시 1회 충전

GRAB_SEEKING (Z키 홀드 중, 조준 상태)
  → 범위 내 NTT/적 없음:              범위만 표시, Z키 뗌 시 NORMAL 복귀
  → 범위 내 NTT/적 있음 + 장애물 있음: 범위 빨간색, Z키 뗌 시 NORMAL 복귀
  → 범위 내 NTT/적 있음 + 장애물 없음: 범위 초록색, Z키 뗌 시 GRAB_READY 전환
  → 대시 입력: GRAB_SEEKING 해제 → DASH 전환 (웅크리기 해제와 동일한 원리)

GRAB_READY (잡기 가능 확인, Z키 뗀 상태, 재입력 대기)
  → GRAB_ACTIVE   조건: Z키 재입력
  → NORMAL        조건: GRAB_READY_TIMEOUT 초과 OR 대상이 범위 벗어남

GRAB_ACTIVE (순간이동 완료, NTT와 이동 중)
  → NORMAL        조건: Z키 뗌 OR 2.5초 초과
                  결과: 대시 1회 충전, 방향키 기준 슈퍼/하이퍼/월바운스/점프 (대시 소모 X)
```

### NORMAL 상태 내부 조건 분기
```python
if vy < 0 and 점프버튼_홀드:
    중력 = GRAVITY_UP
    if abs(vy) < HALF_GRAV_THRESHOLD:
        중력 = GRAVITY_UP * 0.5  # 정점 체공
elif vy < 0 and not 점프버튼_홀드:
    중력 = GRAVITY_UP_RELEASE
elif 벽_접촉 and vy > 0:
    if 아래_입력:
        중력 = GRAVITY_DOWN
    else:
        중력 = GRAVITY_DOWN * WALL_SLIDE_GRAVITY
else:
    if 아래_입력:
        중력 = FAST_FALL_GRAVITY
    else:
        중력 = GRAVITY_DOWN
```

---

## 대시 시스템

### 대시 규칙
- 최대 보유 횟수: 1회
- 충전 조건:
  - 바닥/발판 착지 시 자동 충전
  - 바닥에서 대시 시 즉시 충전
  - 점프패드/스프링 닿을 시 충전 (Phase 3)
  - 잡기로 적 처치 시 충전 (Phase 3)
  - DASH_STRIKE 처치 시 충전 (Phase 3)
- 대시 중 중력 완전 무시
- 대시 방향: 8방향, 방향키 + 대시키 동시 입력 필수
- 방향키 없이 대시키만 입력 시 발동 안 됨
- 쿨타임 없음 (횟수 제한으로만 관리)

### 대시 종료 처리 (방향별)
```
수평 대시 종료:        vx = EndDashSpeed (급격히 줄어드는 느낌)
수직 위 대시 종료:     vy *= EndDashUpMult (0.75 추가 감속)
대각선 아래 대시 종료: 속도 유지 → 하이퍼 트리거 가능
```

### 대시 상태 흐름
```
DASH 진입 → 방향 결정, 속도 설정, 중력 무시 시작, 타이머 시작
DASH 유지 → DashTime 동안 유지, DASH_STRIKE 조건 매 프레임 체크
DASH 종료 → 방향별 속도 처리, 중력 재개 → NORMAL 복귀
```

### DASH_STRIKE 상세 로직 (벽력일섬)
```python
# 탐색 범위: DASH_STRIKE_RANGE (원형, GRAB_RANGE보다 큼)
→ 대상 방향으로 즉시 돌진 (DASH_SPEED 유지)
→ 대상 처치
→ 돌진 방향 vx/vy 외적 속도로 전달 (AIR_FRICTION으로 감쇠)
→ 대시 1회 충전
→ NORMAL 복귀
```

---

## 슈퍼 / 하이퍼 / 월바운스

### 슈퍼 대시
```
발동: 수평 대시 중/후 점프 입력
결과: vx = SUPER_JUMP_H * 점프_입력_방향, vy = JumpSpeed
코너 부스트: 대시 방향과 반대 방향 점프도 동일하게 적용
```

### 하이퍼 대시
```
발동: 공중 대각선 아래 대시 → 착지 순간 점프 (JUMP_INPUT 버퍼 활용)
결과: vx = SUPER_JUMP_H * X_MULT * 점프_입력_방향
      vy = JumpSpeed * Y_MULT (낮은 점프)
코너 부스트: 반대 방향 점프도 동일하게 적용
```

### 월바운스
```python
if 수직_대시_중 and 벽_감지:
    if 아래_입력:
        vx = SUPER_WALL_JUMP_H * 벽_반대_방향
        vy = WALL_BOUNCE_DIAG_Y   # 양수 (아래, 대각선)
    else:
        vx = SUPER_WALL_JUMP_H * 벽_반대_방향
        vy = SUPER_WALL_JUMP_SPEED  # 음수 (상승)
```

### 기술 비교표
```
기술                수평     수직      타이밍
슈퍼                빠름     일반      대시 중/후 점프
슈퍼 코너부스트     빠름     일반      대시 중/후 반대방향 점프
하이퍼              매우빠름 낮음      착지 순간 점프 (버퍼)
하이퍼 코너부스트   매우빠름 낮음      착지 순간 반대방향 점프
월바운스 (기본)     최고     강한상승  수직대시 중 벽 감지
월바운스 (대각선)   최고     아래      수직대시 중 벽+아래입력
```

---

## 잡기 시스템 (Phase 3)

> ⚠️ **최신 사양 안내 (2026-06-01 재설계, 추가만).** 아래 원본 설계는 초기안이며 실제 구현은 변경됨.
> 정확한 최신 사양은 이 섹션 끝의 **`### [재설계] 잡기 시스템 최신 사양`** 와 WORK_LOG 2026-06-01 (3)~(9)를 따른다.
> (요약: GRAB_READY/재입력 폐기 → Z 누름 즉시 순간이동, 짧은 조준 윈도우+슬로우, 아래 릴리즈=대시, 가시 레이캐스트 차단, DASH_STRIKE 제거)

### 잡기 탐색 로직
```
1. Z키 홀드 → 플레이어 중심 원형 범위(GRAB_RANGE) 내 NTT/적 탐색
2. 가장 가까운 대상 선택
3. 레이캐스트 실행
   - 장애물 판정: WALL 레이어만 막힘 (GRABABLE은 통과)
   - 장애물 없으면 초록색, 있으면 빨간색 표시
4. 장애물 있으면 → 다음으로 가까운 대상 재탐색
5. Z키 뗌 시: 초록색이면 GRAB_READY, 빨간/없음이면 NORMAL 복귀
6. GRAB_SEEKING 중 대시 입력 → GRAB_SEEKING 해제 + DASH 전환 (웅크리기 해제와 동일한 원리)
```

### GRAB_ACTIVE 물리 규칙
```python
# 플레이어 자체 중력 무시, NTT 중력만 따라감
vy = ntt.vy
vx = vx_input + ntt.vx
```

### 릴리즈 로직 (방향키 기준)
```
위 / 위 대각선  → 월바운스
좌 / 우         → 슈퍼 대시
아래 + 좌/우    → 하이퍼 대시
방향키 없음     → 일반 점프
```
릴리즈 후: 대시 1회 충전, 슈퍼/하이퍼/월바운스 발동 시 대시 횟수 소모 X

### 릴리즈 시 밀쳐지기
```
플레이어 릴리즈 방향 반대로 대상 밀쳐짐
일반 NTT      → 밀쳐지고 PUSH_RETURN_TIME 후 origin 복귀
일반 적 HP1   → 밀쳐지다가 파괴
강화 적 HP2+  → 밀쳐지고 INVINCIBLE_TIME 후 origin 복귀
벽 충돌 시    → 즉시 멈추고 복귀 타이머 시작
```

### [재설계] 잡기 시스템 최신 사양 (2026-06-01, 위 초기안 대체)
```
[입력 모델]
- Z 누름(엣지) → 짧은 조준 윈도우(GRAB_AIM_TIME=12f) + 슬로우모션(GRAB_SLOW_FACTOR=3, 1/3 속도) 시작
- 윈도우 동안: 일반 이동 유지(걷기/점프/대시 가능, 중력O), 범위(GRAB_RANGE=110) 표시
- 범위+시야 통과 대상 있으면 → 즉시 GRAB_ACTIVE(순간이동), 슬로우 종료
- 대상 없으면 윈도우 만료 시 자동 취소(NORMAL) — Z 홀드로는 못 버팀(엣지 필요, 캠핑 방지)
- 가장 가까운 1개만 일직선으로 표시

[레이캐스트 차단]
- 벽(WALL) + 가시(HAZARD) 둘 다 시야 차단 → 가시 너머 대상 잡기 불가 (GRABABLE은 통과)

[GRAB_ACTIVE]
- 잡은 동안만 얼음(이동불가·중력무시) + 대상에 anchor. 조준(SEEKING) 중엔 자유 이동.
- 대상이 바닥 등 솔리드에 박혀 있으면 순간이동 시 발을 솔리드 위로 스냅(끼임 사망 방지)
- 잡기 중엔 점프패드/스프링 트리거 스킵(NTT 겹침 무한잡기 방지)

[릴리즈 (방향키 기준)]
- 위/위대각  → 월바운스
- 좌/우      → 슈퍼
- 아래(±좌우) → 대시 (하이퍼 아님 — 그 방향 실제 대시 발동)
- 무입력     → 일반 점프
- 공통: 대시 1회 충전, 대상 밀쳐냄

[대상별]
- 일반 NTT: 밀쳐지고 복귀(파괴X)
- 줄 NTT(RopeNTT): 상시 진자, 잡으면 함께 스윙, 릴리즈 시 진자 접선속도 가산, 파괴 불가
- 일반 적 HP1: 릴리즈 시 파괴 → RESPAWN_TIME 후 부활
- 강화 적 HP2+: 1타 생존+무적(INVINCIBLE_TIME)·복귀, HP0 파괴

[제거됨]
- DASH_STRIKE(벽력일섬): 구현했다가 제거(2026-06-01). enum 값만 예약.
```

---

## 적 / NTT 시스템 (Phase 3)

### 적 종류
```
일반 적 (HP 1) — Phase 3 초반
  → 단일 히트박스, 방향 구분 없음
  → 어디서든 그랩/DASH_STRIKE 가능
  → 레이어: GRABABLE 하나만 사용
  → 그랩/DASH_STRIKE 1회 → 파괴 → RESPAWN_TIME 후 재생성

강화 적 (HP 2+) — Phase 3 초반
  → 단일 히트박스, 방향 구분 없음
  → 피격 시 HP 감소 → INVINCIBLE_TIME 동안 투명
  → HP 0 → 파괴 → RESPAWN_TIME 후 재생성

고급 적 (방향별 판정) — Phase 4 이후 추가
  → 히트박스 방향별 분리:
     grab_rect   = 잡기 가능 영역 (예: 위쪽)
     wall_rect   = 막히는 영역 / 보호막 (예: 옆쪽)
     hazard_rect = 닿으면 죽는 영역 (예: 아래쪽)
  → Phase 3에서 미리 구현 금지
```

### NTT 종류
```
일반 NTT
  → 잡기 가능, 중력 받아 낙하
  → 릴리즈 후 INVINCIBLE_TIME 동안 재잡기 불가

줄 NTT (샹들리에형)
  → 천장 고정점에 줄로 매달림
  → 잡으면 진자 운동 시작
  → 릴리즈 시 진자 속도 기반으로 슈퍼/하이퍼/월바운스 발동
  → 파괴 불가 (퍼즐 영구 요소)
```

### 줄 NTT 물리
```python
angular_vel += -(ROPE_GRAVITY / ROPE_LENGTH) * sin(angle)
angle += angular_vel
ntt.x = pivot.x + sin(angle) * ROPE_LENGTH
ntt.y = pivot.y + cos(angle) * ROPE_LENGTH

# 릴리즈 시
vx_external = angular_vel * ROPE_LENGTH * cos(angle)
vy = angular_vel * ROPE_LENGTH * -sin(angle)
```

---

## 움직이는 발판 구조

```python
class MovingPlatform(Solid):
    # vx, vy는 Solid에서 상속
    origin: Vector2      # 원래 위치
    target: Vector2      # 트리거 시 이동할 위치
    speed: float         # 이동 속도
    return_speed: float  # 원위치 복귀 속도
    triggered: bool      # 플레이어 탑승 여부
```

### 발판 동작 방식
```
기본 상태: origin 위치에서 정지
플레이어 탑승 → triggered = True → target으로 이동
플레이어 이탈 → triggered = False → origin으로 서서히 복귀
```

### 발판 관성 전달
```python
# 플레이어가 발판 위에서 점프 시
vx_external += platform.vx * PLATFORM_INERTIA_X
vy += platform.vy * PLATFORM_INERTIA_Y
# 전달된 외적 속도는 AIR_FRICTION으로 감쇠
```

### 발판 끼임 사망 (Crush Death) — 셀레스트식 밀기+핀

```
모델: 옵션 B (셀레스트식 Solid push + 핀 판정)
판정 시점: 발판 이동 직후 + 플레이어 자체 move 이전 (속도 비의존)

1) 캐리 케이스 (플레이어가 발판 위 탑승 중)
   - 발판 델타(dx, dy)만큼 플레이어를 함께 이동
   - 이동 후 다른 솔리드와 겹치면(_pinned) → crushed (벽/천장으로 캐리됨)

2) 밀기 케이스 (탑승 아님 + 발판이 플레이어와 겹침)
   - 발판 진행 방향으로 플레이어를 발판 밖으로 밀어냄 (_push_player)
       dx>0 → 우로,  dx<0 → 좌로,  dy>0 → 아래로,  dy<0 → 위로
   - 밀어낸 뒤 반대편 솔리드와 여전히 겹치면(_pinned) → crushed

핵심: 밀어낼 공간이 있으면 회피(생존), 반대편 솔리드에 막혀 핀되면 사망.
      4방향(좌/우/위/아래) 모두 동일 코드로 처리.
crushed 플래그 True → 낙사와 동일하게 _respawn().
```

---

## 천장 밀착 로직

```python
ceiling_stick_frames = min(
    abs(vy_before_collision) // CEILING_STICK_DIVISOR,
    MAX_CEILING_STICK_FRAMES
)
vy = 0
# 밀착 프레임 동안 중력 적용 안 함, 소진 후 재개
```

---

## 충돌 레이어 시스템

### 레이어 정의
```python
class Layer(Enum):
    WALL      # 막히는 레이어 (타일맵, Solid)
    HAZARD    # 닿으면 죽는 레이어 (가시, 톱니, 투사체)
    GRABABLE  # 잡기 가능한 레이어 (NTT, 적)
    PLAYER    # 플레이어 레이어
```

### 레이어 간 충돌 규칙
```
PLAYER   ↔ WALL     → 막힘
PLAYER   ↔ HAZARD   → 플레이어 사망
PLAYER   ↔ GRABABLE → 통과 (잡기만 가능)
GRABABLE ↔ WALL     → 막힘
GRABABLE ↔ HAZARD   → 무시
투사체   ↔ WALL     → 투사체 소멸
투사체   ↔ GRABABLE → 통과 (NTT/적 통과)
투사체   ↔ PLAYER   → 플레이어 사망
```

### 히트박스 규칙
- 히트박스(rect)는 스프라이트(sprite_rect)보다 약간 작게 설정
- 디버그 모드에서 히트박스 시각화 가능하도록 구현

### 엔티티 업데이트 순서 (매 프레임)
```
1. Solid (MovingPlatform) 이동
2. Projectile 이동
3. Enemy/NTT 이동 + AI 업데이트
4. Player 이동
5. Trigger 충돌 체크 (JumpPad, Spring, Hazard)
6. 카메라 업데이트
7. 렌더링
```

---

## 구현 금지 사항

- vx_input과 vx_external을 하나의 변수로 합치는 것
- 상태머신 없이 if/else로 물리 상태 판단하는 것
- settings.py 없이 수치를 코드에 직접 작성하는 것
- 이전 Phase 완료 전 다음 Phase 코드 작성하는 것
- 원본 파일(PLANNING.md, CLAUDE.md) 기존 내용을 임의로 수정/삭제하는 것
