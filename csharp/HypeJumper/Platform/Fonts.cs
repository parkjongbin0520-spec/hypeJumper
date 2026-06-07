using System;
using System.IO;
using FontStashSharp;

namespace HypeJumper;

/// <summary>폰트 로더 — 시스템 TTF(맑은고딕/Consolas)를 런타임 로드. 파이썬 SysFont 방식 미러.</summary>
/// <remarks>폰트 없으면 null 반환 → 렌더러가 텍스트를 건너뜀(게임은 정상 진행).</remarks>
public class Fonts
{
    private readonly FontSystem? _main;   // 한글 (malgun.ttf)
    private readonly FontSystem? _mono;   // HUD (consola.ttf, 없으면 main)

    /// <summary>시스템 폰트 폴더에서 한글/모노 폰트를 로드(없으면 null).</summary>
    public Fonts()
    {
        _main = TryLoad("malgun.ttf");
        _mono = TryLoad("consola.ttf") ?? _main;
    }

    /// <summary>시스템 폰트 폴더의 파일을 FontSystem으로 로드 (실패 시 null).</summary>
    private static FontSystem? TryLoad(string file)
    {
        try
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string path = Path.Combine(dir, file);
            if (!File.Exists(path))
                return null;
            var fs = new FontSystem();
            fs.AddFont(File.ReadAllBytes(path));
            return fs;
        }
        catch { return null; }
    }

    /// <summary>한글 폰트(지정 크기) — 없으면 null.</summary>
    public SpriteFontBase? Main(int size) => _main?.GetFont(size);

    /// <summary>HUD 모노 폰트(지정 크기) — 없으면 null.</summary>
    public SpriteFontBase? Mono(int size) => _mono?.GetFont(size);
}
