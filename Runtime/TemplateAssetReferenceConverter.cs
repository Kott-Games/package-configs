using System;

namespace Kott.Configs
{
    public class TemplateAssetReferenceConverter : StringReferenceConverter<ITemplateAssetReference>
    {
        protected override string GetInstanceId(ITemplateAssetReference instance)
        {
            return instance.Address;
        }

        protected override ITemplateAssetReference CreateInstance(Type type, string id)
        {
            ITemplateAssetReference instance = (ITemplateAssetReference)Activator.CreateInstance(type);
            instance.Address = id;
            instance.Asset = null;
            return instance;
        }
    }
}
