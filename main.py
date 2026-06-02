"""게임 진입점 — pygame 창/루프, 입력 수집, Scene에 갱신·렌더 위임."""

import sys
from enum import Enum, auto

import pygame

import settings as S
from src import audio
from src.player import PlayerInput
from src.scene import Scene


class GameState(Enum):
    """최상위 게임 상태 — 타이틀/플레이/일시정지 화면 분기."""
    TITLE = auto()    # 진입 화면 (아무 키나 눌러 시작)
    PLAYING = auto()  # 실제 게임 진행 (Scene 갱신)
    PAUSED = auto()   # ESC 일시정지 메뉴 (계속 진행/끝내기)


class Game:
    """창·루프를 관리하고 입력을 모아 Scene에 전달하는 최상위 클래스."""

    def __init__(self):
        """pygame 초기화, 창·클럭·씬·폰트 생성, 타이틀 상태로 시작."""
        pygame.init()
        audio.init()                     # 오디오 mixer 초기화 (실패해도 무음으로 진행)
        self.screen = pygame.display.set_mode((S.SCREEN_WIDTH, S.SCREEN_HEIGHT))
        self.world = pygame.Surface((S.INTERNAL_W, S.INTERNAL_H))  # 줌용 내부 저해상 렌더 타깃
        pygame.display.set_caption(S.TITLE)
        self.clock = pygame.time.Clock()
        self.font = pygame.font.SysFont("consolas", 16)          # 디버그 HUD (영문/숫자)
        self.font_title = pygame.font.SysFont("malgungothic", S.TITLE_BIG_SIZE, bold=True)  # 한글 대문
        self.font_menu = pygame.font.SysFont("malgungothic", S.MENU_ITEM_SIZE)              # 한글 메뉴
        self.font_hint = pygame.font.SysFont("malgungothic", S.MENU_HINT_SIZE)              # 한글 안내
        self.running = True
        self.state = GameState.TITLE     # 게임 진입 시 타이틀 화면부터
        self.pause_index = 0             # 일시정지 메뉴 현재 선택 항목 인덱스
        self.jump_pressed = False        # 이번 프레임 점프키 엣지
        self.dash_pressed = False        # 이번 프레임 대시키 엣지
        self.grab_pressed = False        # 이번 프레임 잡기키(Z) 엣지
        self._slow_phase = 0             # 슬로우모션 프레임 카운터 (조준 윈도우)
        self.scene = Scene()

    # ── 입력 처리 ─────────────────────────────────
    def handle_events(self):
        """현재 게임 상태에 따라 키 이벤트를 알맞은 핸들러로 라우팅."""
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                self.running = False
            elif event.type == pygame.KEYDOWN:
                if self.state == GameState.TITLE:
                    self._on_title_key(event)
                elif self.state == GameState.PAUSED:
                    self._on_pause_key(event)
                else:
                    self._on_play_key(event)

    def _on_title_key(self, event):
        """타이틀 화면 — ESC는 종료, 그 외 아무 키나 누르면 게임 시작."""
        if event.key == pygame.K_ESCAPE:
            self.running = False
        else:
            self.state = GameState.PLAYING

    def _on_play_key(self, event):
        """플레이 중 — ESC로 일시정지, 점프(C)/대시(X)/잡기(Z)/리셋(R) 엣지."""
        if event.key == pygame.K_ESCAPE:
            self._enter_pause()
        elif event.key == pygame.K_c:
            self.jump_pressed = True
        elif event.key == pygame.K_x:
            self.dash_pressed = True
        elif event.key == pygame.K_z:
            self.grab_pressed = True
        elif event.key == pygame.K_r:
            self.scene = Scene()         # 전체 리셋(체크포인트 포함)

    def _on_pause_key(self, event):
        """일시정지 메뉴 — 위/아래로 항목 이동, Enter/C로 확정, ESC로 즉시 재개."""
        if event.key == pygame.K_ESCAPE:
            self._resume()
        elif event.key in (pygame.K_UP, pygame.K_w):
            self.pause_index = (self.pause_index - 1) % len(S.PAUSE_ITEMS)
        elif event.key in (pygame.K_DOWN, pygame.K_s):
            self.pause_index = (self.pause_index + 1) % len(S.PAUSE_ITEMS)
        elif event.key in (pygame.K_RETURN, pygame.K_c):
            self._confirm_pause()

    def _enter_pause(self):
        """일시정지 진입 — 선택 초기화 및 엣지 플래그 정리(재개 시 오발동 방지)."""
        self.state = GameState.PAUSED
        self.pause_index = 0
        self._clear_edges()

    def _confirm_pause(self):
        """현재 선택 항목 실행 — 0=계속 진행(재개), 1=끝내기(종료)."""
        if self.pause_index == 0:
            self._resume()
        else:
            self.running = False

    def _resume(self):
        """일시정지 해제 후 플레이 복귀 — 버퍼된 입력이 즉시 발동하지 않도록 엣지 정리."""
        self.state = GameState.PLAYING
        self._clear_edges()

    def _clear_edges(self):
        """이번 프레임 엣지 입력 플래그를 모두 리셋."""
        self.jump_pressed = False
        self.dash_pressed = False
        self.grab_pressed = False

    def _read_input(self):
        """현재 키 상태를 PlayerInput 스냅샷으로 변환 (점프=C, 대시=X, 잡기=Z)."""
        k = pygame.key.get_pressed()
        return PlayerInput(
            left=k[pygame.K_LEFT] or k[pygame.K_a],
            right=k[pygame.K_RIGHT] or k[pygame.K_d],
            up=k[pygame.K_UP] or k[pygame.K_w],
            down=k[pygame.K_DOWN] or k[pygame.K_s],
            jump_pressed=self.jump_pressed,
            jump_held=k[pygame.K_c],
            dash_pressed=self.dash_pressed,
            grab_pressed=self.grab_pressed,
            grab_held=k[pygame.K_z],
        )

    # ── 갱신 ─────────────────────────────────────
    def update(self):
        """플레이 중일 때만 씬을 갱신(슬로우 중 일부 스킵)하고 엣지 플래그를 리셋."""
        if self.state != GameState.PLAYING:   # 타이틀/일시정지: 씬 정지
            return
        inp = self._read_input()
        if not self._should_step():       # 슬로우모션: 이번 렌더 프레임은 갱신 스킵
            return
        self.scene.update(inp)
        self._clear_edges()

    def _should_step(self):
        """조준 슬로우 중이면 GRAB_SLOW_FACTOR 프레임마다 1번만 갱신(1/N 속도)."""
        if self.scene.player.aim_slow:
            self._slow_phase += 1
            return self._slow_phase % S.GRAB_SLOW_FACTOR == 0
        self._slow_phase = 0
        return True

    # ── 렌더 ─────────────────────────────────────
    def draw(self):
        """상태별 화면 출력 — 타이틀/플레이/일시정지 오버레이."""
        if self.state == GameState.TITLE:
            self._draw_title()
        else:
            self.scene.draw(self.world)             # 320×180 내부 렌더(배경 포함)
            pygame.transform.scale(self.world, (S.SCREEN_WIDTH, S.SCREEN_HEIGHT), self.screen)
            self._draw_hud()                        # HUD는 확대 후 네이티브 해상도에 (선명)
            if self.state == GameState.PAUSED:
                self._draw_pause()                  # 정지 화면 위에 메뉴 오버레이
        pygame.display.flip()

    def _draw_centered(self, font, text, color, cy):
        """문구를 화면 가로 중앙·지정 y에 그린다 (메뉴 공통 헬퍼)."""
        surf = font.render(text, True, color)
        self.screen.blit(surf, (S.SCREEN_WIDTH // 2 - surf.get_width() // 2, cy))

    def _draw_title(self):
        """타이틀 화면 — 배경 단색 위 대문 문구와 시작 안내를 중앙 정렬."""
        self.screen.fill(S.COLOR_BG)
        self._draw_centered(self.font_title, S.TITLE_TEXT, S.COLOR_MENU_TITLE,
                            S.SCREEN_HEIGHT // 2 - S.TITLE_BIG_SIZE)
        self._draw_centered(self.font_hint, S.TITLE_HINT, S.COLOR_MENU_HINT,
                            S.SCREEN_HEIGHT // 2 + S.TITLE_BIG_SIZE)

    def _draw_pause(self):
        """일시정지 메뉴 — 반투명 오버레이 위 제목과 항목(선택 항목 강조) 표시."""
        overlay = pygame.Surface((S.SCREEN_WIDTH, S.SCREEN_HEIGHT))
        overlay.set_alpha(S.MENU_OVERLAY_ALPHA)
        overlay.fill(S.MENU_OVERLAY_COLOR)
        self.screen.blit(overlay, (0, 0))
        self._draw_centered(self.font_title, S.PAUSE_TITLE, S.COLOR_MENU_TITLE,
                            S.SCREEN_HEIGHT // 2 - S.TITLE_BIG_SIZE - 20)
        base_y = S.SCREEN_HEIGHT // 2
        for i, item in enumerate(S.PAUSE_ITEMS):
            sel = (i == self.pause_index)
            color = S.COLOR_MENU_SELECTED if sel else S.COLOR_MENU_TEXT
            label = f"> {item} <" if sel else item        # 선택 항목은 꺾쇠로 강조
            self._draw_centered(self.font_menu, label, color, base_y + i * S.MENU_ITEM_GAP)
        self._draw_centered(self.font_hint, "[↑/↓ 이동]  [Enter/C 확정]  [ESC 계속]",
                            S.COLOR_MENU_HINT, S.SCREEN_HEIGHT - 60)

    def _draw_hud(self):
        """검증용 디버그 정보(상태/속도/접지/대시/테크)를 출력."""
        p = self.scene.player
        lines = [
            f"state={p.state.name} ground={p.on_ground} wall={p.on_wall} slide={p.wall_sliding}",
            f"vx_in={p.vx_input:5.2f} vx_ext={p.vx_external:5.2f} vy={p.vy:5.2f}",
            f"duck={p.is_ducking} fast_fall={p.fast_fall} ceil_stick={p.ceiling_stick} dashes={p.dashes} dash_t={p.dash_timer}",
            f"grab={p.state.name if p.state.name.startswith('GRAB') else '-'} ok={p.grab_ok} target={'Y' if p.grab_target else 'N'} grab_t={p.grab_timer}",
            f"level={self.scene.level_index + 1}/{len(S.LEVEL_FILES)} cam={self.scene.camera.offset} map={self.scene.tilemap.width}x{self.scene.tilemap.height}",
            "[A/D 이동] [C 점프] [X 대시] [Z 잡기(홀드조준→뗌→재입력)] [S 웅크림] [R 리셋] [ESC 메뉴]",
        ]
        for i, line in enumerate(lines):
            self.screen.blit(self.font.render(line, True, (220, 220, 220)), (12, 12 + i * 20))
        if p.tech_flash > 0:
            big = self.font.render(p.last_tech + "!", True, (255, 230, 90))
            self.screen.blit(big, (S.SCREEN_WIDTH // 2 - 60, 90))

    def run(self):
        """메인 루프 — 고정 FPS로 이벤트·갱신·렌더 반복."""
        while self.running:
            self.handle_events()
            self.update()
            self.draw()
            self.clock.tick(S.FPS)
        pygame.quit()
        sys.exit()


if __name__ == "__main__":
    Game().run()
