"""에셋 정리 도구 — ROOT(또는 _inbox/)에 드롭한 파일을 이름 규칙대로 assets/ 하위로 이동.

사용법:
    python tools/organize_assets.py            # 실제 이동
    python tools/organize_assets.py --dry-run  # 미리보기(이동 안 함)

분류 규칙(ASSETS.md와 동일):
    player_*.png            → assets/sprites/player
    tile_*.png              → assets/sprites/tiles
    bg_*.png                → assets/sprites/bg
    ui_*.png                → assets/sprites/ui
    sfx_*.(wav|ogg|mp3)     → assets/sounds/sfx
    bgm_*.(ogg|mp3|wav)     → assets/sounds/bgm
    map_*.txt               → assets/tilemaps
    (화이트리스트 엔티티).png → assets/sprites/entities
    그 외 → 건드리지 않고 스킵(안전) — map_preview 등 프로젝트 파일 보호
"""

import os
import shutil
import sys

# 프로젝트 루트 = 이 파일의 상위(tools)의 상위
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

IMG_EXT = {".png"}
SFX_EXT = {".wav", ".ogg", ".mp3"}
BGM_EXT = {".ogg", ".mp3", ".wav"}

# 접두사 → (대상 폴더, 허용 확장자) ─ 가장 명확한 1순위 규칙
PREFIX_RULES = [
    ("player_", "assets/sprites/player", IMG_EXT),
    ("tile_",   "assets/sprites/tiles",  IMG_EXT),
    ("bg_",     "assets/sprites/bg",     IMG_EXT),
    ("ui_",     "assets/sprites/ui",     IMG_EXT),
    ("sfx_",    "assets/sounds/sfx",     SFX_EXT),
    ("bgm_",    "assets/sounds/bgm",     BGM_EXT),
    ("map_",    "assets/tilemaps",       {".txt"}),
]

# 접두사 없이 정해진 이름으로 들어오는 엔티티 스프라이트 → entities 폴더
ENTITY_SPRITES = {
    "enemy", "enemy_armored", "ntt", "rope_ntt", "spring_up", "spring_wall",
    "jumppad", "checkpoint", "checkpoint_on", "goal", "spike", "platform",
}

# 스캔 대상 — 루트 최상위 + _inbox/ (있으면). 하위 재귀 안 함(assets/ 이미정리분 보호)
SCAN_DIRS = [ROOT, os.path.join(ROOT, "_inbox")]


def classify(filename):
    """파일명을 규칙에 대입해 대상 폴더(상대경로)를 반환, 해당 없으면 None."""
    name = filename.lower()
    base, ext = os.path.splitext(name)
    for prefix, dest, exts in PREFIX_RULES:
        if name.startswith(prefix) and ext in exts:
            return dest
    if ext in IMG_EXT and base in ENTITY_SPRITES:
        return "assets/sprites/entities"
    return None


def iter_candidates():
    """스캔 폴더의 최상위 파일을 (전체경로, 파일명)으로 순회."""
    for d in SCAN_DIRS:
        if not os.path.isdir(d):
            continue
        for entry in os.listdir(d):
            path = os.path.join(d, entry)
            if os.path.isfile(path):
                yield path, entry


def main(dry_run=False):
    """후보 파일을 분류해 이동(또는 미리보기)하고 결과를 출력."""
    moved = skipped = 0
    for path, name in iter_candidates():
        dest_rel = classify(name)
        if dest_rel is None:
            continue                                  # 모르는 파일은 조용히 무시(보호)
        dest_dir = os.path.join(ROOT, dest_rel)
        dest_path = os.path.join(dest_dir, name)
        if os.path.abspath(path) == os.path.abspath(dest_path):
            continue                                  # 이미 제자리
        tag = "[DRY]" if dry_run else "[MOVE]"
        note = " (덮어쓰기)" if os.path.exists(dest_path) else ""
        print(f"{tag} {name} -> {dest_rel}/{note}")
        if not dry_run:
            os.makedirs(dest_dir, exist_ok=True)
            shutil.move(path, dest_path)
        moved += 1
    print(f"--- {'미리보기 ' if dry_run else ''}대상 {moved}개 처리 (모르는 파일은 스킵) ---")
    return moved


if __name__ == "__main__":
    main(dry_run="--dry-run" in sys.argv)
