using System;

namespace Kott.Configs
{
    public class TemplateReferenceConverter : StringReferenceConverter<ITemplateReference>
    {
        protected override string GetInstanceId(ITemplateReference instance)
        {
            return instance.Template ? instance.Template.ID : null;
        }

        protected override ITemplateReference CreateInstance(Type type, string id)
        {
            ITemplateReference instance = (ITemplateReference)Activator.CreateInstance(type);
            instance.Template = null;
            return instance;
        }
    }
}
