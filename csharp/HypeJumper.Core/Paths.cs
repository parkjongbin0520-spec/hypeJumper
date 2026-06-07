using System;
using System.IO;

namespace HypeJumper.Core;

/// <summary>리소스 경로 해석 — 출력 폴더(assets/ 복사본) 기준. paths.py 대응.</summary>
public static class Paths
{
    /// <summary>상대 경로(예: "assets/tilemaps/level1.txt")를 실행 파일 기준 절대 경로로 변환.</summary>
    public static string ResourcePath(string relative)
        => Path.Combine(AppContext.BaseDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
}
