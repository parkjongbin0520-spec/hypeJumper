"""적 — 잡기/스트라이크로 처치, 파괴 후 재생성. Enemy(HP1)·ArmoredEnemy(HP2+). (Phase 3C-2)"""

import pygame

import settings as S
from src.layer import Layer
from src.entities.actor import Actor


def _approach(value, target, amount):
    """value를 target 쪽으로 amount만큼 다가가게 한 뒤 반환 (오버슛 없음)."""
    if value < target:
        return min(value + amount, target)
    return max(value - amount, target)


class Enemy(Actor):
    """잡기 가능한 적 — NTT와 동일한 잡기 인터페이스 + HP/피격/파괴/리스폰. 기본 HP1."""

    def __init__(self, x, y, hp=1):
        """위치·원점·HP·잡힘/무적/파괴 상태를 초기화하고 GRABABLE 레이어로 설정."""
        super().__init__(x, y, S.ENEMY_WIDTH, S.ENEMY_HEIGHT)
        self.layer = Layer.GRABABLE
        self.origin = (float(x), float(y))
        self.max_hp = hp
        self.hp = hp
        self.grabbed = False          # 플레이어가 잡고 있는지 (GRAB_ACTIVE)
        self.destroyed = False        # 파괴(숨김·충돌X) 상태
        self.respawn_timer = 0        # 파괴 후 재생성까지 남은 프레임
        self.invincible_timer = 0     # 피격 후 무적(재잡기 불가·투명) 남은 프레임
        self.push_timer = 0           # 밀쳐진 후 복귀 시작까지 남은 프레임
        self.returning = False        # origin 복귀 중

    # ── 잡기 인터페이스 (NTT와 동일 시그니처) ───────────────────
    def grabbable(self):
        """현재 잡을 수 있는 상태인지 (파괴/무적 중이면 불가)."""
        return (not self.destroyed) and self.invincible_timer <= 0

    def on_grab(self):
        """잡히는 순간 — 밀쳐짐/복귀를 끄고 속도를 0으로."""
        self.grabbed = True
        self.push_timer = 0
        self.returning = False
        self.vx_external = 0.0
        self.vy = 0.0

    def on_release(self, push_x, push_y):
        """릴리즈 — 발사 반대로 밀쳐지며 피격(HP 감소). HP0이면 파괴, 아니면 무적+복귀."""
        self.grabbed = False
        self.vx_input = 0.0
        self.vx_external = push_x * S.PUSH_SPEED
        self.vy = push_y * S.PUSH_SPEED
        self._take_hit()

    def _take_hit(self):
        """HP를 1 깎고, 0이면 파괴(리스폰 예약), 남으면 무적 프레임 + 복귀 예약."""
        self.hp -= 1
        if self.hp <= 0:
            self.destroyed = True
            self.respawn_timer = S.RESPAWN_TIME
        else:
            self.invincible_timer = S.INVINCIBLE_TIME
            self.push_timer = S.PUSH_RETURN_TIME

    # ── 업데이트 ────────────────────────────────────────────────
    def update(self, solids):
        """파괴 중이면 리스폰 카운트다운, 잡힌 중이면 정지, 그 외 무적·밀쳐짐·복귀 처리."""
        if self.destroyed:
            self.respawn_timer -= 1
            if self.respawn_timer <= 0:
                self._respawn()
            return
        if self.invincible_timer > 0:
            self.invincible_timer -= 1
        if self.grabbed:
            return                       # 플레이어가 위치를 고정(anchor)
        if self.push_timer > 0:
            self._update_pushed(solids)
        elif self.returning:
            self._update_returning()

    def _update_pushed(self, solids):
        """밀쳐진 동안 공기저항 감속 이동, 벽에 막히면 멈추고 타이머 끝나면 복귀로 전환."""
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
        """origin으로 PUSH_RETURN_SPEED만큼 복귀, 도달하면 종료."""
        ox, oy = self.origin
        self.x = _approach(self.x, ox, S.PUSH_RETURN_SPEED)
        self.y = _approach(self.y, oy, S.PUSH_RETURN_SPEED)
        if self.x == ox and self.y == oy:
            self.returning = False

    def _respawn(self):
        """원점에서 HP 가득 채워 재생성."""
        self.destroyed = False
        self.hp = self.max_hp
        self.x, self.y = self.origin
        self.vx_input = self.vx_external = self.vy = 0.0
        self.invincible_timer = 0
        self.push_timer = 0
        self.returning = False

    # ── 렌더 ────────────────────────────────────────────────────
    def _base_color(self):
        """HP에 따른 기본 색 (강화 적은 별색)."""
        return S.COLOR_ENEMY_ARMORED if self.max_hp > 1 else S.COLOR_ENEMY

    def draw(self, surface, offset=(0, 0)):
        """파괴 중엔 안 그리고, 무적 중엔 깜빡임 색으로 적을 렌더."""
        if self.destroyed:
            return
        if self.invincible_timer > 0 and (self.invincible_timer // 4) % 2 == 0:
            color = S.COLOR_ENEMY_HIT      # 무적 동안 깜빡임
        else:
            color = self._base_color()
        pygame.draw.rect(surface, color, self.rect.move(-offset[0], -offset[1]))


class ArmoredEnemy(Enemy):
    """강화 적 — 기본 HP2+, 피격 시 무적 프레임 후 복귀, HP0에 파괴 (Enemy 로직 재사용)."""

    def __init__(self, x, y, hp=S.ARMORED_ENEMY_HP):
        """HP를 강화 값으로 설정해 Enemy 초기화."""
        super().__init__(x, y, hp=hp)
