using Microsoft.Xna.Framework;

namespace HypeJumper;

/// <summary>색상 상수 — settings.py COLOR_* 1:1 (Core는 색을 모름, 렌더 전용).</summary>
public static class Palette
{
    // 팔레트 기본
    public static readonly Color Jade = new(78, 206, 170);
    public static readonly Color JadeDeep = new(40, 130, 120);
    public static readonly Color Mint = new(158, 240, 212);
    public static readonly Color Milky = new(232, 246, 240);
    public static readonly Color MistGray = new(120, 150, 152);
    public static readonly Color Gold = new(255, 208, 120);
    public static readonly Color Coral = new(236, 96, 92);
    public static readonly Color Magenta = new(212, 92, 150);
    public static readonly Color Violet = new(176, 158, 232);

    // 배경
    public static readonly Color Bg = new(14, 30, 36);
    public static readonly Color BgTop = new(12, 26, 34);
    public static readonly Color BgBottom = new(22, 48, 52);
    public static readonly Color Firefly = Gold;

    // 엔티티/지형
    public static readonly Color Player = Milky;
    public static readonly Color Solid = new(58, 84, 86);
    public static readonly Color Platform = Mint;
    public static readonly Color Hazard = Coral;
    public static readonly Color Checkpoint = JadeDeep;
    public static readonly Color CheckpointOn = Gold;
    public static readonly Color JumpPad = new(110, 220, 228);
    public static readonly Color Spring = Magenta;
    public static readonly Color Ntt = Violet;
    public static readonly Color GrabOk = new(108, 240, 168);
    public static readonly Color GrabNo = Coral;
    public static readonly Color Enemy = new(236, 128, 92);
    public static readonly Color EnemyArmored = Magenta;
    public static readonly Color EnemyHit = MistGray;
    public static readonly Color RopeNtt = new(118, 212, 220);
    public static readonly Color RopeLine = new(84, 110, 112);
    public static readonly Color Goal = Gold;

    // 메뉴
    public static readonly Color MenuOverlay = new(8, 18, 22);
    public static readonly Color MenuTitle = Gold;
    public static readonly Color MenuText = Milky;
    public static readonly Color MenuSelected = Jade;
    public static readonly Color MenuHint = MistGray;
}
