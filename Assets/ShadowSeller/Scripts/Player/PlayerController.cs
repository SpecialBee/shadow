using UnityEngine;

namespace ShadowSeller.Core
{
    // 플레이어 이동 — InputReader.MoveInput을 받아 Rigidbody2D 속도로 변환.
    // 씬 시작 시 spawnPoint 위치로 이동. 스프라이트 미설정 시 임시 파란 사각형 생성.
    public class PlayerController : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.PlayerMove;

        [SerializeField] private float       moveSpeed  = 4f;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform   spawnPoint;

        private Rigidbody2D _rb;
        private InputReader _input;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _input = GetComponent<InputReader>();

            if (bodyRenderer == null)
                bodyRenderer = GetComponent<SpriteRenderer>();

            if (bodyRenderer != null && bodyRenderer.sprite == null)
                bodyRenderer.sprite = CreatePlaceholderSprite();

            GameLoopController.Instance.Register(this);
        }

        private void Start()
        {
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                if (_rb != null) _rb.position = spawnPoint.position;
            }
        }

        private void OnDestroy()
        {
            GameLoopController.Instance?.Unregister(this);
        }

        public void Tick()
        {
            _rb.linearVelocity = _input.MoveInput * moveSpeed;
        }

        public Vector2 FootPoint => (Vector2)transform.position + Vector2.down * 0.25f;

        private static Sprite CreatePlaceholderSprite()
        {
            const int size = 32;
            var tex    = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0.3f, 0.8f, 1f);
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
