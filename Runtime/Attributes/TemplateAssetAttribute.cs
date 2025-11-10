using System;

namespace Kott.Configs.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class TemplateAssetAttribute : Attribute
    {
        public TemplateAssetAttribute(Type assetType, string assetKey)
        {
            AssetType = assetType;
            AssetKey = assetKey;
        }

        public Type AssetType { get; }
        public string AssetKey { get; }
        public bool IsOptional { get; set; }
    }
}
