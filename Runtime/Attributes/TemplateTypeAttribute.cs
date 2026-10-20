using System;

namespace Kott.Configs.Attributes
{
    /// <summary>
    ///     Pins the type identifier written to config payloads for the attributed template class. Without it the
    ///     identifier is derived from the class name (<c>ItemTemplate</c> → <c>Item</c>), so renaming the class breaks
    ///     published payloads.
    /// </summary>
    /// <example>
    ///     <code>
    ///     [TemplateType("Item")]          // stays "Item" after renaming the class
    ///     public class ItemDefinition : Template { … }
    ///     </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TemplateTypeAttribute : Attribute
    {
        /// <param name="identifier">The type identifier used in payloads.</param>
        public TemplateTypeAttribute(string identifier)
        {
            Identifier = identifier;
        }

        /// <summary>The type identifier used in payloads.</summary>
        public string Identifier { get; }
    }
}
