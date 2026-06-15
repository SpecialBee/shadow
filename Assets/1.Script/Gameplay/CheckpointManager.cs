using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ShadowSeller.UI;

namespace ShadowSeller.Core
{
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        public bool HasCheckpoint => _state.hasCheckpoint;

        // ── 저장 상태 ─────────────────────────────────────────────────────────
        private struct SaveState
        {
            public bool                        hasCheckpoint;
            public string                      sceneName;
            public Vector2                     playerPosition;
            public bool                        objectiveComplete;
            public List<InventoryManager.ItemData> inventory;
            public HashSet<string>             collectedIDs;
        }

        private SaveState          _state;
        private HashSet<string>    _runtimeCollectedIDs = new HashSet<string>();
        private bool               _pendingRestore;

        private const string KEY_HAS   = "cp_has";
        private const string KEY_SCENE = "cp_scene";
        private const string KEY_X     = "cp_x";
        private const string KEY_Y     = "cp_y";

        // ── 초기화 ───────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryLoadFromDisk();
        }

        // ── 공개 API ──────────────────────────────────────────────────────────

        public void SaveCheckpoint(Vector2 playerPos)
        {
            _state.hasCheckpoint  = true;
            _state.sceneName      = SceneManager.GetActiveScene().name;
            _state.playerPosition = playerPos;
            _state.objectiveComplete = ObjectiveManager.Instance?.IsComplete ?? false;

            // 인벤토리 스냅샷
            _state.inventory = new List<InventoryManager.ItemData>();
            if (InventoryManager.Instance != null)
                for (int i = 0; i < InventoryManager.MaxSlots; i++)
                {
                    var slot = InventoryManager.Instance.GetSlot(i);
                    if (slot.HasValue) _state.inventory.Add(slot.Value);
                }

            // 수집 오브젝트 스냅샷
            _state.collectedIDs = new HashSet<string>(_runtimeCollectedIDs);

            SaveToDisk();
            Debug.Log($"[CheckpointManager] 저장: pos={playerPos}, 아이템={_state.inventory.Count}, 수집={_state.collectedIDs.Count}");
        }

        // ── 디스크 저장/로드 (PlayerPrefs — 씬·위치만 저장) ────────────────────
        private void SaveToDisk()
        {
            PlayerPrefs.SetInt(KEY_HAS,   1);
            PlayerPrefs.SetString(KEY_SCENE, _state.sceneName);
            PlayerPrefs.SetFloat(KEY_X,   _state.playerPosition.x);
            PlayerPrefs.SetFloat(KEY_Y,   _state.playerPosition.y);
            PlayerPrefs.Save();
        }

        private void TryLoadFromDisk()
        {
            if (PlayerPrefs.GetInt(KEY_HAS, 0) == 0) return;
            _state.hasCheckpoint   = true;
            _state.sceneName       = PlayerPrefs.GetString(KEY_SCENE, "");
            _state.playerPosition  = new Vector2(
                PlayerPrefs.GetFloat(KEY_X, 0f),
                PlayerPrefs.GetFloat(KEY_Y, 0f));
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(KEY_HAS);
            PlayerPrefs.DeleteKey(KEY_SCENE);
            PlayerPrefs.DeleteKey(KEY_X);
            PlayerPrefs.DeleteKey(KEY_Y);
            PlayerPrefs.Save();
            _state = default;
        }

        public void RegisterCollected(string stableID)
        {
            _runtimeCollectedIDs.Add(stableID);
        }

        public bool IsCollected(string stableID)
        {
            return _runtimeCollectedIDs.Contains(stableID);
        }

        public void Respawn()
        {
            if (!_state.hasCheckpoint) return;

            // 런타임 수집 목록을 체크포인트 스냅샷으로 되돌림
            _runtimeCollectedIDs.Clear();
            if (_state.collectedIDs != null)
                foreach (var id in _state.collectedIDs)
                    _runtimeCollectedIDs.Add(id);

            StartCoroutine(RespawnRoutine());
        }

        // ── 리스폰 흐름 ───────────────────────────────────────────────────────

        private IEnumerator RespawnRoutine()
        {
            yield return StartCoroutine(SceneFader.Instance.FadeOut());

            _pendingRestore = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(_state.sceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_pendingRestore) StartCoroutine(ApplyStateRoutine());
        }

        private IEnumerator ApplyStateRoutine()
        {
            _pendingRestore = false;

            // Start()가 모두 실행된 후 상태 복원
            yield return new WaitForEndOfFrame();

            // 플레이어 위치
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.position = _state.playerPosition;
                player.IsLocked = false;
            }

            // 의심도 초기화
            SuspicionManager.Instance?.ResetForRespawn();

            // 목표 복원
            if (_state.objectiveComplete) ObjectiveManager.Instance?.Complete();

            // 인벤토리 복원
            InventoryManager.Instance?.RestoreFromSave(_state.inventory);

            // InputReader 재활성화
            var reader = FindAnyObjectByType<InputReader>();
            if (reader != null) reader.enabled = true;

            yield return StartCoroutine(SceneFader.Instance.FadeIn());
        }
    }
}
