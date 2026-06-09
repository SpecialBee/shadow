using UnityEngine;

namespace ShadowSeller.Core
{
    // 플레이어 이동 — InputReader.MoveInput을 받아 Rigidbody2D 속도로 변환.
    // 씬 시작 시 spawnPoint 위치로 이동. 스프라이트 미설정 시 임시 파란 사각형 생성.
    // LastMoveDir: 마지막으로 이동한 방향 (BringObject 등 방향 의존 기능에서 참조)
    // IsLocked: true이면 이동 입력 무시 (PushObject 슬라이딩 중 등)
    public class PlayerController : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.PlayerMove;

        [SerializeField] private float          moveSpeed  = 4f;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform      spawnPoint;

        private Rigidbody2D _rb;
        private InputReader _input;

        public Vector2 LastMoveDir { get; private set; } = Vector2.down;

        // 이동 잠금 — true이면 속도 0 고정 (PushObject 슬라이딩 중 사용)
        public bool IsLocked { get; set; } = false;

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
            if (IsLocked)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = _input.MoveInput * moveSpeed;

            if (_input.MoveInput.sqrMagnitude > 0.01f)
                LastMoveDir = _input.MoveInput.normalized;
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
