using System;
using System.Collections.Generic;

namespace Kott.Configs
{
    /// <summary>Typed read access to loaded config templates.</summary>
    public interface IConfigProvider
    {
        /// <summary>The template with <paramref name="id" />.</summary>
        /// <exception cref="KeyNotFoundException">No template has this ID.</exception>
        /// <exception cref="InvalidCastException">The template is not a <typeparamref name="T" />.</exception>
        T GetTemplate<T>(string id) where T : Template;

        /// <summary>The template with <paramref name="id" />, if there is one of type <typeparamref name="T" />.</summary>
        bool TryGetTemplate<T>(string id, out T template) where T : Template;

        /// <summary>
        ///     The template mapped to <paramref name="value" /> through the <see cref="Attributes.TemplateEnumAttribute" />
        ///     of <typeparamref name="T" />.
        /// </summary>
        /// <exception cref="ArgumentException"><typeparamref name="T" /> has no enum mapping for the value's enum type.</exception>
        /// <exception cref="KeyNotFoundException">No template is mapped to the value.</exception>
        T GetTemplate<T>(Enum value) where T : Template;

        /// <summary>
        ///     All templates of type <typeparamref name="T" />, including subtypes (in load order). Empty if the type is
        ///     known but has no templates.
        /// </summary>
        /// <exception cref="KeyNotFoundException"><typeparamref name="T" /> is not a known template type.</exception>
        IReadOnlyList<T> GetCollection<T>() where T : Template;
    }
}
