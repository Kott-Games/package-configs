namespace Kott.Configs
{
    public interface ITemplateAssetReference
    {
#if UNITY_EDITOR
        UnityEngine.Object Asset { get; }
#endif
        string Address { get; set; }
    }
}
