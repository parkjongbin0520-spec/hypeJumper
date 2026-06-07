namespace HypeJumper.Core;

/// <summary>프레임 카운트다운식 버퍼 (코요테 타임/입력 버퍼 공통).</summary>
public class InputBuffer
{
    public int Frames;     // 남은 활성 프레임
    public int MaxFrames;  // 활성화 시 채울 최대 프레임

    /// <summary>버퍼 최대 프레임 수를 설정하고 비활성 상태로 시작.</summary>
    public InputBuffer(int maxFrames)
    {
        Frames = 0;
        MaxFrames = maxFrames;
    }

    /// <summary>버퍼를 최대치로 채워 활성화.</summary>
    public void Set() => Frames = MaxFrames;

    /// <summary>매 프레임 1 감소 (0 미만으로는 내려가지 않음).</summary>
    public void Tick() { if (Frames > 0) Frames--; }

    /// <summary>버퍼가 아직 살아있는지 여부.</summary>
    public bool IsActive() => Frames > 0;

    /// <summary>조건 충족으로 사용 시 즉시 소멸.</summary>
    public void Consume() => Frames = 0;
}
