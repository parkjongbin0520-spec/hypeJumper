using System;

namespace HypeJumper.Core;

/// <summary>이동 공통 수식 — 파이썬 approach() 1:1 (player/ntt/enemy 중복 제거).</summary>
public static class MoveMath
{
    /// <summary>value를 target 쪽으로 amount만큼 다가가게 한 뒤 반환 (오버슛 없음).</summary>
    public static double Approach(double value, double target, double amount)
    {
        if (value < target)
            return Math.Min(value + amount, target);
        return Math.Max(value - amount, target);
    }

    /// <summary>2D 거리 — 파이썬 math.hypot 대응(대시 방향 정규화/잡기 거리). sqrt(x²+y²).</summary>
    public static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);
}
