using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kott.Configs
{
    /// <summary>
    ///     Builds and publishes <see cref="ConfigSet" />s. Push = collect the set's templates (assets and JSON files) →
    ///     resolve IDs and references → validate (all problems at once; errors block publishing) → bake the payload →
    ///     update the type mapping → publish to each target.
    /// </summary>
    public static class ConfigPipeline
    {
        private static readonly IAssetAddressResolver Addresses = new AddressablesAddressResolver();

        /// <summary>
        ///     Collects, resolves, validates and bakes <paramref name="set" /> without publishing. Derived IDs and asset
        ///     addresses of the template assets are updated (marked dirty, not saved).
        /// </summary>
        public static ConfigBuildResult Build(ConfigSet set)
        {
            if (!set) throw new ArgumentNullException(nameof(set));

            List<ConfigIssue> issues = new();
            List<ConfigSet> sets = FindSets();
            List<string> folders = set.GetFolderPaths();
            if (folders.Count == 0)
            {
                issues.Add(new ConfigIssue(ConfigIssueSeverity.Error,
                    $"Config set '{set.name}' has no valid folders.", set));
            }

            CheckOverlaps(set, folders, sets, issues);

            HashSet<string> outputs = new(sets.SelectMany(GetOutputPaths), StringComparer.OrdinalIgnoreCase);
            ConfigBuildResult result = ConfigBuild.Run(CollectTemplates(folders), CollectJsonFiles(folders, outputs),
                Addresses);

            if (result.TemplateCount == 0 && folders.Count > 0)
            {
                issues.Add(new ConfigIssue(ConfigIssueSeverity.Warning,
                    $"Config set '{set.name}' has no templates.", set));
            }

            if (issues.Count == 0) return result;

            List<ConfigIssue> allIssues = issues.Concat(result.Issues).OrderBy(issue => issue.Severity).ToList();
            bool hasErrors = allIssues.Any(issue => issue.Severity == ConfigIssueSeverity.Error);
            return new ConfigBuildResult(allIssues, hasErrors ? null : result.Payload,
                result.TemplateTypes.ToList(), result.TemplateCount, result.ModifiedAssets.ToList());
        }

        /// <summary>
        ///     Builds <paramref name="set" /> and, without errors, updates its type mapping (creating it next to the set
        ///     if needed), saves the updated assets and publishes to every enabled target. Logs all issues.
        /// </summary>
        /// <param name="set">The set to push.</param>
        /// <param name="logger">Receives issues and results. Default: <see cref="Debug.unityLogger" />.</param>
        /// <returns>The build result; check <see cref="ConfigBuildResult.HasErrors" />.</returns>
        public static ConfigBuildResult Push(ConfigSet set, ILogger logger = null)
        {
            logger ??= Debug.unityLogger;
            ConfigBuildResult result = Build(set);
            LogIssues(result.Issues, logger);
            if (result.HasErrors)
            {
                logger.Log(LogType.Error, (object)($"[Configs] Push of '{set.name}' failed: " +
                                                   $"{result.Issues.Count(issue => issue.Severity == ConfigIssueSeverity.Error)} " +
                                                   "errors, nothing published."), set);
                return result;
            }

            if (!set.TypeMapping)
            {
                ConfigTypeMapping mapping = ScriptableObject.CreateInstance<ConfigTypeMapping>();
                AssetDatabase.CreateAsset(mapping, GetDefaultOutputPath(set, "TypeMapping.asset"));
                set.TypeMapping = mapping;
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssetIfDirty(set);
            }

            UpdateTypeMapping(set.TypeMapping, result.TemplateTypes);
            AssetDatabase.SaveAssetIfDirty(set.TypeMapping);
            foreach (Object asset in result.ModifiedAssets)
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            ConfigPublishContext context = new(set, result.Payload);
            int published = 0;
            foreach (ConfigPublishTarget target in set.PublishTargets)
            {
                if (target is not { Enabled: true }) continue;

                try
                {
                    target.Publish(context);
                    published++;
                }
                catch (Exception exception)
                {
                    logger.Log(LogType.Error,
                        (object)$"[Configs] Publishing '{set.name}' to {target.Name} failed: {exception}", set);
                }
            }

            logger.Log(LogType.Log, (object)($"[Configs] Pushed '{set.name}': {result.TemplateCount} templates, " +
                                             $"published to {published} targets."), set);
            return result;
        }

        /// <summary>
        ///     Builds every config set (without publishing) and reports their issues, plus template assets in no set.
        /// </summary>
        public static List<ConfigIssue> ValidateAll()
        {
            List<ConfigIssue> issues = new();
            List<ConfigSet> sets = FindSets();
            foreach (ConfigSet set in sets)
            {
                issues.AddRange(Build(set).Issues);
            }

            List<string> folders = sets.SelectMany(set => set.GetFolderPaths()).ToList();
            foreach (string path in FindTemplatePaths(null))
            {
                if (folders.Any(folder => IsInFolder(path, folder))) continue;
                issues.Add(new ConfigIssue(ConfigIssueSeverity.Warning, $"Template '{path}' is in no config set.",
                    AssetDatabase.LoadMainAssetAtPath(path)));
            }

            return issues.OrderBy(issue => issue.Severity).ToList();
        }

        /// <summary>All config sets in the project, ordered by path.</summary>
        public static List<ConfigSet> FindSets()
        {
            return AssetDatabase.FindAssets($"t:{typeof(ConfigSet).FullName}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<ConfigSet>)
                .Where(set => set)
                .ToList();
        }

        /// <summary>The template assets in the folders of <paramref name="set" />, ordered by path. Sub-assets are skipped.</summary>
        public static List<Template> FindTemplates(ConfigSet set)
        {
            if (!set) throw new ArgumentNullException(nameof(set));

            List<string> folders = set.GetFolderPaths();
            if (folders.Count == 0) return new List<Template>();

            return FindTemplatePaths(folders.ToArray())
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .OfType<Template>()
                .ToList();
        }

        /// <summary>Logs <paramref name="issues" /> with their context.</summary>
        public static void LogIssues(IEnumerable<ConfigIssue> issues, ILogger logger = null)
        {
            foreach (ConfigIssue issue in issues)
            {
                issue.Log(logger);
            }
        }

        /// <summary>Sets the types of <paramref name="mapping" /> to <paramref name="templateTypes" /> and their base types.</summary>
        /// <returns>Whether the mapping changed (then it is marked dirty).</returns>
        public static bool UpdateTypeMapping(ConfigTypeMapping mapping, IEnumerable<Type> templateTypes)
        {
            if (!mapping) throw new ArgumentNullException(nameof(mapping));

            ConfigTypeMapping updated = ConfigTypeMapping.Create(templateTypes.ToArray());
            try
            {
                bool same = mapping.TypeGroups.Count == updated.TypeGroups.Count &&
                            mapping.TypeGroups.Zip(updated.TypeGroups, (a, b) =>
                                a.TypeIdentifier == b.TypeIdentifier && a.TypeString == b.TypeString &&
                                a.BaseTypeStrings.SequenceEqual(b.BaseTypeStrings)).All(equal => equal);
                if (same) return false;

                mapping.TypeGroups.Clear();
                mapping.TypeGroups.AddRange(updated.TypeGroups);
                EditorUtility.SetDirty(mapping);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(updated);
            }
        }

        /// <summary><c>"&lt;set folder&gt;/&lt;set name&gt;&lt;suffix&gt;"</c>, e.g. the default publish path.</summary>
        public static string GetDefaultOutputPath(ConfigSet set, string suffix)
        {
            string directory = set.GetDirectory()
                               ?? throw new InvalidOperationException(
                                   $"Config set '{set.name}' is not saved as an asset.");
            return $"{directory}/{set.name}{suffix}";
        }

        [InitializeOnLoadMethod]
        private static void RegisterEditorLoader()
        {
            EditorLoader.BakeHandler = BakeForEditorLoader;
        }

        private static string BakeForEditorLoader(ScriptableObject configSet)
        {
            if (configSet is not ConfigSet set)
            {
                throw new ConfigException($"'{configSet.name}' is no {nameof(ConfigSet)}.");
            }

            ConfigBuildResult result = Build(set);
            LogIssues(result.Issues);
            if (result.HasErrors)
            {
                ConfigIssue first = result.Issues[0];
                throw new ConfigException(
                    $"Config set '{set.name}' has " +
                    $"{result.Issues.Count(issue => issue.Severity == ConfigIssueSeverity.Error)} errors (see the " +
                    $"console), first: {first.Message}");
            }

            // the runtime reads the payload with this mapping
            if (set.TypeMapping) UpdateTypeMapping(set.TypeMapping, result.TemplateTypes);
            return result.Payload.ToString(Formatting.None);
        }

        private static void CheckOverlaps(ConfigSet set, List<string> folders, List<ConfigSet> sets,
            List<ConfigIssue> issues)
        {
            foreach (ConfigSet other in sets)
            {
                if (other == set) continue;

                foreach (string folder in folders)
                {
                    foreach (string otherFolder in other.GetFolderPaths())
                    {
                        if (!IsInFolder(folder, otherFolder) && !IsInFolder(otherFolder, folder)) continue;

                        issues.Add(new ConfigIssue(ConfigIssueSeverity.Error,
                            $"Config set '{set.name}': folder '{folder}' overlaps '{otherFolder}' of config set " +
                            $"'{other.name}'. A template must belong to exactly one set.", set));
                    }
                }
            }
        }

        private static IEnumerable<string> GetOutputPaths(ConfigSet set)
        {
            foreach (ConfigPublishTarget target in set.PublishTargets)
            {
                if (target == null) continue;

                string[] paths;
                try
                {
                    paths = target.GetOutputPaths(set);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                foreach (string path in paths)
                {
                    yield return path.Replace('\\', '/');
                }
            }
        }

        private static IEnumerable<Template> CollectTemplates(List<string> folders)
        {
            if (folders.Count == 0) return Enumerable.Empty<Template>();

            return FindTemplatePaths(folders.ToArray())
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<Template>();
        }

        private static IEnumerable<string> FindTemplatePaths(string[] folders)
        {
            string filter = $"t:{typeof(Template).FullName}";
            string[] guids = folders == null
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, folders);
            return guids.Distinct().Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal);
        }

        private static IEnumerable<JsonTemplateFile> CollectJsonFiles(List<string> folders, HashSet<string> outputs)
        {
            if (folders.Count == 0) yield break;

            IEnumerable<string> paths = AssetDatabase.FindAssets("t:TextAsset", folders.ToArray())
                .Distinct()
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && !outputs.Contains(path))
                .OrderBy(path => path, StringComparer.Ordinal);

            foreach (string path in paths)
            {
                yield return new JsonTemplateFile(path, File.ReadAllText(path),
                    AssetDatabase.LoadMainAssetAtPath(path));
            }
        }

        private static bool IsInFolder(string path, string folder)
        {
            return path.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
