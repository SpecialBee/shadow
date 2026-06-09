using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowSeller.Core
{
    public class InputReader : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.Input;

        public Vector2 MoveInput       { get; private set; }
        public bool    InteractPressed { get; private set; }
        public bool    PickupPressed   { get; private set; }
        public bool    ResetPressed    { get; private set; }

        private void Awake()
        {
            GameLoopController.Instance.Register(this);
        }

        private void OnDestroy()
        {
            GameLoopController.Instance?.Unregister(this);
        }

        public void Tick()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                MoveInput       = Vector2.zero;
                InteractPressed = false;
                PickupPressed   = false;
                ResetPressed    = false;
                return;
            }

            float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            MoveInput = new Vector2(h, v).normalized;

            InteractPressed = kb.eKey.wasPressedThisFrame;
            PickupPressed   = kb.fKey.wasPressedThisFrame;
            ResetPressed    = kb.rKey.wasPressedThisFrame;
        }
    }
}
