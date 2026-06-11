// ─────────────────────────────────────────────────────────────────────────────
// GameTypes — 게임 전역 열거형 정의.
//   ExposureState : 플레이어 노출 상태.
//   NpcType       : NPC 종류 (Guard=기존 FSM, Civilian=의심도 구간 반응).
//   NpcState      : NPC 행동 상태 FSM 레이블.
// ─────────────────────────────────────────────────────────────────────────────
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

    public enum NpcType  { Guard, Civilian }

    public enum GameOverReason { SuspicionFull, Arrested }

    public enum NpcState { Idle, Patrol, Suspicious, Alert, Chase, Search }
}
