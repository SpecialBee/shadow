using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShadowSeller.Core
{
    public enum PrologueStepType
    {
        Dialogue,    // DialogueData 순차 재생
        MovePlayer,  // 플레이어를 moveTarget으로 순간이동
        PlaySound,   // AudioClip 1회 재생
        Wait,        // waitSeconds만큼 대기
    }

    [Serializable]
    public class PrologueStep
    {
        public PrologueStepType type = PrologueStepType.Dialogue;

        [Tooltip("type = Dialogue — 재생할 대화 데이터")]
        public DialogueData dialogue;

        [Tooltip("type = MovePlayer — 플레이어를 이동시킬 목표 Transform")]
        public Transform moveTarget;

        [Tooltip("type = PlaySound — 재생할 AudioClip")]
        public AudioClip sound;

        [Tooltip("type = Wait — 대기 시간 (초)")]
        [Range(0f, 10f)]
        public float waitSeconds = 0.5f;
    }

    [Serializable]
    public class ChapterData
    {
        public string chapterName = "챕터";

        [Tooltip("플레이어가 이 챕터에서 시작할 위치")]
        public Transform spawnPoint;

        [Tooltip("카메라가 이동할 목표 위치")]
        public Transform cameraTarget;

        [Tooltip("true = 챕터 내에서 카메라가 플레이어를 따라감 / false = cameraTarget 고정")]
        public bool cameraFollowsPlayer = false;

        [Tooltip("페이드 아웃 후 검정 화면에서 재생할 나레이션 (null이면 생략)")]
        public DialogueData blackScreenDialogue;

        [Tooltip("페이드 인 후 순서대로 실행할 스텝 (대화 / 플레이어 이동 / 사운드 / 대기)")]
        public PrologueStep[] introSteps;

        [Tooltip("true = 스텝 완료 후 자동으로 다음 챕터 / false = PrologueTrigger를 기다림")]
        public bool autoAdvance = true;

        [Tooltip("페이드 인/아웃 소요 시간 (초)")]
        [Range(0.3f, 3f)]
        public float fadeDuration = 0.6f;
    }

    // 프롤로그 씬 전체 흐름 제어 — 싱글톤
    //   챕터 순서: 페이드 아웃 → 검정 나레이션 → 플레이어·카메라 이동 → 페이드 인 → introSteps 실행
    //   마지막 챕터 완료 시 nextSceneName으로 씬 전환
    public class PrologueDirector : MonoBehaviour
    {
        public static PrologueDirector Instance { get; private set; }

        [Header("챕터 목록 (인덱스 0부터 순서대로 실행)")]
        [SerializeField] private ChapterData[] chapters;

        [Header("마지막 챕터 완료 후 로드할 씬 이름")]
        [SerializeField] private string nextSceneName = "MainPlay";

        [Header("페이드 이미지 — Canvas 하위 전체 화면 검정 Image")]
        [SerializeField] private Image fadeImage;

        [Header("사운드 스텝용 AudioSource (비워두면 PlayClipAtPoint 사용)")]
        [SerializeField] private AudioSource sfxSource;

        // ── 런타임 ────────────────────────────────────────────────────────────
        private int              _currentChapter = -1;
        private bool             _transitioning  = false;
        private PlayerController _player;
        private CameraFollow     _cameraFollow;

        public int  CurrentChapterIndex => _currentChapter;
        public bool IsTransitioning     => _transitioning;

        // ── 초기화 ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _player       = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            _cameraFollow = Camera.main?.GetComponent<CameraFollow>();

            if (_cameraFollow != null) _cameraFollow.enabled = false;

            SetFadeAlpha(1f);
            if (fadeImage != null) fadeImage.gameObject.SetActive(true);

            if (chapters != null && chapters.Length > 0)
                StartCoroutine(EnterChapter(0));
            else
                Debug.LogError("[PrologueDirector] chapters 배열이 비어 있습니다.");
        }

        // ── 공개 API ──────────────────────────────────────────────────────────

        public void AdvanceChapter()
        {
            if (_transitioning) return;
            StartCoroutine(TransitionToNext());
        }

        // ── 챕터 진입 (첫 챕터 전용) ─────────────────────────────────────────

        private IEnumerator EnterChapter(int index)
        {
            _transitioning  = true;
            _currentChapter = index;

            var chapter = chapters[index];

            SetPlayerLocked(true);
            TeleportPlayer(chapter.spawnPoint);
            ApplyCameraForChapter(chapter);

            yield return StartCoroutine(DoFade(1f, 0f, chapter.fadeDuration, easeOut: true));

            yield return StartCoroutine(RunSteps(chapter.introSteps));

            if (chapter.autoAdvance)
                yield return StartCoroutine(TransitionToNext());
            else
            {
                SetPlayerLocked(false);
                _transitioning = false;
            }
        }

        // ── 챕터 전환 ─────────────────────────────────────────────────────────

        private IEnumerator TransitionToNext()
        {
            _transitioning = true;

            int  nextIndex = _currentChapter + 1;
            bool isLast    = nextIndex >= chapters.Length;

            SetPlayerLocked(true);

            if (_cameraFollow != null) _cameraFollow.enabled = false;

            float fadeDur = chapters[_currentChapter].fadeDuration;
            yield return StartCoroutine(DoFade(0f, 1f, fadeDur, easeOut: false));
            SetFadeAlpha(1f);

            if (isLast)
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }

            _currentChapter = nextIndex;
            var chapter = chapters[nextIndex];

            if (chapter.blackScreenDialogue != null)
                yield return StartCoroutine(PlayDialogue(chapter.blackScreenDialogue));
            else
                yield return new WaitForSeconds(0.15f);

            TeleportPlayer(chapter.spawnPoint);
            ApplyCameraForChapter(chapter);

            yield return StartCoroutine(DoFade(1f, 0f, chapter.fadeDuration, easeOut: true));

            yield return StartCoroutine(RunSteps(chapter.introSteps));

            if (chapter.autoAdvance)
                yield return StartCoroutine(TransitionToNext());
            else
            {
                SetPlayerLocked(false);
                _transitioning = false;
            }
        }

        // ── 스텝 실행 ─────────────────────────────────────────────────────────

        private IEnumerator RunSteps(PrologueStep[] steps)
        {
            if (steps == null) yield break;

            foreach (var step in steps)
            {
                switch (step.type)
                {
                    case PrologueStepType.Dialogue:
                        yield return StartCoroutine(PlayDialogue(step.dialogue));
                        break;

                    case PrologueStepType.MovePlayer:
                        if (_player != null && step.moveTarget != null)
                        {
                            bool arrived = false;
                            _player.WalkTo(step.moveTarget.position, () => arrived = true);
                            yield return new WaitUntil(() => arrived);
                        }
                        break;

                    case PrologueStepType.PlaySound:
                        if (step.sound != null)
                        {
                            if (sfxSource != null)
                                sfxSource.PlayOneShot(step.sound);
                            else
                                AudioSource.PlayClipAtPoint(step.sound, Camera.main.transform.position);
                        }
                        break;

                    case PrologueStepType.Wait:
                        yield return new WaitForSeconds(step.waitSeconds);
                        break;
                }
            }
        }

        // ── 유틸 ──────────────────────────────────────────────────────────────

        private IEnumerator DoFade(float from, float to, float duration, bool easeOut = false)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t   = Mathf.Clamp01(elapsed / duration);
                float val = easeOut ? 1f - (1f - t) * (1f - t) : t * t;
                SetFadeAlpha(Mathf.Lerp(from, to, val));
                yield return null;
            }
            SetFadeAlpha(to);
        }

        private IEnumerator PlayDialogue(DialogueData data)
        {
            if (data == null || DialogueSystem.Instance == null) yield break;
            bool done = false;
            DialogueSystem.Instance.StartDialogue(data, () => done = true);
            yield return new WaitUntil(() => done);
        }

        private void ApplyCameraForChapter(ChapterData chapter)
        {
            if (Camera.main == null) return;

            if (chapter.cameraTarget != null)
            {
                var pos = chapter.cameraTarget.position;
                pos.z = Camera.main.transform.position.z;
                Camera.main.transform.position = pos;
            }

            if (_cameraFollow != null)
                _cameraFollow.enabled = chapter.cameraFollowsPlayer;
        }

        private void TeleportPlayer(Transform target)
        {
            if (_player == null || target == null) return;
            _player.transform.position = target.position;
            var rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = target.position;
        }

        private void SetPlayerLocked(bool locked)
        {
            if (_player != null) _player.IsLocked = locked;
        }

        private void SetFadeAlpha(float a)
        {
            if (fadeImage == null) return;
            var c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;
        }
    }
}
