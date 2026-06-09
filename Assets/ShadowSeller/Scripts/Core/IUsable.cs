namespace ShadowSeller.Core
{
    public interface IUsable
    {
        string UseHint { get; }
        void OnUse(PlayerController user);
    }
}
