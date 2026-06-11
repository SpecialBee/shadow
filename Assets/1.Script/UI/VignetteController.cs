using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 전역 경계 레벨(D)에 따라 화면 주변 비네팅 강도를 조절.
    //   레벨 1/2: 비네팅 없음 (0)
    //   레벨 3:   약하게 (0.28)
    //   레벨 4:   강하게 (0.48)
    // Volume 컴포넌트에 Vignette 오버라이드가 추가돼 있어야 작동함.
    [RequireComponent(typeof(Volume))]
    public class VignetteController : MonoBehaviour
    {
        [SerializeField] private float lerpSpeed = 2f;

        private Vignette _vignette;

        private void Awake()
        {
            GetComponent<Volume>().profile.TryGet(out _vignette);
        }

        private void Update()
        {
            if (_vignette == null) return;

            float target = (AlertManager.Instance?.Level ?? 1) switch
            {
                3 => 0.28f,
                4 => 0.48f,
                _ => 0f,
            };

            _vignette.intensity.value = Mathf.Lerp(
                _vignette.intensity.value, target, Time.deltaTime * lerpSpeed);
        }
    }
}
