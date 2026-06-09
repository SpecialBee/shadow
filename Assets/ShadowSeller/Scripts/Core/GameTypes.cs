namespace ShadowSeller.Core
{
    public enum ExposureState
    {
        Dark         = 0,
        ShadowA      = 1,
        ShadowB      = 2,
        ShadowC      = 3,
        ShadowD      = 4,
        Lit          = 5,
        ExposedSight = 6,
        ExposedClose = 7,
    }

    public enum NpcKind  { Guest, Guard, Target }

    public enum NpcState { Idle, Patrol, Suspicious, Alert, Chase, Search }
}
