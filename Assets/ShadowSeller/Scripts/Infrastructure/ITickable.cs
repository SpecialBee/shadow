// ─────────────────────────────────────────────────────────────────────────────
// ITickable — 게임루프 참여 계약 인터페이스.
//   TickPhase 값에 따라 GameLoopController가 매 프레임 순서대로 Tick()을 호출함.
//   실행 순서: Input(0) → PlayerMove(10) → Interaction(20) → LightUpdate(30)
//             → NpcAI(40) → ShadowUpdate(45) → SuspicionUpdate(50)
// ─────────────────────────────────────────────────────────────────────────────
namespace ShadowSeller.Core
{
    public enum TickPhase
    {
        Input           = 0,
        PlayerMove      = 10,
        Interaction     = 20,
        LightUpdate     = 30,
        NpcAI           = 40,
        ShadowUpdate    = 45,
        SuspicionUpdate = 50,
    }

    public interface ITickable
    {
        TickPhase Phase { get; }
        void Tick();
    }
}
