using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Loads config payloads from an ordered list of <see cref="ConfigLoader" />s and gives typed access to the
    ///     templates. Later loaders override earlier ones per template ID (e.g. a built-in baseline plus remote tuning);
    ///     template references are resolved after merging. Owns the templates it creates: <see cref="Dispose" /> destroys
    ///     them.
    /// </summary>
    /// <example>
    ///     <code>
    ///     ConfigProvider configs = new(typeMapping, new ConfigLoader[]
    ///     {
    ///         new TextAssetLoader(baseline),
    ///         new FileLoader(FileLoader.PathRoot.PersistentData, "overrides.json", skipIfMissing: true),
    ///     });
    ///     await configs.LoadAsync();
    ///     ItemTemplate sword = configs.GetTemplate&lt;ItemTemplate&gt;("Item+Sword");
    ///     </code>
    /// </example>
    public sealed class ConfigProvider : IConfigProvider, IDisposable
    {
        private readonly ConfigTypeMapping _typeMapping;
        private readonly List<ConfigLoader> _loaders;
        private readonly ILogger _logger;

        private TemplateCatalog _catalog;
        private bool _loading;

        /// <param name="typeMapping">Mapping of the payloads' type identifiers.</param>
        /// <param name="loaders">The loaders, lowest priority first.</param>
        /// <param name="logger">Receives warnings (optional loader failures, dangling references). Default: <see cref="Debug.unityLogger" />.</param>
        public ConfigProvider(ConfigTypeMapping typeMapping, IEnumerable<ConfigLoader> loaders, ILogger logger = null)
        {
            _typeMapping = typeMapping ? typeMapping : throw new ArgumentNullException(nameof(typeMapping));
            _loaders = loaders?.ToList() ?? throw new ArgumentNullException(nameof(loaders));
            if (_loaders.Contains(null)) throw new ArgumentException("Loaders contain null.", nameof(loaders));
            _logger = logger ?? Debug.unityLogger;
        }

        /// <summary>Logs what each load does (loaders, template counts, overrides). Payloads are never logged.</summary>
        public bool Verbose { get; set; }

        /// <summary>Whether a load has completed. The previous data stays available while reloading.</summary>
        public bool IsLoaded => _catalog != null;

        /// <summary>The loaded templates.</summary>
        /// <exception cref="InvalidOperationException">Nothing loaded yet.</exception>
        public TemplateCatalog Catalog =>
            _catalog ?? throw new InvalidOperationException("Configs are not loaded yet. Await LoadAsync first.");

        /// <summary>
        ///     Runs all loaders in order, merges their payloads and replaces the current templates (if any) on success.
        /// </summary>
        /// <exception cref="ConfigException">A required loader failed, or the type mapping is invalid.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
        public async Task LoadAsync(IProgress<ProgressReport> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (_loading) throw new InvalidOperationException("A load is already running.");
            _loading = true;

            List<ConfigPayload> payloads = new();
            try
            {
                Dictionary<string, Type> types = _typeMapping.ResolveTypes();

                SubProgress subProgress = new(progress);
                for (int i = 0; i < _loaders.Count; i++)
                {
                    ConfigLoader loader = _loaders[i];
                    subProgress.Start = new ProgressReport((float)i / _loaders.Count, loader.Name);
                    subProgress.Scale = 1f / _loaders.Count;

                    ConfigPayload payload = await LoadPayloadAsync(loader, subProgress, cancellationToken);
                    if (payload != null) payloads.Add(payload);
                }

                List<Template> templates = Merge(payloads);
                ResolveReferences(payloads, templates.ToDictionary(template => template.ID));

                TemplateCatalog previous = _catalog;
                _catalog = new TemplateCatalog(templates, types.Values);
                payloads.Clear();
                if (previous != null) DestroyTemplates(previous.Templates.Values);

                if (Verbose) _logger.Log(LogType.Log, $"Configs loaded: {templates.Count} templates.");
                progress?.Report(new ProgressReport(1f, null));
            }
            finally
            {
                foreach (ConfigPayload payload in payloads)
                {
                    DestroyTemplates(payload.Templates);
                }

                _loading = false;
            }
        }

        /// <summary>Destroys the loaded templates. The provider can be loaded again afterwards.</summary>
        public void Dispose()
        {
            if (_catalog == null) return;
            DestroyTemplates(_catalog.Templates.Values);
            _catalog = null;
        }

        /// <inheritdoc />
        public T GetTemplate<T>(string id) where T : Template => Catalog.GetTemplate<T>(id);

        /// <inheritdoc />
        public bool TryGetTemplate<T>(string id, out T template) where T : Template =>
            Catalog.TryGetTemplate(id, out template);

        /// <inheritdoc />
        public T GetTemplate<T>(Enum value) where T : Template => Catalog.GetTemplate<T>(value);

        /// <inheritdoc />
        public IReadOnlyList<T> GetCollection<T>() where T : Template => Catalog.GetCollection<T>();

        private async Task<ConfigPayload> LoadPayloadAsync(ConfigLoader loader, IProgress<ProgressReport> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                string json = await RunLoaderAsync(loader, progress, cancellationToken);
                if (json == null)
                {
                    if (Verbose) _logger.Log(LogType.Log, $"Config loader {loader.Name}: nothing to load.");
                    return null;
                }

                ConfigPayload payload = ConfigSerialization.Deserialize(_typeMapping, json);
                if (Verbose)
                {
                    _logger.Log(LogType.Log, $"Config loader {loader.Name}: {payload.Templates.Count} templates.");
                }

                return payload;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (loader.Policy == ConfigLoaderPolicy.Optional)
            {
                _logger.Log(LogType.Warning,
                    $"Optional config loader {loader.Name} failed, continuing without it: {exception.Message}");
                return null;
            }
            catch (Exception exception)
            {
                throw new ConfigException($"Config loader {loader.Name} failed: {exception.Message}", exception);
            }
        }

        private static async Task<string> RunLoaderAsync(ConfigLoader loader, IProgress<ProgressReport> progress,
            CancellationToken cancellationToken)
        {
            using CancellationTokenSource loaderCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task<string> load = loader.LoadAsync(progress, loaderCancellation.Token)
                                ?? throw new InvalidOperationException("LoadAsync returned no task.");

            if (loader.Timeout > 0f && !load.IsCompleted)
            {
                loaderCancellation.CancelAfter(TimeSpan.FromSeconds(loader.Timeout));
                Task timeout = Task.Delay(Timeout.Infinite, loaderCancellation.Token);
                if (await Task.WhenAny(load, timeout) != load)
                {
                    // observe a late failure of the abandoned load
                    _ = load.ContinueWith(task => task.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException($"Timed out after {loader.Timeout} s.");
                }
            }

            return await load;
        }

        private List<Template> Merge(List<ConfigPayload> payloads)
        {
            List<Template> templates = new();
            Dictionary<string, int> indices = new();
            foreach (ConfigPayload payload in payloads)
            {
                foreach (Template template in payload.Templates)
                {
                    if (indices.TryGetValue(template.ID, out int index))
                    {
                        if (Verbose) _logger.Log(LogType.Log, $"Config template '{template.ID}' overridden.");
                        ConfigObjectUtility.Destroy(templates[index]);
                        templates[index] = template;
                    }
                    else
                    {
                        indices.Add(template.ID, templates.Count);
                        templates.Add(template);
                    }
                }
            }

            return templates;
        }

        private void ResolveReferences(List<ConfigPayload> payloads, Dictionary<string, Template> templates)
        {
            foreach (ConfigPayload payload in payloads)
            {
                foreach (PendingReference pending in payload.References)
                {
                    // the owner was overridden by a later payload
                    if (templates[pending.Owner.ID] != pending.Owner) continue;

                    if (!templates.TryGetValue(pending.TargetId, out Template target))
                    {
                        _logger.Log(LogType.Warning,
                            $"Config template '{pending.Owner.ID}' references '{pending.TargetId}', which doesn't " +
                            "exist. The reference stays empty.");
                        continue;
                    }

                    pending.Reference.Template = target;
                    if (pending.Reference.Template != target)
                    {
                        _logger.Log(LogType.Warning,
                            $"Config template '{pending.Owner.ID}' references '{pending.TargetId}', whose type " +
                            $"{target.GetType().Name} doesn't match the reference. The reference stays empty.");
                    }
                }
            }
        }

        private static void DestroyTemplates(IEnumerable<Template> templates)
        {
            foreach (Template template in templates.ToList())
            {
                ConfigObjectUtility.Destroy(template);
            }
        }
    }
}
