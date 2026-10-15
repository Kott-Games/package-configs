namespace Kott.Configs
{
    public static class TemplateExtensions
    {
        public static string FormatID(this Template template)
        {
            if (template == null) return "[NULL]";
            return $"[{template.ID}]";
        }
    }
}
