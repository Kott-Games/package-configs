namespace Kott.Configs
{
    /// <summary>
    ///     Untyped access to a <see cref="TemplateReference{TTemplate}" /> for the serializer and the editor pipeline.
    ///     Game code uses <see cref="TemplateReference{TTemplate}.Template" />.
    /// </summary>
    public interface ITemplateReference
    {
        /// <summary>The referenced template; setting a template of the wrong type stores <c>null</c>.</summary>
        Template Template { get; set; }
    }
}
