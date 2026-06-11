// ─────────────────────────────────────────────────────────────────────────────
// GameTypes — 게임 전역 열거형 정의.
//   ExposureState : 플레이어 노출 상태. 의심도 변화율(Rates[])의 인덱스로 사용됨.
//   NpcKind       : NPC 종류 구분 (현재 미사용, 확장 예약).
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

    public enum NpcKind  { Guest, Guard, Target }

    public enum NpcState { Idle, Patrol, Suspicious, Alert, Chase, Search }
}
