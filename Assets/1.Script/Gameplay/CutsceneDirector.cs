using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using ShadowSeller.UI;

namespace ShadowSeller.Core
{
    public class CutsceneDirector : MonoBehaviour
    {
        public static CutsceneDirector Instance { get; private set; }

        public bool IsPlaying { get; private set; }

        private PlayerController _player;
        private CameraFollow     _cameraFollow;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _player       = Object.FindAnyObjectByType<PlayerController>();
            _cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        }

        public void Play(CutsceneStep[] steps, bool overrideSpawn = false, Transform spawnPoint = null, System.Action onComplete = null)
        {
            if (IsPlaying) return;
            StartCoroutine(PlayRoutine(steps, overrideSpawn, spawnPoint, onComplete));
        }

        private IEnumerator PlayRoutine(CutsceneStep[] steps, bool overrideSpawn, Transform spawnPoint, System.Action onComplete)
        {
            IsPlaying = true;

            // 이전 대화창이 열려있거나 페이드 중이면 즉시 숨김
            DialogueSystem.Instance?.ForceHide();

            if (_player != null) _player.IsLocked = true;

            // 레터박스 + HUD 숨김
            if (CinematicBars.Instance != null)
                yield return StartCoroutine(CinematicBars.Instance.Enter());

            if (steps != null)
            {
                var running = new List<Coroutine>();

                foreach (var step in steps)
                {
                    if (step.waitForComplete)
                    {
                        foreach (var c in running) yield return c;
                        running.Clear();
                        yield return StartCoroutine(ExecuteStep(step));
                    }
                    else
                    {
                        running.Add(StartCoroutine(ExecuteStep(step)));
                    }
                }

                foreach (var c in running) yield return c;
            }

            if (overrideSpawn && spawnPoint != null && _player != null)
            {
                _player.transform.position = spawnPoint.position;
                var rb = _player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.position = spawnPoint.position;
            }

            if (_cameraFollow != null) _cameraFollow.enabled = true;

            // 레터박스 아웃 + HUD 복원
            if (CinematicBars.Instance != null)
                yield return StartCoroutine(CinematicBars.Instance.Exit());

            if (_player != null) _player.IsLocked = false;

            IsPlaying = false;
            onComplete?.Invoke();
        }

        private IEnumerator ExecuteStep(CutsceneStep step)
        {
            // 대화 스텝이 아니면 대화창 즉시 숨김
            if (step.type != CutsceneStepType.Dialogue)
                DialogueSystem.Instance?.ForceHide();

            switch (step.type)
            {
                case CutsceneStepType.Dialogue:
                    yield return StartCoroutine(StepDialogue(step));
                    break;
                case CutsceneStepType.MovePlayer:
                    yield return StartCoroutine(StepMovePlayer(step));
                    break;
                case CutsceneStepType.MoveObject:
                    yield return StartCoroutine(StepMoveObject(step));
                    break;
                case CutsceneStepType.MoveCamera:
                    yield return StartCoroutine(StepMoveCamera(step));
                    break;
                case CutsceneStepType.Wait:
                    yield return new WaitForSeconds(step.waitSeconds);
                    break;
                case CutsceneStepType.Fade:
                    if (SceneFader.Instance != null)
                    {
                        if (step.fadeOut) yield return StartCoroutine(SceneFader.Instance.FadeOut());
                        else              yield return StartCoroutine(SceneFader.Instance.FadeIn());
                    }
                    break;
                case CutsceneStepType.PlaySound:
                    yield return StartCoroutine(StepPlaySound(step));
                    break;
                case CutsceneStepType.PlayVideo:
                    yield return StartCoroutine(StepPlayVideo(step));
                    break;
            }
        }

        private IEnumerator StepDialogue(CutsceneStep step)
        {
            if (step.dialogue == null || DialogueSystem.Instance == null) yield break;
            bool done = false;
            DialogueSystem.Instance.StartDialogue(step.dialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        private IEnumerator StepMovePlayer(CutsceneStep step)
        {
            if (_player == null || step.moveTo == null) yield break;
            bool arrived = false;
            _player.WalkTo(step.moveTo.position, () => arrived = true);
            yield return new WaitUntil(() => arrived);
        }

        private IEnumerator StepMoveObject(CutsceneStep step)
        {
            if (step.objectToMove == null || step.moveTo == null) yield break;

            Vector3 start    = step.objectToMove.position;
            Vector3 end      = step.moveTo.position;
            float   elapsed  = 0f;
            float   duration = Mathf.Max(0.01f, step.moveDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / duration);
                float factor = step.smoothMove ? t * t * (3f - 2f * t) : t;
                step.objectToMove.position = Vector3.Lerp(start, end, factor);
                yield return null;
            }
            step.objectToMove.position = end;
        }

        private IEnumerator StepMoveCamera(CutsceneStep step)
        {
            if (Camera.main == null || step.cameraTarget == null) yield break;

            if (_cameraFollow != null) _cameraFollow.enabled = false;

            Vector3 start    = Camera.main.transform.position;
            Vector3 end      = new Vector3(step.cameraTarget.position.x,
                                           step.cameraTarget.position.y,
                                           Camera.main.transform.position.z);
            float elapsed  = 0f;
            float duration = Mathf.Max(0.01f, step.cameraDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / duration);
                float factor = t * t * (3f - 2f * t);
                Camera.main.transform.position = Vector3.Lerp(start, end, factor);
                yield return null;
            }
            Camera.main.transform.position = end;

            if (step.cameraFollowAfter && _cameraFollow != null)
                _cameraFollow.enabled = true;
        }

        // ── PlaySound ─────────────────────────────────────────────────────────

        private IEnumerator StepPlaySound(CutsceneStep step)
        {
            if (step.soundClip == null) yield break;

            var pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(step.soundClip, pos);

            // waitForComplete=true 이면 클립 길이만큼 대기
            if (step.waitForComplete)
                yield return new WaitForSeconds(step.soundClip.length);
        }

        // ── PlayVideo ─────────────────────────────────────────────────────────

        private IEnumerator StepPlayVideo(CutsceneStep step)
        {
            if (step.videoClip == null) yield break;

            // Canvas 탐색
            Canvas canvas = null;
            var cGo = GameObject.Find("UICanvas");
            if (cGo != null) canvas = cGo.GetComponent<Canvas>();
            if (canvas == null)
                foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.renderMode != RenderMode.WorldSpace) { canvas = c; break; }
            if (canvas == null) yield break;

            // RenderTexture
            int w = Mathf.Max(1, (int)step.videoClip.width);
            int h = Mathf.Max(1, (int)step.videoClip.height);
            var rt = new RenderTexture(w, h, 0);

            // 전체화면 검정 패널
            var panelGo    = new GameObject("_VideoPanel", typeof(RectTransform));
            panelGo.transform.SetParent(canvas.transform, false);
            panelGo.transform.SetAsLastSibling();
            var panelRT    = panelGo.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            var panelImg   = panelGo.AddComponent<Image>();
            panelImg.color = Color.black;

            // RawImage + CanvasGroup (페이드용)
            var rawGo  = new GameObject("_VideoRaw", typeof(RectTransform));
            rawGo.transform.SetParent(panelGo.transform, false);
            var rawRT  = rawGo.GetComponent<RectTransform>();
            rawRT.anchorMin = Vector2.zero;
            rawRT.anchorMax = Vector2.one;
            rawRT.offsetMin = Vector2.zero;
            rawRT.offsetMax = Vector2.zero;
            var rawImg = rawGo.AddComponent<RawImage>();
            rawImg.texture = rt;
            var cg     = rawGo.AddComponent<CanvasGroup>();
            cg.alpha   = 0f;

            // VideoPlayer
            var vpGo  = new GameObject("_VideoPlayer");
            var vp    = vpGo.AddComponent<VideoPlayer>();
            vp.playOnAwake      = false;
            vp.clip             = step.videoClip;
            vp.renderMode       = VideoRenderMode.RenderTexture;
            vp.targetTexture    = rt;
            vp.isLooping        = false;

            if (step.videoAudio)
            {
                vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
                var asSrc = vpGo.AddComponent<AudioSource>();
                vp.SetTargetAudioSource(0, asSrc);
            }
            else
            {
                vp.audioOutputMode = VideoAudioOutputMode.None;
            }

            vp.Prepare();
            yield return new WaitUntil(() => vp.isPrepared);
            vp.Play();

            // 페이드 인
            float fd = Mathf.Max(0f, step.videoFadeDuration);
            for (float t = 0f; t < fd; t += Time.deltaTime)
            {
                if (cg != null) cg.alpha = t / fd;
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;

            // 재생 대기
            float timeout = step.videoTimeout > 0f ? step.videoTimeout : float.MaxValue;
            float elapsed = 0f;
            while (vp.isPlaying && elapsed < timeout - fd)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 페이드 아웃
            for (float t = 0f; t < fd; t += Time.deltaTime)
            {
                if (cg != null) cg.alpha = 1f - t / fd;
                yield return null;
            }
            if (cg != null) cg.alpha = 0f;

            vp.Stop();
            rt.Release();
            Destroy(vpGo);
            Destroy(panelGo);
        }
    }
}
