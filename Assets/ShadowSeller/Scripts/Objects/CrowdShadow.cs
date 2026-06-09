using System.Collections;
using UnityEngine;

namespace ShadowSeller.Core
{
    // 군중 그림자: lifetime 후 소멸. 소멸 blinkWarningTime 전 깜빡임 경고.
    public class CrowdShadow : MonoBehaviour
    {
        [SerializeField] private ShadowZone      shadowZone;
        [SerializeField] private SpriteRenderer  bodyRenderer;
        [SerializeField] private float           lifetime        = 15f;
        [SerializeField] private float           blinkWarningTime = 1.5f;
        [SerializeField] private float           blinkInterval   = 0.18f;

        private float _timer;

        private void Start()
        {
            _timer = lifetime;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;

            if (_timer <= blinkWarningTime && _timer + Time.deltaTime > blinkWarningTime)
                StartCoroutine(BlinkRoutine());

            if (_timer <= 0f)
                Destroy(gameObject);
        }

        private IEnumerator BlinkRoutine()
        {
            while (_timer > 0f)
            {
                bool on = (Mathf.FloorToInt(_timer / blinkInterval) % 2) == 0;
                if (bodyRenderer != null) bodyRenderer.enabled = on;
                if (shadowZone   != null) shadowZone.gameObject.SetActive(on);
                yield return null;
            }
        }
    }
}
