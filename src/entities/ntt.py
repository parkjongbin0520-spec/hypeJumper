"""잡기 대상 NTT — GRABABLE 레이어, 잡기/릴리즈/밀쳐짐·원위치 복귀 (Phase 3C-1 기본형)."""

import math

import pygame

import settings as S
from src import assets
from src.layer import Layer
from src.entities.actor import Actor


def _approach(value, target, amount):
    """value를 target 쪽으로 amount만큼 다가가게 한 뒤 반환 (오버슛 없음)."""
    if value < target:
        return min(value + amount, target)
    return max(value - amount, target)


class NTT(Actor):
    """잡기 가능한 기본 엔티티 — 고정형(중력 없음), 잡히면 플레이어가 위치를 고정한다."""

    def __init__(self, x, y):
        """위치·원점·잡힘/밀쳐짐 상태를 초기화하고 GRABABLE 레이어로 설정."""
        super().__init__(x, y, S.NTT_WIDTH, S.NTT_HEIGHT)
        self.layer = Layer.GRABABLE
        self.origin = (float(x), float(y))  # 밀쳐진 뒤 복귀할 원래 위치
        self.grabbed = False                # GRAB_ACTIVE 중 플레이어가 잡고 있는지
        self.push_timer = 0                 # 밀쳐진 후 복귀 시작까지 남은 프레임
        self.returning = False              # origin 복귀 중 여부

    def on_grab(self):
        """플레이어가 잡는 순간 — 밀쳐짐/복귀 상태를 끄고 속도를 0으로."""
        self.grabbed = True
        self.push_timer = 0
        self.returning = False
        self.vx_external = 0.0
        self.vy = 0.0

    def on_release(self, push_x, push_y):
        """릴리즈 순간 — 플레이어 발사 반대 방향(push_x/y, -1~1)으로 밀쳐지고 복귀 타이머 시작."""
        self.grabbed = False
        self.vx_input = 0.0
        self.vx_external = push_x * S.PUSH_SPEED
        self.vy = push_y * S.PUSH_SPEED
        self.push_timer = S.PUSH_RETURN_TIME
        self.returning = False

    def update(self, solids):
        """잡힌 상태면 정지(플레이어가 고정), 밀쳐졌으면 감속 이동, 그 후 origin으로 복귀."""
        if self.grabbed:
            return                           # 플레이어가 매 프레임 위치를 덮어씀(anchor)
        if self.push_timer > 0:
            self._update_pushed(solids)
        elif self.returning:
            self._update_returning()

    def _update_pushed(self, solids):
        """밀쳐진 동안 공기저항으로 감속 이동, 벽에 막히면 멈추고 타이머 종료 시 복귀로 전환."""
        self.vx_input = 0.0
        self.vx_external *= S.AIR_FRICTION
        self.vy *= S.AIR_FRICTION
        self.move(solids)
        if self.collisions["left"] or self.collisions["right"]:
            self.vx_external = 0.0
        if self.collisions["up"] or self.collisions["down"]:
            self.vy = 0.0
        self.push_timer -= 1
        if self.push_timer <= 0:
            self.returning = True

    def _update_returning(self):
        """origin 좌표로 PUSH_RETURN_SPEED만큼 서서히 복귀, 도달하면 복귀 종료."""
        ox, oy = self.origin
        self.x = _approach(self.x, ox, S.PUSH_RETURN_SPEED)
        self.y = _approach(self.y, oy, S.PUSH_RETURN_SPEED)
        if self.x == ox and self.y == oy:
            self.returning = False

    def draw(self, surface, offset=(0, 0)):
        """NTT 스프라이트(있으면)로, 없으면 사각형으로 렌더 (잡힌 동안은 밝게)."""
        color = S.COLOR_GRAB_OK if self.grabbed else S.COLOR_NTT
        assets.blit_or_rect(surface, "ntt", self.rect, color, offset)


class RopeNTT(NTT):
    """줄 NTT (샹들리에형) — 천장 pivot에 매달려 상시 진자 운동, 파괴 불가. 릴리즈 시 진자 속도 전달."""

    def __init__(self, pivot_x, pivot_y, start_angle=S.ROPE_START_ANGLE, length=S.ROPE_LENGTH):
        """피벗·각도·각속도·줄 길이를 설정하고 진자 위치에서 NTT를 초기화."""
        self.pivot = (float(pivot_x), float(pivot_y))
        self.angle = start_angle          # 줄과 수직선이 이루는 각 (라디안)
        self.angular_vel = 0.0            # 각속도
        self.length = length
        x = pivot_x + math.sin(start_angle) * length - S.NTT_WIDTH / 2
        y = pivot_y + math.cos(start_angle) * length - S.NTT_HEIGHT / 2
        super().__init__(x, y)

    def on_release(self, push_x, push_y):
        """줄은 밀쳐지지 않음(영구 퍼즐 요소) — 잡힘만 해제하고 진자는 계속."""
        self.grabbed = False

    def release_velocity(self):
        """현재 진자 접선속도(vx, vy)를 반환 — 릴리즈 시 플레이어에 가산."""
        vx = self.angular_vel * self.length * math.cos(self.angle)
        vy = self.angular_vel * self.length * -math.sin(self.angle)
        return vx, vy

    def update(self, solids):
        """잡혀도 진자 운동을 계속 갱신 (플레이어가 anchor로 따라 흔들림)."""
        self._swing()

    def _swing(self):
        """진자 공식으로 각속도·각도·위치를 갱신 (PLANNING 공식)."""
        self.angular_vel += -(S.ROPE_GRAVITY / self.length) * math.sin(self.angle)
        self.angle += self.angular_vel
        px, py = self.pivot
        self.x = px + math.sin(self.angle) * self.length - self.width / 2
        self.y = py + math.cos(self.angle) * self.length - self.height / 2

    def draw(self, surface, offset=(0, 0)):
        """피벗에서 NTT까지 줄을 긋고 NTT 본체를 렌더."""
        px, py = self.pivot
        cx, cy = self.center
        pygame.draw.line(surface, S.COLOR_ROPE_LINE,
                         (int(px - offset[0]), int(py - offset[1])),
                         (int(cx - offset[0]), int(cy - offset[1])), 2)
        color = S.COLOR_GRAB_OK if self.grabbed else S.COLOR_ROPE_NTT
        assets.blit_or_rect(surface, "rope_ntt", self.rect, color, offset)
