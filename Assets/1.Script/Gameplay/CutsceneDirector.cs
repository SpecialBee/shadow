using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    }
}
