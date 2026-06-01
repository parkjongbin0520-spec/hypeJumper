"""게임 진입점 — pygame 창/루프, 입력 수집, Scene에 갱신·렌더 위임."""

import sys

import pygame

import settings as S
from src import audio
from src.player import PlayerInput
from src.scene import Scene


class Game:
    """창·루프를 관리하고 입력을 모아 Scene에 전달하는 최상위 클래스."""

    def __init__(self):
        """pygame 초기화, 창·클럭·씬·디버그 폰트 생성."""
        pygame.init()
        audio.init()                     # 오디오 mixer 초기화 (실패해도 무음으로 진행)
        self.screen = pygame.display.set_mode((S.SCREEN_WIDTH, S.SCREEN_HEIGHT))
        pygame.display.set_caption(S.TITLE)
        self.clock = pygame.time.Clock()
        self.font = pygame.font.SysFont("consolas", 16)
        self.running = True
        self.jump_pressed = False        # 이번 프레임 점프키 엣지
        self.dash_pressed = False        # 이번 프레임 대시키 엣지
        self.grab_pressed = False        # 이번 프레임 잡기키(Z) 엣지
        self._slow_phase = 0             # 슬로우모션 프레임 카운터 (조준 윈도우)
        self.scene = Scene()

    def handle_events(self):
        """종료, 점프(C)/대시(X)/잡기(Z) 엣지, 리셋(R) 입력 처리."""
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                self.running = False
            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_ESCAPE:
                    self.running = False
                elif event.key == pygame.K_c:
                    self.jump_pressed = True
                elif event.key == pygame.K_x:
                    self.dash_pressed = True
                elif event.key == pygame.K_z:
                    self.grab_pressed = True
                elif event.key == pygame.K_r:
                    self.scene = Scene()        # 전체 리셋(체크포인트 포함)

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

    def update(self):
        """입력을 모아 씬을 갱신(조준 슬로우 중엔 일부 프레임 스킵)하고 엣지 플래그를 리셋."""
        inp = self._read_input()
        if not self._should_step():       # 슬로우모션: 이번 렌더 프레임은 갱신 스킵
            return
        self.scene.update(inp)
        self.jump_pressed = False
        self.dash_pressed = False
        self.grab_pressed = False

    def _should_step(self):
        """조준 슬로우 중이면 GRAB_SLOW_FACTOR 프레임마다 1번만 갱신(1/N 속도)."""
        if self.scene.player.aim_slow:
            self._slow_phase += 1
            return self._slow_phase % S.GRAB_SLOW_FACTOR == 0
        self._slow_phase = 0
        return True

    def draw(self):
        """씬(배경 포함)·디버그 HUD를 렌더하고 화면을 갱신."""
        self.scene.draw(self.screen)        # Scene이 그라데이션 배경부터 그림
        self._draw_hud()
        pygame.display.flip()

    def _draw_hud(self):
        """검증용 디버그 정보(상태/속도/접지/대시/테크)를 출력."""
        p = self.scene.player
        lines = [
            f"state={p.state.name} ground={p.on_ground} wall={p.on_wall} slide={p.wall_sliding}",
            f"vx_in={p.vx_input:5.2f} vx_ext={p.vx_external:5.2f} vy={p.vy:5.2f}",
            f"duck={p.is_ducking} fast_fall={p.fast_fall} ceil_stick={p.ceiling_stick} dashes={p.dashes} dash_t={p.dash_timer}",
            f"grab={p.state.name if p.state.name.startswith('GRAB') else '-'} ok={p.grab_ok} target={'Y' if p.grab_target else 'N'} grab_t={p.grab_timer}",
            f"level={self.scene.level_index + 1}/{len(S.LEVEL_FILES)} cam={self.scene.camera.offset} map={self.scene.tilemap.width}x{self.scene.tilemap.height}",
            "[A/D 이동] [C 점프] [X 대시] [Z 잡기(홀드조준→뗌→재입력)] [S 웅크림] [R 리셋]",
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
