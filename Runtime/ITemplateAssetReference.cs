namespace Kott.Configs
{
    /// <summary>
    ///     Untyped access to a <see cref="TemplateAssetReference{TAsset}" /> for the serializer and the editor pipeline.
    /// </summary>
    public interface ITemplateAssetReference
    {
#if UNITY_EDITOR
        /// <summary>The referenced asset (editor only; builds only carry the address).</summary>
        UnityEngine.Object Asset { get; set; }
#endif
        /// <summary>The asset's Addressables address, written by the pipeline on push.</summary>
        string Address { get; set; }
    }
}
