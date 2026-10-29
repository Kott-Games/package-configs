using UnityEngine;

namespace Kott.Configs
{
    internal static class ConfigObjectUtility
    {
        /// <summary>Destroys an object created at runtime; immediately outside play mode (edit mode tests).</summary>
        public static void Destroy(Object target)
        {
            if (!target) return;

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
