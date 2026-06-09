namespace ShadowSeller.Core
{
    public enum ExposureState
    {
        Dark         = 0,
        Shadow       = 1,
        Lit          = 2,
        ExposedSight = 3,
        ExposedClose = 4,
    }

    public enum NpcKind  { Guest, Guard, Target }

    public enum NpcState { Idle, Patrol, Suspicious, Alert, Chase, Search }
}
