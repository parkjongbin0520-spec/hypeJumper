using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HypeJumper.Core;

namespace HypeJumper;

/// <summary>모든 렌더링 — scene.py/player.py/entities 의 draw() + main.py HUD·메뉴 이식.</summary>
public class Renderer
{
    private readonly GraphicsDevice _gd;
    private readonly Assets _assets;
    private readonly Fonts _fonts;
    private Texture2D? _bg;     // 그라데이션+반딧불 1회 베이킹
    private int _animT;         // 플레이어 애니메이션 클럭 (draw마다 증가)

    public Renderer(GraphicsDevice gd, Assets assets, Fonts fonts)
    {
        _gd = gd;
        _assets = assets;
        _fonts = fonts;
    }

    // ── 월드(480x272 렌더타깃) ──────────────────────────────────
    /// <summary>배경 → 타일맵 → 체크포인트/골/NTT/적/플레이어를 카메라 오프셋으로 렌더.</summary>
    public void DrawWorld(SpriteBatch sb, Scene scene)
    {
        var offset = scene.Camera.Offset;
        sb.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.PointClamp);
        DrawBackground(sb, offset.X);
        DrawTilemap(sb, scene.Tilemap, offset);
        foreach (var cp in scene.Checkpoints)
            _assets.DrawSprite(sb, cp.Active ? "checkpoint_on" : "checkpoint", cp.Rect,
                               cp.Active ? Palette.CheckpointOn : Palette.Checkpoint, offset);
        foreach (var g in scene.Goals)
            _assets.DrawSprite(sb, "goal", g.Rect, Palette.Goal, offset);
        foreach (var ntt in scene.Tilemap.Ntts)
            DrawNtt(sb, ntt, offset);
        foreach (var en in scene.Tilemap.Enemies)
            DrawEnemy(sb, en, offset);
        DrawPlayer(sb, scene.Player, offset);
        sb.End();
    }

    /// <summary>그라데이션+반딧불 base 위에 패럴럭스 bg 레이어(있으면)를 렌더.</summary>
    private void DrawBackground(SpriteBatch sb, int camX)
    {
        _bg ??= BakeBackground();
        sb.Draw(_bg, Vector2.Zero, Color.White);
        DrawParallax(sb, "bg_sky", camX, Settings.PARALLAX_SKY);
        DrawParallax(sb, "bg_bamboo_far", camX, Settings.PARALLAX_FAR);
        DrawParallax(sb, "bg_bamboo_near", camX, Settings.PARALLAX_NEAR);
    }

    /// <summary>비취빛 야간 그라데이션 + 정적 반딧불을 1회 베이킹 (scene._build_background).</summary>
    private Texture2D BakeBackground()
    {
        int w = Settings.INTERNAL_W, h = Settings.INTERNAL_H;
        var data = new Color[w * h];
        Color top = Palette.BgTop, bot = Palette.BgBottom;
        for (int y = 0; y < h; y++)
        {
            double t = y / (double)Math.Max(1, h - 1);
            var col = new Color(
                (int)(top.R + (bot.R - top.R) * t),
                (int)(top.G + (bot.G - top.G) * t),
                (int)(top.B + (bot.B - top.B) * t));
            for (int x = 0; x < w; x++) data[y * w + x] = col;
        }
        var rng = new Random(20260601);   // 고정 시드 — 반딧불 위치 고정(파이썬과 좌표는 다르나 정적)
        for (int i = 0; i < Settings.FIREFLY_COUNT; i++)
        {
            int fx = rng.Next(0, w), fy = rng.Next(0, h);
            int r = rng.Next(3) == 2 ? 2 : 1;
            for (int dy = 0; dy < r; dy++)
                for (int dx = 0; dx < r; dx++)
                {
                    int px = fx + dx, py = fy + dy;
                    if (px < w && py < h) data[py * w + px] = Palette.Firefly;
                }
        }
        var tex = new Texture2D(_gd, w, h);
        tex.SetData(data);
        return tex;
    }

    /// <summary>bg 레이어를 카메라 x의 factor 비율로 가로 무한 타일링(바닥 정렬). 없으면 스킵.</summary>
    private void DrawParallax(SpriteBatch sb, string name, int camX, double factor)
    {
        var spr = _assets.GetSprite(name);
        if (spr == null) return;
        int w = spr.Width, h = spr.Height;
        int sy = Settings.SCREEN_HEIGHT - h;
        int start = PosMod(-(int)(camX * factor), w);
        int x = start - w;
        while (x < Settings.SCREEN_WIDTH)
        {
            sb.Draw(spr, new Vector2(x, sy), Color.White);
            x += w;
        }
    }

    private static int PosMod(int a, int b) => ((a % b) + b) % b;

    /// <summary>타일맵 — solid(타일)·가시·발판·점프패드·스프링 렌더.</summary>
    private void DrawTilemap(SpriteBatch sb, TileMap map, (int X, int Y) offset)
    {
        foreach (var r in map.Solids)
            _assets.TileFill(sb, "tile_ground", r, Palette.Solid, offset);
        foreach (var hz in map.Hazards)
            _assets.DrawSprite(sb, "spike", hz.Rect, Palette.Hazard, offset);
        foreach (var plat in map.Platforms)
            _assets.TileFill(sb, "platform", plat.Rect, Palette.Platform, offset);
        foreach (var jp in map.JumpPads)
            _assets.DrawSprite(sb, "jumppad", jp.Rect, Palette.JumpPad, offset);
        foreach (var sp in map.Springs)
            _assets.DrawSprite(sb, sp.Direction == "up" ? "spring_up" : "spring_wall",
                               sp.Rect, Palette.Spring, offset);
    }

    /// <summary>NTT — 줄 NTT면 피벗~본체 줄을 긋고, 본체를 색 사각형/스프라이트로.</summary>
    private void DrawNtt(SpriteBatch sb, NTT ntt, (int X, int Y) offset)
    {
        if (ntt is RopeNTT rope)
        {
            var (px, py) = rope.Pivot;
            var (cx, cy) = rope.Center;
            _assets.DrawLine(sb, Palette.RopeLine,
                new Vector2((int)px - offset.X, (int)py - offset.Y),
                new Vector2((int)cx - offset.X, (int)cy - offset.Y), 2f);
            _assets.DrawSprite(sb, "rope_ntt", rope.Rect, rope.Grabbed ? Palette.GrabOk : Palette.RopeNtt, offset);
            return;
        }
        _assets.DrawSprite(sb, "ntt", ntt.Rect, ntt.Grabbed ? Palette.GrabOk : Palette.Ntt, offset);
    }

    /// <summary>적 — 파괴 중엔 스킵, 무적 깜빡임은 피격색 사각형, 그 외 적 스프라이트.</summary>
    private void DrawEnemy(SpriteBatch sb, Enemy en, (int X, int Y) offset)
    {
        if (en.Destroyed) return;
        if (en.InvincibleTimer > 0 && (en.InvincibleTimer / 4) % 2 == 0)
        {
            _assets.DrawSprite(sb, Array.Empty<string>(), en.Rect, Palette.EnemyHit, offset);
            return;
        }
        string name = en.MaxHp > 1 ? "enemy_armored" : "enemy";
        _assets.DrawSprite(sb, name, en.Rect, en.MaxHp > 1 ? Palette.EnemyArmored : Palette.Enemy, offset);
    }

    /// <summary>플레이어 — 조준 표시(잡기 중) + 상태별 스프라이트(애니) 좌우반전 렌더.</summary>
    private void DrawPlayer(SpriteBatch sb, Player p, (int X, int Y) offset)
    {
        if (p.State == PlayerState.GrabSeeking || p.State == PlayerState.GrabActive)
            DrawGrabAim(sb, p, offset);
        _animT++;
        var names = Animated(SpriteNames(p));
        _assets.DrawSprite(sb, names, p.Rect, Palette.Player, offset, flip: p.Facing < 0);
    }

    /// <summary>현재 상태로 표시할 스프라이트 이름 후보(우선순위) — player._sprite_names.</summary>
    private static string[] SpriteNames(Player p)
    {
        if (p.IsDucking) return new[] { "player_duck", "player_idle" };
        if (p.State == PlayerState.Dash) return new[] { "player_dash", "player_idle" };
        if (!p.OnGround)
            return p.Vy < 0 ? new[] { "player_jump", "player_idle" } : new[] { "player_fall", "player_idle" };
        if (Math.Abs(p.Vx) > 0.3) return new[] { "player_run", "player_idle" };
        return new[] { "player_idle" };
    }

    /// <summary>번호 프레임이 있으면 현재 프레임을 앞에 끼움 — player._animated.</summary>
    private string[] Animated(string[] bases)
    {
        var frames = _assets.FrameNames(bases[0]);
        if (frames.Count == 0) return bases;
        int idx = (_animT / Settings.ANIM_FRAME_DUR) % frames.Count;
        var outl = new string[bases.Length + 1];
        outl[0] = frames[idx];
        for (int i = 0; i < bases.Length; i++) outl[i + 1] = bases[i];
        return outl;
    }

    /// <summary>조준 범위 원 + 가장 가까운 대상에 직선/하이라이트(초록/빨강) — player._draw_grab_aim.</summary>
    private void DrawGrabAim(SpriteBatch sb, Player p, (int X, int Y) offset)
    {
        bool grabbable = p.GrabTarget != null && p.GrabOk;
        var col = grabbable ? Palette.GrabOk : Palette.GrabNo;
        var (cx, cy) = p.Center;
        var pc = new Vector2((int)cx - offset.X, (int)cy - offset.Y);
        _assets.DrawCircleOutline(sb, col, pc, Settings.GRAB_RANGE, 1f);
        if (p.GrabTarget != null)
        {
            var (tx, ty) = p.GrabTarget.Center;
            if (grabbable)
                _assets.DrawLine(sb, col, pc, new Vector2((int)tx - offset.X, (int)ty - offset.Y), 2f);
            var tr = p.GrabTarget.Rect;
            var box = new Rectangle(tr.X - offset.X - 2, tr.Y - offset.Y - 2, tr.Width + 4, tr.Height + 4);
            _assets.DrawRectOutline(sb, col, box, 2);
        }
    }

    // ── 네이티브 해상도 (HUD / 메뉴) ───────────────────────────
    /// <summary>검증용 디버그 HUD (main._draw_hud).</summary>
    public void DrawHud(SpriteBatch sb, Scene scene)
    {
        var font = _fonts.Mono(16);
        if (font == null) return;
        var p = scene.Player;
        var info = new Color(220, 220, 220);
        var lines = new[]
        {
            $"state={p.State} ground={p.OnGround} wall={p.OnWall} slide={p.WallSliding}",
            $"vx_in={p.VxInput,5:F2} vx_ext={p.VxExternal,5:F2} vy={p.Vy,5:F2}",
            $"duck={p.IsDucking} fast_fall={p.FastFall} ceil_stick={p.CeilingStick} dashes={p.Dashes} dash_t={p.DashTimer}",
            $"grab={(p.State.ToString().StartsWith("Grab") ? p.State.ToString() : "-")} ok={p.GrabOk} target={(p.GrabTarget != null ? "Y" : "N")} grab_t={p.GrabTimer}",
            $"level={scene.LevelIndex + 1}/{Settings.LEVEL_FILES.Length} cam={scene.Camera.Offset} map={scene.Tilemap.Width}x{scene.Tilemap.Height}",
            "[A/D move] [C jump] [X dash] [Z grab] [S duck] [R reset] [ESC menu]",
        };
        sb.Begin(blendState: BlendState.NonPremultiplied);
        for (int i = 0; i < lines.Length; i++)
            sb.DrawString(font, lines[i], new Vector2(12, 12 + i * 20), info);
        if (p.TechFlash > 0)
            sb.DrawString(font, p.LastTech + "!", new Vector2(Settings.SCREEN_WIDTH / 2 - 60, 90), new Color(255, 230, 90));
        sb.End();
    }

    /// <summary>타이틀 화면 (main._draw_title).</summary>
    public void DrawTitle(SpriteBatch sb)
    {
        sb.Begin(blendState: BlendState.NonPremultiplied);
        _assets.FillScreen(sb, Palette.Bg, new Rectangle(0, 0, Settings.SCREEN_WIDTH, Settings.SCREEN_HEIGHT));
        DrawCentered(sb, _fonts.Main(Settings.TITLE_BIG_SIZE), Settings.TITLE_TEXT, Palette.MenuTitle,
                     Settings.SCREEN_HEIGHT / 2 - Settings.TITLE_BIG_SIZE);
        DrawCentered(sb, _fonts.Main(Settings.MENU_HINT_SIZE), Settings.TITLE_HINT, Palette.MenuHint,
                     Settings.SCREEN_HEIGHT / 2 + Settings.TITLE_BIG_SIZE);
        sb.End();
    }

    /// <summary>일시정지 메뉴 — 반투명 오버레이 위 제목/항목/안내 (main._draw_pause).</summary>
    public void DrawPause(SpriteBatch sb, int pauseIndex)
    {
        sb.Begin(blendState: BlendState.NonPremultiplied);
        _assets.FillScreen(sb, new Color(Palette.MenuOverlay, Settings.MENU_OVERLAY_ALPHA),
                           new Rectangle(0, 0, Settings.SCREEN_WIDTH, Settings.SCREEN_HEIGHT));
        DrawCentered(sb, _fonts.Main(Settings.TITLE_BIG_SIZE), Settings.PAUSE_TITLE, Palette.MenuTitle,
                     Settings.SCREEN_HEIGHT / 2 - Settings.TITLE_BIG_SIZE - 20);
        int baseY = Settings.SCREEN_HEIGHT / 2;
        var menuFont = _fonts.Main(Settings.MENU_ITEM_SIZE);
        for (int i = 0; i < Settings.PAUSE_ITEMS.Length; i++)
        {
            bool sel = i == pauseIndex;
            var color = sel ? Palette.MenuSelected : Palette.MenuText;
            string label = sel ? $"> {Settings.PAUSE_ITEMS[i]} <" : Settings.PAUSE_ITEMS[i];
            DrawCentered(sb, menuFont, label, color, baseY + i * Settings.MENU_ITEM_GAP);
        }
        DrawCentered(sb, _fonts.Main(Settings.MENU_HINT_SIZE), "[↑/↓ 이동]  [Enter/C 확정]  [ESC 계속]",
                     Palette.MenuHint, Settings.SCREEN_HEIGHT - 60);
        sb.End();
    }

    /// <summary>문구를 화면 가로 중앙·지정 y에 그린다 (폰트 없으면 스킵).</summary>
    private static void DrawCentered(SpriteBatch sb, SpriteFontBase? font, string text, Color color, int cy)
    {
        if (font == null) return;
        var size = font.MeasureString(text);
        sb.DrawString(font, text, new Vector2(Settings.SCREEN_WIDTH / 2 - size.X / 2, cy), color);
    }
}
