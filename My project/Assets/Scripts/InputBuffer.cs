// 통합 버퍼 — Python InputBuffer 이식. 코요테/점프버퍼 공통. 엔진 무관 순수 C#.
namespace HypeJumper
{
    public class InputBuffer
    {
        private int frames;
        private readonly int maxFrames;

        public InputBuffer(int maxFrames) { this.maxFrames = maxFrames; frames = 0; }

        public void Set() { frames = maxFrames; }          // 최대치로 채워 활성화
        public void Tick() { if (frames > 0) frames--; }   // 매 프레임 1 감소
        public bool IsActive() { return frames > 0; }       // 활성 여부
        public void Consume() { frames = 0; }               // 사용 시 즉시 소멸
    }
}
