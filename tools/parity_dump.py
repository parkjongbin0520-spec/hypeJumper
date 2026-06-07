"""파서티 기준 덤프 — 기존 src.player.Player를 스크립트 입력열로 구동해 프레임별 상태를 JSON으로 출력.

C# 포트(HypeJumper.Core.Player)가 동일 입력열에서 프레임 단위로 일치하는지 검증하는 '정답지'를 만든다.
실행: 저장소 루트에서  python tools/parity_dump.py
출력: csharp/HypeJumper.Tests/Parity/parity_data.json
"""

import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)

import pygame  # noqa: E402  (Player가 pygame.Rect 사용 — init 불필요)
from src.player import Player, PlayerInput  # noqa: E402


def inp(**kw):
    """모든 입력 키를 False 기본으로 깔고 지정 키만 켠 입력 dict 반환."""
    d = dict(left=False, right=False, up=False, down=False, jump_pressed=False,
             jump_held=False, dash_pressed=False, grab_pressed=False, grab_held=False)
    d.update(kw)
    return d


def hold(n, **kw):
    """동일 입력을 n프레임 반복한 리스트."""
    return [inp(**kw) for _ in range(n)]


def run(name, start, solids_xywh, steps):
    """한 시나리오를 구동해 프레임별 플레이어 상태를 기록한 dict 반환."""
    solids = [pygame.Rect(*s) for s in solids_xywh]
    p = Player(*start)
    frames = []
    for st in steps:
        p.update(PlayerInput(**st), solids)
        frames.append({
            "x": p.x, "y": p.y, "vx_in": p.vx_input, "vx_ext": p.vx_external, "vy": p.vy,
            "state": p.state.name.replace("_", ""),
            "on_ground": p.on_ground, "on_wall": p.on_wall, "is_ducking": p.is_ducking,
            "dashes": p.dashes, "dash_timer": p.dash_timer,
            "ceiling_stick": p.ceiling_stick, "hang_timer": p.hang_timer,
        })
    return {"name": name, "player_start": list(start),
            "solids": [list(s) for s in solids_xywh], "steps": steps, "frames": frames}


def build_scenarios():
    """이동/점프/대시/천장/낙하/벽 손맛을 커버하는 시나리오 목록을 만든다."""
    floor = [0, 300, 640, 40]            # 윗면 y=300
    start = (100, 284)                    # 바닥 위(키 16) 시작
    scenarios = []

    scenarios.append(run("idle", start, [floor], hold(15)))
    scenarios.append(run("walk_right", start, [floor], hold(45, right=True)))
    scenarios.append(run(
        "jump_hold_release", start, [floor],
        [inp(right=True, jump_pressed=True, jump_held=True)]
        + hold(9, right=True, jump_held=True)
        + hold(55, right=True)))
    scenarios.append(run(
        "dash_combo", start, [floor],
        [inp(right=True, dash_pressed=True)]
        + hold(14, right=True)
        + [inp(right=True, down=True, dash_pressed=True)]
        + hold(3)
        + [inp(right=True, jump_pressed=True)]
        + hold(35, right=True)))
    scenarios.append(run(
        "ceiling_bonk", start, [floor, [0, 250, 640, 8]],
        [inp(jump_pressed=True, jump_held=True)]
        + hold(22, jump_held=True)
        + hold(25)))
    scenarios.append(run(
        "fall_fastfall", (120, 284), [[0, 300, 160, 40]],
        hold(22, right=True) + hold(30, right=True, down=True)))
    scenarios.append(run(
        "wall_jump", (100, 284), [[0, 300, 400, 40], [180, 180, 16, 120]],
        [inp(right=True, jump_pressed=True, jump_held=True)]
        + hold(6, right=True, jump_held=True)
        + hold(20, right=True)
        + [inp(right=True, jump_pressed=True)]
        + hold(25, right=True)))
    return scenarios


def main():
    """시나리오를 구동해 JSON으로 저장."""
    data = {"scenarios": build_scenarios()}
    out_dir = os.path.join(ROOT, "csharp", "HypeJumper.Tests", "Parity")
    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, "parity_data.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    total = sum(len(s["frames"]) for s in data["scenarios"])
    print(f"[parity] {len(data['scenarios'])} scenarios, {total} frames -> {out_path}")


if __name__ == "__main__":
    main()
