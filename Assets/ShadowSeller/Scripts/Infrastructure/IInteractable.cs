using UnityEngine;

namespace ShadowSeller.Core
{
    public interface IInteractable
    {
        void OnPickup(Transform carrier);
        void OnDrop();
    }
}
