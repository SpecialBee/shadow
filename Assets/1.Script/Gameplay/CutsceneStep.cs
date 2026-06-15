using UnityEngine;

namespace ShadowSeller.Core
{
    public enum CutsceneStepType
    {
        Dialogue,
        MovePlayer,
        MoveObject,
        MoveCamera,
        Wait,
        Fade,
    }

    [System.Serializable]
    public class CutsceneStep
    {
        public CutsceneStepType type          = CutsceneStepType.Wait;
        public bool             waitForComplete = true;

        // 대화
        public DialogueData dialogue;

        // 이동 (플레이어/오브젝트 공통)
        public Transform objectToMove;
        public Transform moveTo;
        public float     moveDuration = 1f;
        public bool      smoothMove   = true;

        // 카메라
        public Transform cameraTarget;
        public float     cameraDuration    = 0.5f;
        public bool      cameraFollowAfter = false;

        // 대기
        public float waitSeconds = 0.5f;

        // 페이드
        public bool  fadeOut      = true;
        public float fadeDuration = 0.4f;
    }
}
