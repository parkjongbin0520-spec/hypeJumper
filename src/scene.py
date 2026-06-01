"""씬 — 타일맵·플레이어·트리거(위험/체크포인트)를 관리하고 갱신/렌더/리스폰을 담당."""

import pygame

import settings as S
from src import assets
from src import audio
from src.camera import Camera
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
        """활성 여부에 따라 스프라이트(있으면)/색 사각형으로 렌더."""
        if self.active:
            assets.blit_or_rect(surface, "checkpoint_on", self.rect, S.COLOR_CHECKPOINT_ON, offset)
        else:
            assets.blit_or_rect(surface, "checkpoint", self.rect, S.COLOR_CHECKPOINT, offset)


class Goal(Trigger):
    """플레이어가 닿으면 다음 레벨로 전환을 요청하는 레벨 종료 트리거 (PLAYER만 반응)."""

    def __init__(self, x, y, width, height):
        """PLAYER 대상 트리거로 초기화."""
        super().__init__(x, y, width, height, [Layer.PLAYER])

    def on_enter(self, actor, scene):
        """플레이어가 닿으면 씬에 레벨 전환을 예약."""
        scene.request_advance()

    def draw(self, surface, offset=(0, 0)):
        """골 스프라이트(있으면)로, 없으면 레벨 종료 색 사각형으로 렌더."""
        assets.blit_or_rect(surface, "goal", self.rect, S.COLOR_GOAL, offset)


class Scene:
    """한 레벨의 모든 요소를 보유하고 업데이트 순서를 조율."""

    def __init__(self):
        """카메라를 만들고 첫 레벨을 로드."""
        self.camera = Camera()
        self.level_index = 0
        self._advance_pending = False    # 골 도달 → 다음 레벨 전환 예약 플래그
        self._bg_surface = None          # 그라데이션+반딧불 배경 (첫 draw에서 1회 베이킹)
        self._load_level(self.level_index)

    def _load_level(self, index):
        """레벨 시퀀스의 index 맵을 로드하고 플레이어/체크포인트/골/카메라를 초기화."""
        self.level_index = index
        map_file = S.LEVEL_FILES[index] if index < len(S.LEVEL_FILES) else None
        self.tilemap = TileMap(map_file=map_file)
        self.checkpoints = [Checkpoint(r.x, r.y, r.width, r.height)
                            for r in self.tilemap.checkpoint_rects]
        self.goals = [Goal(r.x, r.y, r.width, r.height)
                      for r in self.tilemap.goal_rects]
        self.respawn_point = self.tilemap.spawn
        self.player = Player(*self.respawn_point)
        self._dead = False
        self._advance_pending = False
        self.camera.snap_to(self.player.rect, self.tilemap.width, self.tilemap.height)
        audio.play_music("stage1")          # 배경음 루프 (이미 재생 중이면 무시)

    def request_advance(self):
        """골 트리거가 호출 — 이번 프레임 끝에 다음 레벨로 전환하도록 예약 (1회 골 효과음)."""
        if not self._advance_pending:
            audio.play("goal")
        self._advance_pending = True

    def next_level(self):
        """다음 레벨로 전환 (마지막 레벨이면 그대로 유지 = 클리어)."""
        if self.level_index + 1 < len(S.LEVEL_FILES):
            self._load_level(self.level_index + 1)

    # ── 업데이트 ────────────────────────────────────────────────
    def update(self, inp):
        """발판→탑승/끼임→플레이어→트리거→사망/리스폰 순으로 한 프레임 처리."""
        if self.camera.sliding:              # 방 전환 슬라이드 중 = 게임플레이 정지, 카메라만 이동
            self.camera.update(self.player.rect, self.tilemap.width, self.tilemap.height)
            return
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
        self._check_triggers()               # 위험(사망)·체크포인트·골
        if self._advance_pending:            # 골 도달 → 다음 레벨 전환(이번 프레임 종료)
            self._advance_pending = False
            self.next_level()
            return
        if self.player.crushed or self._dead or self.player.y > self.tilemap.height + 100:
            self.respawn()
        if self.camera.update(self.player.rect, self.tilemap.width, self.tilemap.height):
            self.player.dashes = S.MAX_DASHES   # 방 전환 시작 → 대시 1회 초기화(새 방 진입 보상)

    def _check_triggers(self):
        """모든 위험/체크포인트에 대해 플레이어 발동을 검사 (잡기 중엔 발사형 트리거 스킵)."""
        for hz in self.tilemap.hazards:
            hz.try_trigger(self.player, Layer.PLAYER, self)
        for cp in self.checkpoints:
            cp.try_trigger(self.player, Layer.PLAYER, self)
        for g in self.goals:                   # 레벨 종료 — 닿으면 다음 레벨 전환 예약
            g.try_trigger(self.player, Layer.PLAYER, self)
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
        audio.play("checkpoint")

    def respawn(self):
        """플레이어를 마지막 리스폰 지점에서 재생성하고 잡힌 NTT를 풀어주며 카메라를 스냅."""
        audio.play("death")
        self.player = Player(*self.respawn_point)
        self._dead = False
        for grabbable in list(self.tilemap.ntts) + list(self.tilemap.enemies):
            grabbable.grabbed = False        # 잡은 채 사망 시 대상이 얼지 않게 해제(피격 없음)
        self.camera.snap_to(self.player.rect, self.tilemap.width, self.tilemap.height)

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
    def _build_background(self, size):
        """비취빛 야간 그라데이션 + 정적 반딧불을 1회 베이킹한 배경 Surface 생성."""
        import random
        w, h = size
        bg = pygame.Surface(size)
        top, bot = S.COLOR_BG_TOP, S.COLOR_BG_BOTTOM
        for y in range(h):                       # 세로 그라데이션 (상단→하단 보간)
            t = y / max(1, h - 1)
            col = tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3))
            pygame.draw.line(bg, col, (0, y), (w, y))
        rng = random.Random(20260601)            # 고정 시드 — 반딧불 위치 고정(깜빡임 없음)
        for _ in range(S.FIREFLY_COUNT):
            fx, fy = rng.randint(0, w - 1), rng.randint(0, h - 1)
            r = rng.choice((1, 1, 2))            # 작은 점 위주
            pygame.draw.circle(bg, S.COLOR_FIREFLY, (fx, fy), r)
        return bg

    def _draw_background(self, surface):
        """그라데이션+반딧불 base 위에 패럴럭스 bg 레이어(있으면)를 깊이 순서로 렌더."""
        if self._bg_surface is None:
            self._bg_surface = self._build_background(surface.get_size())
        surface.blit(self._bg_surface, (0, 0))      # base (bg 스프라이트 없으면 이것만 = 기존과 동일)
        ox, oy = self.camera.offset
        self._draw_parallax_layer(surface, "bg_sky", ox, S.PARALLAX_SKY)
        self._draw_parallax_layer(surface, "bg_bamboo_far", ox, S.PARALLAX_FAR)
        self._draw_parallax_layer(surface, "bg_bamboo_near", ox, S.PARALLAX_NEAR)

    def _draw_parallax_layer(self, surface, name, cam_x, factor):
        """bg 레이어를 카메라 x의 factor 비율로 가로 무한 타일링(세로 바닥 정렬). 없으면 스킵."""
        spr = assets.get_sprite(name)
        if spr is None:
            return
        w, h = spr.get_width(), spr.get_height()
        sy = S.SCREEN_HEIGHT - h                     # 바닥 정렬 (위 빈 곳은 base 그라데이션이 비침)
        start = (-int(cam_x * factor)) % w           # 스크롤 오프셋을 [0,w)로 래핑
        x = start - w                                # 왼쪽 한 칸 더부터 화면 끝까지 반복
        while x < S.SCREEN_WIDTH:
            surface.blit(spr, (x, sy))
            x += w

    def draw(self, surface):
        """배경 → 카메라 오프셋을 적용해 타일맵·체크포인트·골·NTT·적·플레이어를 렌더."""
        self._draw_background(surface)
        offset = self.camera.offset
        self.tilemap.draw(surface, offset)
        for cp in self.checkpoints:
            cp.draw(surface, offset)
        for g in self.goals:
            g.draw(surface, offset)
        for ntt in self.tilemap.ntts:
            ntt.draw(surface, offset)
        for en in self.tilemap.enemies:
            en.draw(surface, offset)
        self.player.draw(surface, offset)
