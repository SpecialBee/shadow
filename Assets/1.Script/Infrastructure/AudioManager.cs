using System.Collections;
using UnityEngine;

namespace ShadowSeller.Core
{
    public enum BGMTrack { None, MainMenu, Prologue, StageAmbient, StageAlertL2, StageAlertL3, StageAlertL4 }

    public enum SFXClip
    {
        FootStep, CarryPickup, CarryDrop, ItemPickup, ItemReceive,
        DoorOpen, DoorClose, LightOn, LightOff, ObjectSlide,
        NpcSuspicious, NpcAlert, NpcSearch, NpcArrest,
        CheckpointSave, AlertLevelUp, SuspicionSpike,
        DialogueNext, UIClick, CutsceneLetterbox
    }

    // BGM + SFX 통합 관리 싱글턴. DontDestroyOnLoad.
    // BGM은 AlertManager.Level 변화에 따라 자동 페이드 전환.
    // 볼륨은 PlayerPrefs에 저장.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM 클립")]
        [SerializeField] private AudioClip bgmMainMenu;
        [SerializeField] private AudioClip bgmPrologue;
        [SerializeField] private AudioClip bgmStageAmbient;
        [SerializeField] private AudioClip bgmStageAlertL2;
        [SerializeField] private AudioClip bgmStageAlertL3;
        [SerializeField] private AudioClip bgmStageAlertL4;

        [Header("SFX — 플레이어")]
        [SerializeField] private AudioClip sfxFootstep;
        [SerializeField] private AudioClip sfxCarryPickup;
        [SerializeField] private AudioClip sfxCarryDrop;
        [SerializeField] private AudioClip sfxItemPickup;
        [SerializeField] private AudioClip sfxItemReceive;

        [Header("SFX — 상호작용")]
        [SerializeField] private AudioClip sfxDoorOpen;
        [SerializeField] private AudioClip sfxDoorClose;
        [SerializeField] private AudioClip sfxLightOn;
        [SerializeField] private AudioClip sfxLightOff;
        [SerializeField] private AudioClip sfxObjectSlide;

        [Header("SFX — NPC")]
        [SerializeField] private AudioClip sfxNpcSuspicious;
        [SerializeField] private AudioClip sfxNpcAlert;
        [SerializeField] private AudioClip sfxNpcSearch;
        [SerializeField] private AudioClip sfxNpcArrest;

        [Header("SFX — 시스템")]
        [SerializeField] private AudioClip sfxCheckpointSave;
        [SerializeField] private AudioClip sfxAlertLevelUp;
        [SerializeField] private AudioClip sfxSuspicionSpike;
        [SerializeField] private AudioClip sfxDialogueNext;
        [SerializeField] private AudioClip sfxUIClick;
        [SerializeField] private AudioClip sfxCutsceneLetterbox;

        [Header("기본 볼륨")]
        [Range(0f, 1f)] [SerializeField] private float defaultBGMVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float defaultSFXVolume = 1.0f;

        [Header("BGM 페이드")]
        [SerializeField] private float fadeDuration = 0.8f;

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private BGMTrack    _currentTrack = BGMTrack.None;
        private Coroutine   _fadeCoroutine;
        private float       _bgmTargetVol;
        private float       _sfxTargetVol;

        private const string KEY_BGM = "vol_bgm";
        private const string KEY_SFX = "vol_sfx";

        // ── 볼륨 프로퍼티 ────────────────────────────────────────────
        public float BGMVolume
        {
            get => _bgmTargetVol;
            set
            {
                _bgmTargetVol = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KEY_BGM, _bgmTargetVol);
                if (_fadeCoroutine == null) _bgmSource.volume = _bgmTargetVol;
            }
        }

        public float SFXVolume
        {
            get => _sfxTargetVol;
            set
            {
                _sfxTargetVol = Mathf.Clamp01(value);
                _sfxSource.volume = _sfxTargetVol;
                PlayerPrefs.SetFloat(KEY_SFX, _sfxTargetVol);
            }
        }

        // ── 초기화 ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmTargetVol = PlayerPrefs.GetFloat(KEY_BGM, defaultBGMVolume);
            _sfxTargetVol = PlayerPrefs.GetFloat(KEY_SFX, defaultSFXVolume);

            _bgmSource             = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop        = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.volume      = _bgmTargetVol;

            _sfxSource             = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop        = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume      = _sfxTargetVol;

            AlertManager.OnAlertLevelChanged += OnAlertLevelChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                AlertManager.OnAlertLevelChanged -= OnAlertLevelChanged;
        }

        // ── BGM ──────────────────────────────────────────────────────
        private void OnAlertLevelChanged(int level)
        {
            var track = level switch
            {
                2 => BGMTrack.StageAlertL2,
                3 => BGMTrack.StageAlertL3,
                4 => BGMTrack.StageAlertL4,
                _ => BGMTrack.StageAmbient,
            };
            PlayBGM(track);
        }

        public void PlayBGM(BGMTrack track)
        {
            if (_currentTrack == track) return;
            _currentTrack = track;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeBGM(GetBGMClip(track)));
        }

        public void StopBGM()
        {
            _currentTrack = BGMTrack.None;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeBGM(null));
        }

        private IEnumerator FadeBGM(AudioClip newClip)
        {
            float half = fadeDuration * 0.5f;

            // 페이드 아웃
            if (_bgmSource.isPlaying)
            {
                float startVol = _bgmSource.volume;
                float elapsed  = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / half);
                    yield return null;
                }
                _bgmSource.Stop();
            }

            if (newClip == null) { _bgmSource.volume = _bgmTargetVol; _fadeCoroutine = null; yield break; }

            // 페이드 인
            _bgmSource.clip   = newClip;
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            float elapsed2 = 0f;
            while (elapsed2 < half)
            {
                elapsed2 += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, _bgmTargetVol, elapsed2 / half);
                yield return null;
            }
            _bgmSource.volume = _bgmTargetVol;
            _fadeCoroutine = null;
        }

        // ── SFX ──────────────────────────────────────────────────────
        public void PlaySFX(SFXClip clip)
        {
            var audioClip = GetSFXClip(clip);
            if (audioClip == null) return;
            _sfxSource.PlayOneShot(audioClip, _sfxTargetVol);
        }

        // ── 클립 매핑 ────────────────────────────────────────────────
        private AudioClip GetBGMClip(BGMTrack track) => track switch
        {
            BGMTrack.MainMenu     => bgmMainMenu,
            BGMTrack.Prologue     => bgmPrologue,
            BGMTrack.StageAmbient => bgmStageAmbient,
            BGMTrack.StageAlertL2 => bgmStageAlertL2,
            BGMTrack.StageAlertL3 => bgmStageAlertL3,
            BGMTrack.StageAlertL4 => bgmStageAlertL4,
            _                     => null,
        };

        private AudioClip GetSFXClip(SFXClip clip) => clip switch
        {
            SFXClip.FootStep          => sfxFootstep,
            SFXClip.CarryPickup       => sfxCarryPickup,
            SFXClip.CarryDrop         => sfxCarryDrop,
            SFXClip.ItemPickup        => sfxItemPickup,
            SFXClip.ItemReceive       => sfxItemReceive,
            SFXClip.DoorOpen          => sfxDoorOpen,
            SFXClip.DoorClose         => sfxDoorClose,
            SFXClip.LightOn           => sfxLightOn,
            SFXClip.LightOff          => sfxLightOff,
            SFXClip.ObjectSlide       => sfxObjectSlide,
            SFXClip.NpcSuspicious     => sfxNpcSuspicious,
            SFXClip.NpcAlert          => sfxNpcAlert,
            SFXClip.NpcSearch         => sfxNpcSearch,
            SFXClip.NpcArrest         => sfxNpcArrest,
            SFXClip.CheckpointSave    => sfxCheckpointSave,
            SFXClip.AlertLevelUp      => sfxAlertLevelUp,
            SFXClip.SuspicionSpike    => sfxSuspicionSpike,
            SFXClip.DialogueNext      => sfxDialogueNext,
            SFXClip.UIClick           => sfxUIClick,
            SFXClip.CutsceneLetterbox => sfxCutsceneLetterbox,
            _                         => null,
        };
    }
}
