"""플레이어 본체 — 상태머신, 통합 버퍼, NORMAL 이동 로직 (Phase 1)."""

# ════════════════════════════════════════════════════════════════
#  임포트
# ════════════════════════════════════════════════════════════════
import math
from enum import Enum, auto

import pygame

import settings as S
from src import assets
from src import audio
from src.entities.actor import Actor


# ════════════════════════════════════════════════════════════════
#  유틸
# ════════════════════════════════════════════════════════════════
def approach(value, target, amount):
    """value를 target 쪽으로 amount만큼 다가가게 한 뒤 반환 (오버슛 없음)."""
    if value < target:
        return min(value + amount, target)
    return max(value - amount, target)


# ════════════════════════════════════════════════════════════════
#  입력 스냅샷 — main.py가 pygame 키 상태를 담아 Player.update에 전달
# ════════════════════════════════════════════════════════════════
class PlayerInput:
    """한 프레임의 입력 상태를 담는 단순 컨테이너 (테스트 시 직접 생성 가능)."""

    def __init__(self, left=False, right=False, up=False, down=False,
                 jump_pressed=False, jump_held=False, dash_pressed=False,
                 grab_pressed=False, grab_held=False):
        """방향키/점프(눌린 순간·유지)/대시/잡기 입력을 초기화."""
        self.left = left              # 왼쪽 방향키
        self.right = right            # 오른쪽 방향키
        self.up = up                  # 위 방향키
        self.down = down              # 아래 방향키 (웅크리기/패스트폴)
        self.jump_pressed = jump_pressed  # 이번 프레임에 점프키를 새로 누름 (엣지)
        self.jump_held = jump_held        # 점프키 유지 중 (가변 점프 높이용)
        self.dash_pressed = dash_pressed  # 이번 프레임에 대시키를 새로 누름 (엣지)
        self.grab_pressed = grab_pressed  # 이번 프레임에 잡기키(Z)를 새로 누름 (엣지)
        self.grab_held = grab_held        # 잡기키(Z) 유지 중 (조준/잡기 유지용)


# ════════════════════════════════════════════════════════════════
#  플레이어 상태 — enum 전체를 미리 정의 (Phase 1은 NORMAL만 구현)
# ════════════════════════════════════════════════════════════════
class PlayerState(Enum):
    """플레이어 상태 전체 정의 — NORMAL 외에는 후속 Phase에서 로직 추가."""
    NORMAL = auto()        # 걷기+점프+월슬라이드 (웅크리기는 is_ducking 플래그)
    DASH = auto()          # 일반 대시 (Phase 2)
    DASH_STRIKE = auto()   # 대시 중 잡기, 벽력일섬 (Phase 3)
    GRAB_SEEKING = auto()  # Z홀드 조준 중 (Phase 3)
    GRAB_READY = auto()    # 잡기 가능 확인, 재입력 대기 (Phase 3)
    GRAB_ACTIVE = auto()   # 순간이동 완료, NTT와 이동 중 (Phase 3)


# ════════════════════════════════════════════════════════════════
#  통합 버퍼 — 코요테/입력 버퍼 4종을 동일 구조로 사용
# ════════════════════════════════════════════════════════════════
class InputBuffer:
    """프레임 카운트다운식 버퍼 (코요테 타임/입력 버퍼 공통)."""

    def __init__(self, max_frames):
        """버퍼 최대 프레임 수를 설정하고 비활성 상태로 시작."""
        self.frames = 0
        self.max_frames = max_frames

    def set(self):
        """버퍼를 최대치로 채워 활성화."""
        self.frames = self.max_frames

    def tick(self):
        """매 프레임 1 감소 (0 미만으로는 내려가지 않음)."""
        if self.frames > 0:
            self.frames -= 1

    def is_active(self):
        """버퍼가 아직 살아있는지 여부."""
        return self.frames > 0

    def consume(self):
        """조건 충족으로 사용 시 즉시 소멸."""
        self.frames = 0


# ════════════════════════════════════════════════════════════════
#  플레이어
# ════════════════════════════════════════════════════════════════
class Player(Actor):
    """플레이어 캐릭터 — Actor 상속, 상태머신과 NORMAL 무브먼트 보유."""

    # ── 초기화 ──────────────────────────────────────────────────
    def __init__(self, x, y):
        """위치·상태·버퍼·플래그를 초기화."""
        super().__init__(x, y, S.PLAYER_WIDTH, S.NORMAL_HITBOX_HEIGHT)
        self.state = PlayerState.NORMAL
        # 통합 버퍼 4종
        self.ground_coyote = InputBuffer(S.COYOTE_TIME)
        self.wall_coyote = InputBuffer(S.WALL_COYOTE_TIME)
        self.jump_buffer = InputBuffer(S.JUMP_BUFFER)
        self.wall_jump_buffer = InputBuffer(S.WALL_JUMP_BUFFER)
        # 프레임 상태 플래그
        self.on_ground = False
        self.on_wall = False
        self.wall_dir = 0          # 현재 접촉 벽 방향 (-1 좌, +1 우)
        self.last_wall_dir = 0     # 마지막 접촉 벽 (월 코요테용)
        self.facing = 1            # 바라보는 방향 (-1 좌, +1 우) — 스프라이트 좌우반전용
        self.wall_sliding = False
        self.fast_fall = False
        self.is_ducking = False
        self.ceiling_stick = 0     # 천장 밀착 남은 프레임
        self.near_ground = False   # 바닥 근접 여부 (점프 종류 판정용)
        self._prev_on_ground = False  # 직전 프레임 접지 여부 (착지음 엣지 감지용)
        self._anim_t = 0              # 애니메이션 클럭 (draw마다 증가, 렌더 전용)
        self.wj_input_lock = False # 월 점프 후 '벽 쪽' 입력 무시 활성 (정점까지)
        self.ride_vx = 0.0         # 현재 탑승한 발판의 수평 속도 (관성 전달용)
        self.ride_vy = 0.0         # 현재 탑승한 발판의 수직 속도 (관성 전달용)
        self.crushed = False       # 움직이는 발판에 솔리드로 밀려 핀(끼임)당했는지 (사망 신호)
        # 대시 (Phase 2)
        self.dashes = S.MAX_DASHES # 남은 대시 충전 횟수
        self.dash_timer = 0        # 대시 지속 남은 프레임
        self.dash_dir = (0, 0)     # 대시 입력 방향 (-1/0/1, 종료 처리 분기용)
        self.dash_vx = 0.0         # 대시 고정 수평 속도 (정규화 × DASH_SPEED)
        self.dash_vy = 0.0         # 대시 고정 수직 속도
        # 고급 무브먼트 (Phase 2.5)
        self.dash_jump_buffer = InputBuffer(S.AUTO_JUMP_BUFFER)  # 대시 직후 슈퍼/하이퍼 점프 허용 창
        self.wall_bounce_buffer = InputBuffer(S.WALL_BOUNCE_BUFFER)  # 월바운스 입력 버퍼(점프 후 벽 닿으면 자동)
        self.wall_bounce_down = False  # 버퍼된 월바운스가 대각(아래입력)인지 기억
        self.last_dash_dir = (0, 0)  # 마지막 대시 방향 (대시 종료 후에도 유지, 슈퍼/하이퍼 판정)
        self.wall_near_dir = 0       # 월바운스용 벽 근접 방향 (-1 좌, +1 우, 0 없음) — 버퍼 범위
        self.hang_timer = 0          # 테크 후 체공(부유) 남은 프레임 (슈퍼/하이퍼/월바운스 공통)
        self.hang_grav = 1.0         # 체공 중 적용할 중력 배율 (테크별로 설정)
        self.hang_descend_only = False  # True면 하강(vy>=0)에만 체공 적용 → 점프 높이 안 올라감
        self.last_tech = ""          # 마지막 발동 테크 이름 (HUD 피드백용)
        self.tech_flash = 0          # 테크 이름 표시 남은 프레임
        self.launch_lock = 0         # 스프링 등 외력 발사 후 입력 잠금 남은 프레임
        self.launch_lock_dir = 0     # 발사 방향(+1/-1) — 잠금 중 외력이 밀어내는 방향
        # 잡기 (Phase 3C)
        self.grab_target = None      # 현재 조준/잡은 NTT (없으면 None)
        self.grab_ok = False         # 조준 대상이 장애물 없이 잡기 가능한지 (초록/빨강)
        self.grab_timer = 0          # GRAB_ACTIVE 남은 프레임 (MAX_GRAB_TIME)
        self.grab_ready_timer = 0    # GRAB_READY 유지 남은 프레임 (재입력 대기)
        self.aim_timer = 0           # 조준 윈도우 남은 프레임 (Z 누름 후, 만료 시 취소)
        self.aim_slow = False        # 조준 윈도우 동안 슬로우모션 활성 (main이 읽음)

    # ── 메인 업데이트 ───────────────────────────────────────────
    def update(self, inp, solids, grabbables=None, hazards=None):
        """상태에 맞는 로직을 실행. grabbables=잡기 대상, hazards=레이캐스트 차단용 가시 rect."""
        if grabbables is None:
            grabbables = []
        if hazards is None:
            hazards = []
        if self.state == PlayerState.NORMAL:
            self._update_normal(inp, solids, grabbables, hazards)
        elif self.state == PlayerState.DASH:
            self._update_dash(inp, solids)
        elif self.state in (PlayerState.GRAB_SEEKING, PlayerState.GRAB_ACTIVE):
            self._update_grabbing(inp, solids, grabbables, hazards)
        # DASH_STRIKE enum은 예약만 (기능 미사용)

    def _update_normal(self, inp, solids, grabbables, hazards):
        """NORMAL 한 프레임 — Z 누름 시 조준 윈도우(슬로우) 진입, 아니면 일반 이동."""
        if inp.grab_pressed:                 # Z 누름(엣지) → 짧은 조준 윈도우 + 슬로우 시작
            self.state = PlayerState.GRAB_SEEKING
            self.aim_timer = S.GRAB_AIM_TIME
            self.aim_slow = True
            self._update_grabbing(inp, solids, grabbables, hazards)
            return
        self._normal_movement(inp, solids)

    def _normal_movement(self, inp, solids):
        """일반 이동 파이프라인 — 대시 트리거 우선, 그 외 입력→이동→충돌 순서."""
        if self._dash_triggered(inp) and self._start_dash(inp):
            self._update_dash(inp, solids)   # 트리거 프레임에서 바로 대시 시작
            return
        self._pre_state(inp)         # 직전 접촉 정보로 슬라이드/패스트폴 판정
        self._update_duck(inp)       # 웅크리기 진입/해제 (히트박스 높이)
        self._update_horizontal(inp) # 수평 속도 (입력/외적)
        self._handle_jump(inp)       # 점프/월점프 (버퍼+코요테)
        self._apply_vertical(inp)    # 중력/낙하상한/천장밀착
        self.move(solids)            # Actor 축분리 충돌 → self.collisions 갱신
        self._resolve_landing()      # 착지/천장 처리 (vy 정리)
        self._update_contacts(solids)  # 1px 프로브로 지상/벽 접촉 갱신 + 코요테 충전
        self._tick_buffers()         # 버퍼 카운트다운

    # ── 상태 판정 ───────────────────────────────────────────────
    def _pre_state(self, inp):
        """직전 프레임 접촉 플래그로 월슬라이드/패스트폴 여부만 판정 (접촉 갱신은 프레임 끝)."""
        pressing_wall = (inp.right and self.wall_dir == 1) or (inp.left and self.wall_dir == -1)
        descending = self.vy > 0
        self.fast_fall = (not self.on_ground) and descending and inp.down
        self.wall_sliding = self.on_wall and descending and pressing_wall and not self.fast_fall

    def _update_contacts(self, solids):
        """이동 후 1px 프로브로 지상/벽 접촉 + 바닥 근접(near_ground)을 판정하고 코요테 충전."""
        rect = self.rect
        self.on_ground = rect.move(0, 1).collidelist(solids) != -1     # 바로 아래 1px 검사
        # 하강 중 바닥이 NEAR_GROUND_DISTANCE 안에 있으면 근접 (점프=일반점프 판정용)
        self.near_ground = self.vy >= 0 and rect.move(0, S.NEAR_GROUND_DISTANCE).collidelist(solids) != -1
        if self.on_ground:
            self.ground_coyote.set()
            self.wj_input_lock = False             # 착지하면 입력 잠금 해제
            self.dashes = S.MAX_DASHES             # 착지/지상이면 대시 재충전 (지상 대시 즉시 충전 포함)
            self.hang_timer = 0                    # 착지 시 체공 종료
        touch_left = rect.move(-1, 0).collidelist(solids) != -1
        touch_right = rect.move(1, 0).collidelist(solids) != -1
        self.on_wall = (not self.on_ground) and (touch_left or touch_right)
        self.wall_dir = -1 if touch_left else (1 if touch_right else 0)
        if self.on_wall:
            self.wall_coyote.set()
            self.last_wall_dir = self.wall_dir
        self.wall_near_dir = self._probe_wall_near(solids)  # 월바운스용 넓은 벽 감지(버퍼)
        if self.on_ground and not self._prev_on_ground:     # 공중→지상 전이 = 착지음(1회)
            audio.play("land")
        self._prev_on_ground = self.on_ground

    def _probe_wall_near(self, solids):
        """월바운스 버퍼 — WALL_BOUNCE_RANGE 안에 벽이 있으면 그 방향(-1/+1) 반환."""
        rect = self.rect
        left_zone = pygame.Rect(rect.x - S.WALL_BOUNCE_RANGE, rect.y, S.WALL_BOUNCE_RANGE, rect.height)
        right_zone = pygame.Rect(rect.right, rect.y, S.WALL_BOUNCE_RANGE, rect.height)
        if left_zone.collidelist(solids) != -1:
            return -1
        if right_zone.collidelist(solids) != -1:
            return 1
        return 0

    # ── 수평 이동 (Approach 방식) ───────────────────────────────
    def _update_horizontal(self, inp):
        """지상/공중에 따라 입력 속도와 외적 속도를 Approach로 갱신."""
        if self.launch_lock > 0:          # 발사 입력 잠금: 입력 무시, 외력(vx_external)이 그대로 밀어냄
            self.vx_input = 0.0
            return
        direction = (1 if inp.right else 0) - (1 if inp.left else 0)
        if direction != 0:
            self.facing = direction        # 실제 좌우 입력이 있을 때만 바라보는 방향 갱신
        if self.on_ground:
            accel, decel = S.GROUND_ACCEL, S.GROUND_DECEL
            self.vx_external = approach(self.vx_external, 0, S.SPEED_REDUCE)  # 빠른 감속
        else:
            accel, decel = S.GROUND_ACCEL * S.AIR_MULT, S.GROUND_DECEL * S.AIR_MULT
            self.vx_external *= S.AIR_FRICTION  # 공기 저항식 감쇠
        if self.wj_input_lock:                  # 월 점프 후: 벽 쪽 '연속 홀드'만 정점까지 무시
            if self.vy >= 0 or direction != self.last_wall_dir:  # 정점 도달 또는 벽쪽 입력 릴리즈 → 해제
                self.wj_input_lock = False
            else:                               # 벽 쪽 계속 홀드 중 → 무시(확실히 떼짐)
                direction = 0
        if direction != 0:
            if self.vx_external * direction > S.PLAYER_MAX_SPEED:
                self.vx_input = approach(self.vx_input, 0, accel)            # 외적 초과: 추가 가속 불가
            else:
                self.vx_input = approach(self.vx_input, direction * S.PLAYER_MAX_SPEED, accel)
        else:
            self.vx_input = approach(self.vx_input, 0, decel)

    # ── 수직 / 중력 ─────────────────────────────────────────────
    def _apply_vertical(self, inp):
        """천장 밀착/월슬라이드/일반 중력을 분기 적용하고 낙하 상한을 건다."""
        if self.ceiling_stick > 0:        # 천장 밀착 중: 중력 정지
            self.ceiling_stick -= 1
            self.vy = 0
            return
        if self.wall_sliding:             # 월 슬라이드: 느린 종단속도로 cap
            self.vy = min(self.vy + S.WALL_SLIDE_GRAVITY, S.WALL_SLIDE_MAX_FALL)
            return
        apply_hang = self.hang_timer > 0 and not (self.hang_descend_only and self.vy < 0)
        self.vy += self._gravity_value(inp)   # 체공 중력 감소 적용
        if apply_hang:                        # 실제 적용된 프레임만 소진 (descend_only는 하강에서만)
            self.hang_timer -= 1
        max_fall = S.FAST_MAX_FALL if self.fast_fall else S.MAX_FALL_SPEED
        if self.vy > max_fall:            # 낙하만 상한 (상승 vy엔 상한 없음)
            self.vy = max_fall

    def _gravity_value(self, inp):
        """현재 상승/하강·버튼 유지·패스트폴 여부로 적용할 중력값을 선택 (체공 중 감소)."""
        if self.vy < 0:  # 상승 중
            g = S.GRAVITY_UP if inp.jump_held else S.GRAVITY_UP_RELEASE
        elif self.fast_fall:
            g = S.FAST_FALL_GRAVITY
        else:
            g = S.GRAVITY_DOWN
        if self.hang_timer > 0 and not (self.hang_descend_only and self.vy < 0):
            g *= self.hang_grav           # 체공 중 중력 감소 (descend_only면 상승 중엔 제외 → 높이 유지)
        return g

    # ── 점프 / 월 점프 ──────────────────────────────────────────
    def _handle_jump(self, inp):
        """점프 입력을 버퍼에 넣고, 지상/벽 조건이 맞으면 즉시 발동."""
        # 버퍼된 월바운스: 수직대시 점프를 벽 직전에 눌렀어도 벽에 닿는 순간 자동 발동
        if self.wall_bounce_buffer.is_active() and self.wall_near_dir != 0:
            self._do_wall_bounce(self.wall_bounce_down)
            self.wall_bounce_buffer.consume()
            self.dash_jump_buffer.consume()
            return
        if inp.jump_pressed:
            self.jump_buffer.set()
            self.wall_jump_buffer.set()
        # 대시 점프 창 활성 시 슈퍼/하이퍼/월바운스 우선 (일반 점프보다 먼저)
        if (self.jump_buffer.is_active() and self.dash_jump_buffer.is_active()
                and self._try_dash_tech(inp)):
            self.jump_buffer.consume()
            self.wall_jump_buffer.consume()
            self.dash_jump_buffer.consume()
            self.ground_coyote.consume()
            return
        # 수직대시 점프인데 아직 벽이 없음 → 월바운스 의도를 버퍼에 저장(곧 벽 닿으면 위 자동발동)
        if (self.jump_buffer.is_active() and self.dash_jump_buffer.is_active()
                and self.last_dash_dir[1] < 0 and self.wall_near_dir == 0):
            self.wall_bounce_buffer.set()
            self.wall_bounce_down = inp.down
            self.jump_buffer.consume()
            self.wall_jump_buffer.consume()
            return
        if self.jump_buffer.is_active() and (self.on_ground or self.ground_coyote.is_active() or self.near_ground):
            self._do_jump()
            self.jump_buffer.consume()
            self.wall_jump_buffer.consume()   # 한 번 누름이 월점프로 이어지지 않게
            self.ground_coyote.consume()
        elif self.wall_jump_buffer.is_active() and self._wall_jump_ready(inp):
            self._do_wall_jump()
            self.wall_jump_buffer.consume()
            self.jump_buffer.consume()        # 한 번 누름이 일반점프로 이어지지 않게
            self.wall_coyote.consume()

    def _wall_jump_ready(self, inp):
        """월 점프 조건 — 바닥 근처가 아니고, 공중 벽(또는 벽 코요테) 접촉. 방향키 무관."""
        if self.near_ground:           # 바닥 근처면 일반 점프 우선 (옆튐 방지)
            return False
        return self.on_wall or self.wall_coyote.is_active()

    def _do_jump(self):
        """일반 점프 — 웅크리기 해제 후, 탑승 발판 관성을 더해 상승 속도를 부여."""
        if self.is_ducking:
            self._set_height(S.NORMAL_HITBOX_HEIGHT)
            self.is_ducking = False
        self.vy = S.JUMP_SPEED + self.ride_vy * S.PLATFORM_INERTIA_Y   # 발판 수직 관성
        self.vx_external += self.ride_vx * S.PLATFORM_INERTIA_X        # 발판 수평 관성
        audio.play("jump")

    def _do_wall_jump(self):
        """월 점프 — 벽 반대로 외적 속도를 주고 위로 튕기며 잠시 벽 재부착을 잠금."""
        self.vy = S.WALL_JUMP_V
        self.vx_external = -self.last_wall_dir * S.WALL_JUMP_H
        self.vx_input = 0.0
        self.wj_input_lock = True   # 정점까지 '벽 쪽' 입력 무시 → 확실히 떼짐
        audio.play("walljump")

    # ── 대시 (Phase 2) ──────────────────────────────────────────
    def launch(self, vx_external=None, vy=None, refill_dash=True, lock_frames=0):
        """외력 발사(점프패드/스프링) — 지정 축 속도를 덮어쓰고 대시를 1회 충전. lock_frames>0이면 수평 입력 잠금."""
        self.state = PlayerState.NORMAL   # 대시 중이면 강제 종료(중력 재개)
        self.dash_timer = 0               # 대시 타이머 즉시 소진
        self.ceiling_stick = 0            # 천장 밀착 해제
        self.hang_timer = 0               # 체공(부유) 해제
        # 주의: 대시 점프창(dash_jump_buffer)·last_dash_dir은 보존 → 스프링/패드 거쳐도 슈퍼/하이퍼/월바운스 연결 유지
        #       (패드 중첩 폭점프는 JumpPad 재발동 쿨다운으로 방지)
        if vy is not None:                # 수직 발사 속도 (점프패드/위 스프링)
            self.vy = vy
        if vx_external is not None:       # 수평 발사 속도 (벽 스프링) — None이면 기존 관성 유지
            self.vx_external = vx_external
        if refill_dash:                   # 닿으면 대시 1회 충전 (스펙)
            self.dashes = S.MAX_DASHES
        if lock_frames > 0 and vx_external is not None:  # 수평 발사 → 입력 잠금(외력이 밀어냄)
            self.launch_lock = lock_frames
            self.launch_lock_dir = 1 if vx_external > 0 else -1
            self.vx_input = 0.0

    def _dash_triggered(self, inp):
        """대시 발동 조건 — 대시키 엣지 + 충전 있음 + 방향키 입력(없으면 미발동)."""
        has_dir = inp.left or inp.right or inp.up or inp.down
        return inp.dash_pressed and self.dashes > 0 and has_dir

    def _start_dash(self, inp):
        """8방향 정규화 벡터로 대시 속도를 고정하고 DASH 상태로 진입 (방향 없으면 실패)."""
        dx = (1 if inp.right else 0) - (1 if inp.left else 0)
        dy = (1 if inp.down else 0) - (1 if inp.up else 0)
        if dx == 0 and dy == 0:           # 방향 입력 없으면 대시 안 함
            return False
        length = math.hypot(dx, dy)       # 대각선 정규화 → 8방향 등속
        self.dash_vx = dx / length * S.DASH_SPEED
        self.dash_vy = dy / length * S.DASH_SPEED
        self.dash_dir = (dx, dy)
        self.last_dash_dir = (dx, dy)     # 슈퍼/하이퍼 판정용 (종료 후에도 유지)
        self.state = PlayerState.DASH
        self.dash_timer = S.DASH_TIME
        self.dashes -= 1
        audio.play("dash")
        if self.is_ducking:               # 웅크리기 중 대시 → 히트박스 복구
            self._set_height(S.NORMAL_HITBOX_HEIGHT)
            self.is_ducking = False
        return True

    def _update_dash(self, inp, solids):
        """대시 한 프레임 — 중력 무시, 고정 속도 등속 이동, 충돌·접촉·버퍼 갱신."""
        self.vx_input = 0.0
        self.vx_external = self.dash_vx   # 대시 속도로 고정 (중력/마찰 무시)
        self.vy = self.dash_vy
        self.dash_timer -= 1
        self.move(solids)
        self._dash_resolve_collisions()   # 벽/천장/바닥에 막히면 해당 축 속도 0
        self._update_contacts(solids)
        self.dash_jump_buffer.set()       # 대시 중 점프 창 유지 (종료 후 카운트다운)
        self._tick_buffers()
        if inp.jump_pressed and self._try_dash_tech(inp):  # 대시 중 점프 → 슈퍼/하이퍼/월바운스
            self.state = PlayerState.NORMAL
            return
        if self.dash_timer <= 0:
            self._end_dash()

    def _dash_resolve_collisions(self):
        """대시 중 충돌 시 막힌 축 속도를 0으로 (끼임 방지, 타이머는 계속)."""
        if self.collisions["left"] or self.collisions["right"]:
            self.vx_external = self.dash_vx = 0.0
        if self.collisions["up"] or self.collisions["down"]:
            self.vy = self.dash_vy = 0.0

    def _end_dash(self):
        """대시 종료 — 방향별 속도 처리 후 NORMAL 복귀."""
        dx, dy = self.dash_dir
        if dy > 0:                        # 아래 성분(대각 아래 포함) → 속도 유지 (하이퍼 트리거용)
            pass
        else:
            if dx != 0:                   # 수평 성분 → vx 급감속 컷
                self.vx_external = math.copysign(S.END_DASH_SPEED, dx)
            if dy < 0:                    # 위 성분 → vy 감속
                self.vy *= S.END_DASH_UP_MULT
        self.dash_jump_buffer.set()       # 대시 종료 시점에 창을 새로 채움 → 대시 후 AUTO_JUMP_BUFFER 프레임간 슈퍼/하이퍼/월바운스 허용(벽 한정 X)
        self.state = PlayerState.NORMAL

    # ── 고급 무브먼트 (Phase 2.5): 슈퍼 / 하이퍼 / 월바운스 ──────
    def _try_dash_tech(self, inp):
        """대시 점프 창에서 조건별 테크 발동 — 월바운스/슈퍼/하이퍼. 발동 시 True."""
        dx, dy = self.last_dash_dir
        if dy < 0 and self.wall_near_dir != 0:  # 위 성분 대시 중 벽 근접(버퍼) → 월바운스
            self._do_wall_bounce(inp.down)
            return True
        if self.on_ground or self.ground_coyote.is_active() or self.near_ground:
            if dy == 0 and dx != 0:       # 수평 대시 + 지상 → 슈퍼
                self._do_super_jump(inp)
                return True
            if dy > 0:                    # 대각/수직 아래 대시 + 지상 → 하이퍼
                self._do_hyper_jump(inp)
                return True
        return False

    def _boost_sign(self, inp, fallback_dx):
        """코너 부스트 — 점프 시점 방향키 부호 우선, 없으면 대시 방향 부호."""
        d = (1 if inp.right else 0) - (1 if inp.left else 0)
        if d != 0:
            return d
        return 1 if fallback_dx >= 0 else -1

    def _flash_tech(self, name):
        """발동 테크 이름을 HUD 표시용으로 기록하고 해당 테크 효과음 재생."""
        self.last_tech = name
        self.tech_flash = S.FPS // 2      # 0.5초간 표시
        audio.play(name.split("-")[0].lower())  # SUPER/HYPER/WALLBOUNCE(-DIAG→wallbounce)

    def _set_hang(self, time, grav, descend_only=False):
        """테크 체공(부유) 설정 — time 프레임 동안 중력×grav. descend_only면 하강에만 적용(높이 유지)."""
        self.hang_timer = time
        self.hang_grav = grav
        self.hang_descend_only = descend_only

    def _do_super_jump(self, inp):
        """슈퍼 대시 — 큰 수평 속도 + 일반 점프 높이 + 하강 체공(높이 동일, 구분은 부유로)."""
        sign = self._boost_sign(inp, self.last_dash_dir[0])
        self.vx_external = sign * S.SUPER_JUMP_H
        self.vx_input = 0.0
        self.vy = S.JUMP_SPEED
        self._set_hang(S.SUPER_HANG_TIME, S.SUPER_HANG_GRAV, descend_only=True)
        self._flash_tech("SUPER")

    def _do_hyper_jump(self, inp):
        """하이퍼 대시 — 더 큰 수평(×1.25) + 일반 점프 높이 + 하강 체공."""
        sign = self._boost_sign(inp, self.last_dash_dir[0])
        self.vx_external = sign * S.SUPER_JUMP_H * S.DUCK_SUPER_JUMP_X_MULT
        self.vx_input = 0.0
        self.vy = S.JUMP_SPEED * S.DUCK_SUPER_JUMP_Y_MULT
        self._set_hang(S.HYPER_HANG_TIME, S.HYPER_HANG_GRAV, descend_only=True)
        self._flash_tech("HYPER")

    def _do_wall_bounce(self, down):
        """월바운스 — 벽 반대로 크게 + 위(기본, 높고 길게) 또는 아래입력(down) 시 대각(수평 위주)."""
        self.vx_external = -self.wall_near_dir * S.SUPER_WALL_JUMP_H
        self.vx_input = 0.0
        if down:                          # 대각선 월바운스: 수평 위주 빠르게
            self.vy = S.WALL_BOUNCE_DIAG_Y
            self._flash_tech("WALLBOUNCE-DIAG")
        else:                             # 기본 월바운스: 강하게 위로 + 체공 부유
            self.vy = S.SUPER_WALL_JUMP_SPEED
            self._set_hang(S.WALL_BOUNCE_HANG_TIME, S.WALL_BOUNCE_HANG_GRAV)
            self._flash_tech("WALLBOUNCE")
        self.wj_input_lock = True         # 벽 재부착 방지

    # ── 잡기 (Phase 3C): Z홀드 즉시 순간이동 + 얼음(이동불가·중력무시) ──
    def _update_grabbing(self, inp, solids, grabbables, hazards):
        """잡은 상태(ACTIVE)는 홀드 유지·뗌 시 릴리즈, 조준 윈도우(SEEKING)는 만료 시 자동 취소."""
        if self.state == PlayerState.GRAB_ACTIVE and self._holding_target():
            if not inp.grab_held:                    # Z 뗌 → 릴리즈(테크)
                self._end_grab(inp)
                return
            self.vx_input = self.vx_external = self.vy = 0.0   # 잡은 상태만 얼음
            self.grab_timer -= 1                     # 대상에 고정 + 타이머
            self._anchor_to(self.grab_target, solids)
            if self.grab_timer <= 0:
                self._end_grab(inp)                  # 시간 초과 → 강제 릴리즈
            return
        self._normal_movement(inp, solids)           # 조준 윈도우 중엔 일반 이동 유지
        if self.state not in (PlayerState.GRAB_SEEKING, PlayerState.GRAB_ACTIVE):
            return                                   # 대시 등으로 상태 바뀌면 조준 종료
        blockers = solids + hazards                  # 레이캐스트는 벽+가시 모두로 막힘
        self.grab_target, self.grab_ok = self._find_grab_target(grabbables, blockers)
        if self.grab_target is not None and self.grab_ok:
            self._start_grab_active(solids)          # 대상 확보 → 즉시 순간이동(얼음 시작)
            return
        self.aim_timer -= 1                           # 대상 없음/막힘 → 윈도우 카운트다운
        if self.aim_timer <= 0:                       # 만료 → 잡기 취소(허공 Z), 슬로우 해제
            self.state = PlayerState.NORMAL
            self.grab_target = None
            self.aim_slow = False

    def _holding_target(self):
        """현재 잡고 있는 대상이 유효한지 여부."""
        return self.grab_target is not None and self.grab_target.grabbed

    def _start_grab_active(self, solids):
        """순간이동 — 대상 중심으로 이동(솔리드 박힘 보정) 후 GRAB_ACTIVE 진입."""
        target = self.grab_target
        target.on_grab()
        self._anchor_to(target, solids)
        self.vx_input = self.vx_external = self.vy = 0.0   # 잡는 순간 속도 정리(얼음 진입)
        self.aim_slow = False                              # 잡으면 조준 슬로우 종료
        self.state = PlayerState.GRAB_ACTIVE
        self.grab_timer = S.MAX_GRAB_TIME
        audio.play("grab")

    def _anchor_to(self, target, solids):
        """플레이어 중심을 대상 중심에 맞추되, 솔리드와 겹치면 위로 빼서 발을 올림(바닥 박힘 사망 방지)."""
        tcx, tcy = target.center
        self.x = tcx - self.width / 2
        self.y = tcy - self.height / 2
        idx = self.rect.collidelist(solids)
        if idx != -1:                                # 대상이 바닥 등에 박혀 겹치면 솔리드 위로 스냅
            r = self.rect
            r.bottom = solids[idx].top
            self.x, self.y = r.x, r.y

    def _end_grab(self, inp):
        """Z 뗌/시간초과 — 잡은 대상이 있으면 릴리즈(테크), 없으면 NORMAL 복귀."""
        self.aim_slow = False
        if self._holding_target():
            self._release_grab(inp)
        else:
            self.state = PlayerState.NORMAL
            self.grab_target = None

    def _find_grab_target(self, grabbables, blockers):
        """범위 내 가장 가까운 대상을 찾고, 장애물(벽/가시) 없는 첫 대상이면 (대상,True) 반환."""
        cx, cy = self.center
        cands = []
        for ntt in grabbables:
            tx, ty = ntt.center
            dist = math.hypot(tx - cx, ty - cy)
            if dist <= S.GRAB_RANGE:
                cands.append((dist, ntt))
        cands.sort(key=lambda c: c[0])
        for _, ntt in cands:                        # 가까운 순으로 시야 확인
            if self._has_los(ntt, blockers):
                return ntt, True                    # 장애물 없는 가장 가까운 대상
        if cands:
            return cands[0][1], False               # 있지만 전부 막힘 (빨강)
        return None, False

    def _has_los(self, target, blockers):
        """플레이어 중심→대상 중심 직선이 벽/가시(blockers)에 막히지 않으면 True (레이캐스트)."""
        cx, cy = self.center
        tx, ty = target.center
        line = (cx, cy, tx, ty)
        for b in blockers:
            if b.clipline(line):                    # 차단 rect와 교차 → 장애물 있음
                return False
        return True

    def _in_range(self, target):
        """대상이 GRAB_RANGE 안에 있는지 여부."""
        cx, cy = self.center
        tx, ty = target.center
        return math.hypot(tx - cx, ty - cy) <= S.GRAB_RANGE

    def _release_grab(self, inp):
        """릴리즈 — 위=월바운스 / 좌우=슈퍼 / 아래(±좌우)=대시 / 무입력=점프. 대상 밀쳐냄 + 대시 충전."""
        dx = (1 if inp.right else 0) - (1 if inp.left else 0)
        up, down = inp.up, inp.down
        target = self.grab_target
        self.grab_target = None
        self.dashes = S.MAX_DASHES                  # 릴리즈 후 대시 1회 충전
        audio.play("release")
        rvx, rvy = getattr(target, "release_velocity", lambda: (0.0, 0.0))()  # 줄 진자 접선속도
        if down:                                    # 아래/아래+좌우 → 대시 (그 방향으로 발사)
            target.on_release(-dx, -1)              # 대상은 위로 밀쳐짐
            self._start_dash(inp)                   # DASH 상태 진입(아래/대각아래 대시)
            return
        self.state = PlayerState.NORMAL
        self.last_dash_dir = (dx, -1 if up else 0)
        if up:                                      # 위/위대각 → 월바운스(상승+체공)
            self.vx_external = self._boost_sign(inp, 0) * S.SUPER_WALL_JUMP_H if dx != 0 else 0.0
            self.vx_input = 0.0
            self.vy = S.SUPER_WALL_JUMP_SPEED
            self._set_hang(S.WALL_BOUNCE_HANG_TIME, S.WALL_BOUNCE_HANG_GRAV)
            self._flash_tech("WALLBOUNCE")
            push_y = 1
        elif dx != 0:                               # 좌/우 → 슈퍼
            self._do_super_jump(inp)
            push_y = 0
        else:                                       # 입력 없음 → 일반 점프
            self._do_jump()
            push_y = 1
        self.vx_external += rvx                     # 줄 NTT면 진자 접선속도 가산(고정/적은 0)
        self.vy += rvy
        target.on_release(-dx, push_y)              # 대상은 플레이어 발사 반대로 밀쳐짐

    # ── 착지 / 천장 / 벽 충돌 처리 ──────────────────────────────
    def _resolve_landing(self):
        """이동 후 충돌 결과로 착지(vy=0)·천장 밀착·벽쪽 수평속도 0을 처리."""
        if self.collisions["down"]:
            self.vy = 0
        if self.collisions["up"]:
            self.ceiling_stick = int(min(abs(self.vy) / S.CEILING_STICK_DIVISOR,
                                         S.MAX_CEILING_STICK_FRAMES))
            self.vy = 0
        # 벽에 박으면 벽 쪽 수평 속도 0 (벽쪽 가속 누적 방지)
        if self.collisions["left"]:
            self.vx_input = max(self.vx_input, 0.0)
            self.vx_external = max(self.vx_external, 0.0)
        if self.collisions["right"]:
            self.vx_input = min(self.vx_input, 0.0)
            self.vx_external = min(self.vx_external, 0.0)

    # ── 웅크리기 ────────────────────────────────────────────────
    def _update_duck(self, inp):
        """지상+아래 입력(이동 입력 없음)일 때만 히트박스를 낮춤."""
        want_duck = self.on_ground and inp.down and not (inp.left or inp.right)
        if want_duck and not self.is_ducking:
            self._set_height(S.DUCK_HITBOX_HEIGHT)
            self.is_ducking = True
        elif not want_duck and self.is_ducking:
            self._set_height(S.NORMAL_HITBOX_HEIGHT)
            self.is_ducking = False

    def _set_height(self, new_height):
        """발밑(bottom)을 고정한 채 히트박스 높이를 변경."""
        self.y += self.height - new_height
        self.height = new_height

    # ── 버퍼 갱신 ───────────────────────────────────────────────
    def _tick_buffers(self):
        """지상/벽에서 떨어졌으면 코요테를, 입력 버퍼는 매 프레임 감소."""
        if not self.on_ground:
            self.ground_coyote.tick()
        if not self.on_wall:
            self.wall_coyote.tick()
        self.jump_buffer.tick()
        self.wall_jump_buffer.tick()
        self.dash_jump_buffer.tick()   # 대시 점프 창 카운트다운 (대시 종료 후 만료)
        self.wall_bounce_buffer.tick() # 월바운스 입력 버퍼 카운트다운
        if self.launch_lock > 0:       # 발사 입력 잠금 카운트다운
            self.launch_lock -= 1
        if self.tech_flash > 0:        # 테크 이름 표시 타이머
            self.tech_flash -= 1

    # ── 렌더 ────────────────────────────────────────────────────
    def draw(self, surface, offset=(0, 0)):
        """상태별 플레이어 스프라이트(있으면)로, 없으면 사각형으로 렌더. 조준 중이면 범위/대상 표시."""
        if self.state in (PlayerState.GRAB_SEEKING, PlayerState.GRAB_ACTIVE):
            self._draw_grab_aim(surface, offset)
        self._anim_t += 1
        assets.blit_or_rect(surface, self._animated(self._sprite_names()),
                            self.rect, S.COLOR_PLAYER, offset, flip=(self.facing < 0))

    def _animated(self, bases):
        """기본 이름 리스트의 첫 상태에 번호 프레임이 있으면 현재 프레임을 앞에 끼움 (없으면 그대로)."""
        frames = assets.frame_names(bases[0])
        if not frames:
            return bases                          # 번호 프레임 없음 → 단일 스프라이트/사각형 폴백
        idx = (self._anim_t // S.ANIM_FRAME_DUR) % len(frames)
        return [frames[idx]] + bases              # 현재 프레임 → (없으면 단일 → 사각형)

    def _sprite_names(self):
        """현재 상태로 표시할 스프라이트 이름 후보(우선순위) 반환 — 렌더 전용(물리 무관)."""
        if self.is_ducking:
            return ["player_duck", "player_idle"]
        if self.state == PlayerState.DASH:
            return ["player_dash", "player_idle"]
        if not self.on_ground:
            rise = self.vy < 0
            return (["player_jump", "player_idle"] if rise else ["player_fall", "player_idle"])
        if abs(self.vx) > 0.3:
            return ["player_run", "player_idle"]
        return ["player_idle"]

    def _draw_grab_aim(self, surface, offset):
        """조준 범위 원 + 가장 가까운 잡기 가능 대상에 일직선/하이라이트(초록/빨강) 렌더."""
        grabbable = self.grab_target is not None and self.grab_ok
        col = S.COLOR_GRAB_OK if grabbable else S.COLOR_GRAB_NO
        cx, cy = self.center
        pc = (int(cx - offset[0]), int(cy - offset[1]))
        pygame.draw.circle(surface, col, pc, S.GRAB_RANGE, 1)
        if self.grab_target is not None:
            tx, ty = self.grab_target.center
            if grabbable:                            # 잡을 수 있을 때 플레이어↔대상 일직선
                pygame.draw.line(surface, col, pc, (int(tx - offset[0]), int(ty - offset[1])), 2)
            tr = self.grab_target.rect.move(-offset[0], -offset[1]).inflate(4, 4)
            pygame.draw.rect(surface, col, tr, 2)
