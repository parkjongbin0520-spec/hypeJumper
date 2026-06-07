using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using HypeJumper.Core;

namespace HypeJumper;

/// <summary>효과음/배경음 — audio.py 미러. 파일/장치 없으면 무음 폴백. Core.Sound 훅에 연결.</summary>
public class Audio
{
    private readonly Dictionary<string, SoundEffect?> _sfx = new();   // 미존재도 캐시(무음)
    private string? _currentMusic;

    /// <summary>효과음 재생 — sfx_&lt;name&gt;.wav (없으면 무음).</summary>
    public void Play(string name)
    {
        if (!_sfx.TryGetValue(name, out var snd))
        {
            snd = LoadSfx(name);
            _sfx[name] = snd;
        }
        try { snd?.Play(); }
        catch { /* 장치 없음 등 → 무음 */ }
    }

    /// <summary>sfx_&lt;name&gt;.wav 로드 (없거나 실패 시 null).</summary>
    private static SoundEffect? LoadSfx(string name)
    {
        string path = Paths.ResourcePath($"assets/sounds/sfx/sfx_{name}.wav");
        if (!File.Exists(path))
            return null;
        try { using var fs = File.OpenRead(path); return SoundEffect.FromStream(fs); }
        catch { return null; }
    }

    /// <summary>배경음 루프 — bgm_&lt;name&gt;.ogg (이미 같은 곡이면 무시, 실패 시 무음).</summary>
    public void PlayMusic(string name)
    {
        if (name == _currentMusic)
            return;
        string path = Paths.ResourcePath($"assets/sounds/bgm/bgm_{name}.ogg");
        if (!File.Exists(path))
            return;
        try
        {
            var song = Song.FromUri(name, new Uri(path));
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(song);
            _currentMusic = name;
        }
        catch { /* ogg 런타임 로드 불가 등 → 무음 폴백 */ }
    }
}
