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
