using System;

namespace HypeJumper.Core;

/// <summary>오디오 훅 — Core 로직이 호출하는 효과음/배경음 진입점. Platform이 실제 구현을 연결.</summary>
/// <remarks>Core를 MonoGame 무의존으로 유지하기 위한 facade. 기본은 no-op(파서티 테스트는 무음).</remarks>
public static class Sound
{
    /// <summary>효과음 재생 훅 (audio.play 대응). Platform.Audio가 연결.</summary>
    public static Action<string> Play = _ => { };

    /// <summary>배경음 재생 훅 (audio.play_music 대응). Platform.Audio가 연결.</summary>
    public static Action<string> PlayMusic = _ => { };
}
