"""사운드 — SFX/BGM 로더 + 재생. mixer 미초기화·장치없음·파일없음이면 전부 무음(no-op)."""

import os

import pygame

from src.paths import resource_path

_SND_DIR = resource_path("assets", "sounds")
_SFX_DIR = os.path.join(_SND_DIR, "sfx")
_BGM_DIR = os.path.join(_SND_DIR, "bgm")
_SFX_EXT = (".wav", ".ogg", ".mp3")
_BGM_EXT = (".ogg", ".mp3", ".wav")

_enabled = False     # mixer 초기화 성공 여부 (실패 시 전체 무음)
_cache = {}          # "jump" -> Sound | None (미존재도 캐시)
_current_music = None # 현재 재생 중인 BGM 이름 (중복 재생 방지)


def init():
    """오디오 mixer를 초기화 (실패해도 게임은 무음으로 정상 진행)."""
    global _enabled
    try:
        pygame.mixer.init()
        _enabled = True
    except pygame.error:
        _enabled = False


def _find(directory, base, exts):
    """디렉터리에서 base + 확장자 후보 중 처음 존재하는 경로를 반환 (없으면 None)."""
    for ext in exts:
        path = os.path.join(directory, base + ext)
        if os.path.isfile(path):
            return path
    return None


def _load_sfx(name):
    """sfx_<name> 음원을 로드 (없거나 실패 시 None)."""
    path = _find(_SFX_DIR, "sfx_" + name, _SFX_EXT)
    if path is None:
        return None
    try:
        return pygame.mixer.Sound(path)
    except pygame.error:
        return None


def play(name):
    """효과음 재생 — sfx_<name> (mixer/파일 없으면 무음)."""
    if not _enabled:
        return
    if name not in _cache:
        _cache[name] = _load_sfx(name)
    snd = _cache[name]
    if snd is not None:
        snd.play()


def play_music(name, loop=True):
    """배경음 재생 — bgm_<name> 루프 (이미 같은 곡이면 무시, 없으면 무음)."""
    global _current_music
    if not _enabled or name == _current_music:
        return
    path = _find(_BGM_DIR, "bgm_" + name, _BGM_EXT)
    if path is None:
        return
    try:
        pygame.mixer.music.load(path)
        pygame.mixer.music.play(-1 if loop else 0)
        _current_music = name
    except pygame.error:
        pass
