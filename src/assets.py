"""스프라이트 로더 — 이미지 캐시 + 폴백(없으면 사각형). 에셋이 없으면 기존 렌더와 100% 동일."""

import os

import pygame

from src.paths import resource_path

# assets/sprites/ 경로 (개발/번들 공통 — paths.resource_path가 루트 해석)
_SPRITE_DIR = resource_path("assets", "sprites")
_SUBDIRS = ("player", "entities", "tiles", "bg", "ui")  # 이름으로 검색할 하위 폴더
_cache = {}        # name -> Surface | None (미존재도 캐시해 매프레임 디스크 재시도 방지)
_frames_cache = {} # base -> ["base_0","base_1",...] (애니 프레임 목록 캐시)


def _load(name):
    """이름.png를 sprites 하위 폴더에서 찾아 로드 (없으면 None, 디스플레이 없으면 raw)."""
    for sub in _SUBDIRS:
        path = os.path.join(_SPRITE_DIR, sub, name + ".png")
        if os.path.isfile(path):
            img = pygame.image.load(path)
            try:
                return img.convert_alpha()       # 디스플레이 있으면 알파 변환(빠름)
            except pygame.error:
                return img                        # 헤드리스/미초기화 → raw surface
    return None


def get_sprite(name):
    """캐시된 스프라이트 Surface를 반환 (없으면 None). 첫 호출 시 로드·캐싱."""
    if name not in _cache:
        _cache[name] = _load(name)
    return _cache[name]


def frame_names(base):
    """base_0, base_1, … 연속 번호 프레임이 존재하는 만큼의 이름 리스트 반환 (없으면 빈 리스트)."""
    if base in _frames_cache:
        return _frames_cache[base]
    out, i = [], 0
    while get_sprite(f"{base}_{i}") is not None:
        out.append(f"{base}_{i}")
        i += 1
    _frames_cache[base] = out
    return out


def first_sprite(names):
    """이름(문자열 또는 리스트) 중 처음 존재하는 스프라이트를 반환 (없으면 None)."""
    if isinstance(names, str):
        names = (names,)
    for n in names:
        spr = get_sprite(n)
        if spr is not None:
            return spr
    return None


def blit_or_rect(surface, names, rect, color, offset=(0, 0), flip=False):
    """스프라이트가 있으면 히트박스 발-중앙에 맞춰 blit(flip=좌우반전), 없으면 사각형 폴백."""
    spr = first_sprite(names)
    if spr is None:
        pygame.draw.rect(surface, color, rect.move(-offset[0], -offset[1]))
        return
    if flip:
        spr = pygame.transform.flip(spr, True, False)   # 좌향 시 가로 반전
    sx = rect.centerx - spr.get_width() // 2    # 가로 중앙 정렬
    sy = rect.bottom - spr.get_height()         # 발(아래)을 히트박스 바닥에 맞춤
    surface.blit(spr, (sx - offset[0], sy - offset[1]))


def tile_fill(surface, name, rect, color, offset=(0, 0)):
    """타일 스프라이트가 있으면 rect 영역에 반복 타일링(경계 클립), 없으면 사각형 폴백."""
    spr = get_sprite(name)
    if spr is None:
        pygame.draw.rect(surface, color, rect.move(-offset[0], -offset[1]))
        return
    tw, th = spr.get_width(), spr.get_height()
    ox, oy = offset
    prev_clip = surface.get_clip()
    surface.set_clip(rect.move(-ox, -oy))       # rect 밖으로 타일이 넘치지 않게
    y = rect.top
    while y < rect.bottom:
        x = rect.left
        while x < rect.right:
            surface.blit(spr, (x - ox, y - oy))
            x += tw
        y += th
    surface.set_clip(prev_clip)
