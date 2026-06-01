"""씬 — 타일맵·플레이어·트리거(위험/체크포인트)를 관리하고 갱신/렌더/리스폰을 담당."""

import pygame

import settings as S
from src.layer import Layer
from src.player import Player
from src.tilemap import TileMap
from src.entities.trigger import Trigger


class Checkpoint(Trigger):
    """플레이어가 닿으면 리스폰 지점을 갱신하는 체크포인트 (PLAYER만 반응)."""

    def __init__(self, x, y, width, height):
        """PLAYER 대상 트리거로 초기화 (비활성 상태로 시작)."""
        super().__init__(x, y, width, height, [Layer.PLAYER])
        self.active = False

    def on_enter(self, actor, scene):
        """플레이어가 닿으면 씬에 이 체크포인트를 활성 지점으로 등록."""
        scene.set_checkpoint(self)

    def draw(self, surface, offset=(0, 0)):
        """활성 여부에 따라 다른 색으로 렌더."""
        color = S.COLOR_CHECKPOINT_ON if self.active else S.COLOR_CHECKPOINT
        pygame.draw.rect(surface, color, self.rect.move(-offset[0], -offset[1]))


class Scene:
    """한 레벨의 모든 요소를 보유하고 업데이트 순서를 조율."""

    def __init__(self):
        """타일맵·플레이어·체크포인트를 만들고 리스폰 지점을 초기화."""
        self.tilemap = TileMap()
        self.checkpoints = [Checkpoint(r.x, r.y, r.width, r.height)
                            for r in self.tilemap.checkpoint_rects]
        self.respawn_point = self.tilemap.spawn
        self.player = Player(*self.respawn_point)
        self._dead = False

    # ── 업데이트 ────────────────────────────────────────────────
    def update(self, inp):
        """발판→탑승/끼임→플레이어→트리거→사망/리스폰 순으로 한 프레임 처리."""
        rider = self._riding_platform()      # 발판 이동 전 탑승 판정
        self.tilemap.update()                # 발판 이동
        self._carry_and_push(rider)          # 캐리/밀기/끼임
        solids = self.tilemap.solid_rects()
        hazard_rects = [hz.rect for hz in self.tilemap.hazards]  # 레이캐스트 차단용 가시
        grabbables = list(self.tilemap.ntts) + [e for e in self.tilemap.enemies if e.grabbable()]
        self.player.update(inp, solids, grabbables, hazard_rects)
        for ntt in self.tilemap.ntts:        # NTT 밀쳐짐/복귀 이동
            ntt.update(solids)
        for en in self.tilemap.enemies:      # 적 무적/밀쳐짐/복귀/리스폰
            en.update(solids)
        self._check_triggers()               # 위험(사망)·체크포인트
        if self.player.crushed or self._dead or self.player.y > S.SCREEN_HEIGHT + 100:
            self.respawn()

    def _check_triggers(self):
        """모든 위험/체크포인트에 대해 플레이어 발동을 검사 (잡기 중엔 발사형 트리거 스킵)."""
        for hz in self.tilemap.hazards:
            hz.try_trigger(self.player, Layer.PLAYER, self)
        for cp in self.checkpoints:
            cp.try_trigger(self.player, Layer.PLAYER, self)
        if self._player_grabbing():            # 잡기 중엔 점프패드/스프링 무시(NTT 겹침 무한 잡기 방지)
            return
        for jp in self.tilemap.jump_pads:
            jp.try_trigger(self.player, Layer.PLAYER, self)
        for sp in self.tilemap.springs:
            sp.try_trigger(self.player, Layer.PLAYER, self)

    def _player_grabbing(self):
        """플레이어가 잡기(SEEKING/ACTIVE) 상태인지 여부."""
        from src.player import PlayerState
        return self.player.state in (PlayerState.GRAB_SEEKING, PlayerState.GRAB_ACTIVE)

    # ── 사망 / 리스폰 / 체크포인트 ──────────────────────────────
    def kill(self):
        """사망 요청 (위험 트리거 등이 호출)."""
        self._dead = True

    def set_checkpoint(self, cp):
        """해당 체크포인트를 활성으로 만들고 리스폰 지점을 갱신."""
        if cp.active:
            return
        for c in self.checkpoints:
            c.active = False
        cp.active = True
        self.respawn_point = (cp.x, cp.y)

    def respawn(self):
        """플레이어를 마지막 리스폰 지점에서 재생성하고 잡힌 NTT를 풀어줌."""
        self.player = Player(*self.respawn_point)
        self._dead = False
        for grabbable in list(self.tilemap.ntts) + list(self.tilemap.enemies):
            grabbable.grabbed = False        # 잡은 채 사망 시 대상이 얼지 않게 해제(피격 없음)

    # ── 움직이는 발판 탑승/밀기/끼임 (main에서 이관) ────────────
    def _riding_platform(self):
        """플레이어 발밑(1px)에 닿은 발판을 반환 (상승 중이면 제외)."""
        if self.player.vy < 0:
            return None
        feet = self.player.rect.move(0, 1)
        for plat in self.tilemap.platforms:
            if feet.colliderect(plat.rect):
                return plat
        return None

    def _carry_and_push(self, rider):
        """탑승 발판은 캐리(겹치면 진행 반대로 탈출, 못하면 끼임), 그 외 겹친 발판은 밀어냄."""
        p = self.player
        p.crushed = False
        if rider is not None:
            p.x += rider.dx
            p.y += rider.dy
            p.ride_vx, p.ride_vy = rider.vx, rider.vy
            if self._resolve_carry(rider):   # 캐리 후 지형 겹침 → 탈출 시도/샌드위치 끼임
                p.crushed = True
        else:
            p.ride_vx = p.ride_vy = 0.0
        for plat in self.tilemap.platforms:
            if plat is rider:
                continue
            if p.rect.colliderect(plat.rect):
                self._push_player(plat)
                if self._pinned(exclude=plat):
                    p.crushed = True

    def _resolve_carry(self, rider):
        """캐리 후 다른 솔리드와 겹치면 발판 진행 반대로 밀어내 탈출. 탈출 후에도 발판과 겹치면 샌드위치 끼임(True)."""
        hit = self._overlapping_solid(exclude=rider)
        if hit is None:
            return False
        r = self.player.rect
        # 발판 진행 방향의 반대로 솔리드 밖으로 스냅 (착지·벽 슬라이드는 탈출=생존)
        if rider.dy > 0:
            r.bottom = hit.top
        elif rider.dy < 0:
            r.top = hit.bottom
        if rider.dx > 0:
            r.right = hit.left
        elif rider.dx < 0:
            r.left = hit.right
        self.player.x, self.player.y = r.x, r.y
        # 탈출시켰는데도 발판 본체와 겹치면 = 발판↔솔리드 사이에 낀 것(샌드위치) → 사망
        return r.colliderect(rider.rect)

    def _overlapping_solid(self, exclude):
        """exclude 발판을 뺀 솔리드/발판 중 플레이어와 겹치는 첫 Rect를 반환 (없으면 None)."""
        r = self.player.rect
        idx = r.collidelist(self.tilemap.solids)
        if idx != -1:
            return self.tilemap.solids[idx]
        for plat in self.tilemap.platforms:
            if plat is not exclude and r.colliderect(plat.rect):
                return plat.rect
        return None

    def _push_player(self, plat):
        """발판 이동 방향(dx/dy)으로 플레이어를 발판 밖으로 밀어냄."""
        r = self.player.rect
        if plat.dx > 0:
            r.left = plat.rect.right
        elif plat.dx < 0:
            r.right = plat.rect.left
        if plat.dy > 0:
            r.top = plat.rect.bottom
        elif plat.dy < 0:
            r.bottom = plat.rect.top
        self.player.x, self.player.y = r.x, r.y

    def _pinned(self, exclude):
        """exclude 발판을 뺀 모든 솔리드와 플레이어가 겹치면 핀(끼임) True."""
        r = self.player.rect
        if r.collidelist(self.tilemap.solids) != -1:
            return True
        for plat in self.tilemap.platforms:
            if plat is not exclude and r.colliderect(plat.rect):
                return True
        return False

    # ── 렌더 ────────────────────────────────────────────────────
    def draw(self, surface, offset=(0, 0)):
        """타일맵·체크포인트·플레이어를 렌더."""
        self.tilemap.draw(surface, offset)
        for cp in self.checkpoints:
            cp.draw(surface, offset)
        for ntt in self.tilemap.ntts:
            ntt.draw(surface, offset)
        for en in self.tilemap.enemies:
            en.draw(surface, offset)
        self.player.draw(surface, offset)
