using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HypeJumper.Core;

namespace HypeJumper;

/// <summary>스프라이트 로더 + 그리기 헬퍼 — assets.py 미러(이미지 캐시 + 폴백 사각형 + 타일링 + 프리미티브).</summary>
public class Assets
{
    private static readonly string[] Subs = { "player", "entities", "tiles", "bg", "ui" };
    private readonly GraphicsDevice _gd;
    private readonly Texture2D _pixel;                       // 사각형/선/원 프리미티브용 1x1 흰 텍스처
    private readonly Dictionary<string, Texture2D?> _cache = new();   // name -> Texture | null
    private readonly Dictionary<string, List<string>> _frames = new();

    /// <summary>GraphicsDevice를 받아 1x1 흰 픽셀 텍스처를 만든다.</summary>
    public Assets(GraphicsDevice gd)
    {
        _gd = gd;
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>이름.png를 sprites 하위 폴더에서 찾아 로드 (없으면 null, 캐시).</summary>
    public Texture2D? GetSprite(string name)
    {
        if (_cache.TryGetValue(name, out var cached))
            return cached;
        Texture2D? tex = null;
        foreach (var sub in Subs)
        {
            string path = Paths.ResourcePath($"assets/sprites/{sub}/{name}.png");
            if (File.Exists(path))
            {
                try { using var fs = File.OpenRead(path); tex = Texture2D.FromStream(_gd, fs); }
                catch { tex = null; }
                break;
            }
        }
        _cache[name] = tex;
        return tex;
    }

    /// <summary>base_0, base_1, … 연속 번호 프레임 이름 리스트 (없으면 빈 리스트).</summary>
    public List<string> FrameNames(string baseName)
    {
        if (_frames.TryGetValue(baseName, out var f))
            return f;
        var outl = new List<string>();
        int i = 0;
        while (GetSprite($"{baseName}_{i}") != null)
        {
            outl.Add($"{baseName}_{i}");
            i++;
        }
        _frames[baseName] = outl;
        return outl;
    }

    /// <summary>이름들 중 처음 존재하는 스프라이트 (없으면 null).</summary>
    public Texture2D? FirstSprite(IEnumerable<string> names)
    {
        foreach (var n in names)
        {
            var s = GetSprite(n);
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>스프라이트 있으면 발-중앙 정렬로 그리고(flip 가능), 없으면 색 사각형 폴백 (blit_or_rect).</summary>
    public void DrawSprite(SpriteBatch sb, IReadOnlyList<string> names, RectI rect, Color color, (int X, int Y) offset, bool flip = false)
    {
        var spr = FirstSprite(names);
        if (spr == null)
        {
            DrawRect(sb, color, rect, offset);
            return;
        }
        int sx = rect.CenterX - spr.Width / 2;   // 가로 중앙
        int sy = rect.Bottom - spr.Height;       // 발(아래)을 히트박스 바닥에 맞춤
        var pos = new Vector2(sx - offset.X, sy - offset.Y);
        sb.Draw(spr, pos, null, Color.White, 0f, Vector2.Zero, 1f,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    /// <summary>단일 이름 편의 오버로드.</summary>
    public void DrawSprite(SpriteBatch sb, string name, RectI rect, Color color, (int X, int Y) offset, bool flip = false)
        => DrawSprite(sb, new[] { name }, rect, color, offset, flip);

    /// <summary>타일 스프라이트를 rect 영역에 반복 타일링(경계 트림), 없으면 색 사각형 폴백 (tile_fill).</summary>
    public void TileFill(SpriteBatch sb, string name, RectI rect, Color color, (int X, int Y) offset)
    {
        var spr = GetSprite(name);
        if (spr == null)
        {
            DrawRect(sb, color, rect, offset);
            return;
        }
        int tw = spr.Width, th = spr.Height;
        for (int y = rect.Top; y < rect.Bottom; y += th)
        {
            int dh = Math.Min(th, rect.Bottom - y);
            for (int x = rect.Left; x < rect.Right; x += tw)
            {
                int dw = Math.Min(tw, rect.Right - x);
                sb.Draw(spr, new Rectangle(x - offset.X, y - offset.Y, dw, dh), new Rectangle(0, 0, dw, dh), Color.White);
            }
        }
    }

    /// <summary>월드 사각형을 카메라 오프셋 적용해 단색으로 채움 (draw.rect 폴백).</summary>
    public void DrawRect(SpriteBatch sb, Color color, RectI rect, (int X, int Y) offset)
        => sb.Draw(_pixel, new Rectangle(rect.X - offset.X, rect.Y - offset.Y, rect.Width, rect.Height), color);

    /// <summary>화면 좌표 사각형을 단색으로 채움 (HUD/오버레이용, 오프셋 없음).</summary>
    public void FillScreen(SpriteBatch sb, Color color, Rectangle r) => sb.Draw(_pixel, r, color);

    /// <summary>사각형 외곽선(두께 thickness) — 네 변을 픽셀로 (draw.rect width&gt;0).</summary>
    public void DrawRectOutline(SpriteBatch sb, Color color, Rectangle r, int thickness)
    {
        sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);                       // 상
        sb.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);      // 하
        sb.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);                      // 좌
        sb.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);      // 우
    }

    /// <summary>두 점을 잇는 선 (회전된 픽셀 quad).</summary>
    public void DrawLine(SpriteBatch sb, Color color, Vector2 a, Vector2 b, float thickness = 1f)
    {
        var d = b - a;
        float len = d.Length();
        float ang = (float)Math.Atan2(d.Y, d.X);
        sb.Draw(_pixel, a, null, color, ang, new Vector2(0f, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0f);
    }

    /// <summary>원 외곽선 (세그먼트 근사, 그랩 조준 표시용).</summary>
    public void DrawCircleOutline(SpriteBatch sb, Color color, Vector2 center, float radius, float thickness = 1f)
    {
        int seg = Math.Max(16, (int)radius);
        var prev = center + new Vector2(radius, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (float)(i / (double)seg * Math.PI * 2.0);
            var p = center + new Vector2((float)Math.Cos(a) * radius, (float)Math.Sin(a) * radius);
            DrawLine(sb, color, prev, p, thickness);
            prev = p;
        }
    }
}
