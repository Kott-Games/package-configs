using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Draws a <c>[SerializeReference]</c> field (or each element of a list) with a dropdown to pick the instance's
    ///     type among the serializable subtypes of the field type.
    /// </summary>
    internal sealed class SubtypePickerAttribute : PropertyAttribute
    { }
}
