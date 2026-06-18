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
        PlaySound,   // AudioClip 즉시 재생 (waitForComplete=true → 끝날 때까지 대기)
        PlayVideo,   // VideoClip 전체화면 재생
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

        // 사운드
        public AudioClip soundClip;

        // 영상
        public UnityEngine.Video.VideoClip videoClip;
        [Tooltip("최대 재생 시간 (초). 0 = 끝날 때까지 대기")]
        public float videoTimeout    = 0f;
        [Tooltip("영상 소리 재생 여부")]
        public bool  videoAudio      = true;
        [Tooltip("영상 시작/끝 페이드 시간 (초)")]
        public float videoFadeDuration = 0.5f;
    }
}
