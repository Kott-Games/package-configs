using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Kott.Configs.Basics
{
    /// <summary>
    ///     Loads the sample's configs without Scopes and shows them as a bestiary. Two loaders: the baseline payload the
    ///     config set publishes as a <see cref="TextAsset" />, and an optional override file in the persistent data path
    ///     that can be edited while playing and reloaded: tuning without a new build.
    /// </summary>
    public class BestiaryDemo : MonoBehaviour
    {
        private const string OverriddenId = "Enemy+Goblin";

        [SerializeField]
        [Tooltip("Written by the config set's push; maps the payload's type identifiers to template classes.")]
        private ConfigTypeMapping _typeMapping;

        [SerializeField]
        [Tooltip("The payload published by the config set's File publish target.")]
        private TextAsset _baseline;

        [SerializeField]
        [Tooltip("Override file, relative to Application.persistentDataPath.")]
        private string _overrideFile = "ConfigsBasics/overrides.json";

        private readonly List<AsyncOperationHandle> _iconHandles = new();

        private ConfigProvider _configs;
        private Text _status;
        private RectTransform _entries;
        private string _warning;

        private string OverridePath => Path.Combine(Application.persistentDataPath, _overrideFile);

        private void Start()
        {
            _configs = new ConfigProvider(_typeMapping, new ConfigLoader[]
            {
                new TextAssetLoader(_baseline),
                // optional: a missing file loads nothing, a broken one is skipped with a warning
                new FileLoader(FileLoader.PathRoot.PersistentData, _overrideFile, skipIfMissing: true)
                {
                    Policy = ConfigLoaderPolicy.Optional,
                },
            }, new Logger(new WarningRecorder(warning => _warning = warning)));

            BuildUI();
            Reload();
        }

        private void OnDestroy()
        {
            ReleaseIcons();
            _configs?.Dispose();
        }

        private async void Reload()
        {
            _warning = null;
            try
            {
                await _configs.LoadAsync();
            }
            catch (Exception exception)
            {
                // a failed load keeps the previous data
                Debug.LogException(exception, this);
                if (this) ShowStatus($"<color=#ff6b6b>Load failed: {exception.Message}</color>");
                return;
            }

            if (this) ShowBestiary();
        }

        private void ShowBestiary()
        {
            ReleaseIcons();
            BasicsUI.Clear(_entries);

            List<BestiaryEntry> entries;
            try
            {
                entries = Bestiary.Build(_configs);
            }
            catch (KeyNotFoundException exception)
            {
                ShowStatus($"<color=#ff6b6b>{exception.Message}</color>");
                return;
            }

            foreach (BestiaryEntry entry in entries)
            {
                RectTransform card = BasicsUI.Row(_entries, false, BasicsUI.PanelColor, entry.Enemy.ID);
                // tinted placeholder until the sprite is loaded
                Image icon = BasicsUI.Icon(card, 96f, entry.Element.Color);
                LoadIcon(entry.Enemy, icon);
                BasicsUI.Label(card, Describe(entry), 24);
            }

            ShowStatus($"{_configs.Catalog.Templates.Count} templates loaded.");
        }

        private static string Describe(BestiaryEntry entry)
        {
            EnemyTemplate enemy = entry.Enemy;
            string title = $"<b>{enemy.DisplayName}</b>";
            if (entry.Boss) title += $"   <color=#ffcc66>BOSS · {entry.Boss.Phases} phases</color>";
            title += $"   <color=#7f8796>{enemy.ID}</color>";

            string stats = $"{Colored(entry.Element)}, weak to {Colored(entry.Weakness)} · " +
                           $"HP {enemy.Health} · DMG {enemy.Damage} · threat {entry.Threat}";

            List<ItemTemplate> loot = entry.Loot.ToList();
            string drops = loot.Count == 0
                ? "Drops nothing"
                : $"Drops {string.Join(", ", loot.Select(item => item.DisplayName))} ({entry.LootValue} gold)";
            EnemyTemplate minion = entry.Boss ? entry.Boss.Minion.Template : null;
            if (minion) drops += $" · summons {minion.DisplayName}";

            return $"{title}\n{stats}\n{drops}";
        }

        private static string Colored(ElementTemplate element)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(element.Color)}>{element.DisplayName}</color>";
        }

        private void LoadIcon(EnemyTemplate enemy, Image image)
        {
            string address = enemy.Icon.Address;
            if (string.IsNullOrEmpty(address)) return;

            // until the sample is set up (see README), its sprites aren't addressable in your project: check
            // instead of logging an error per icon
            AsyncOperationHandle<IList<IResourceLocation>> locations =
                Addressables.LoadResourceLocationsAsync(address, typeof(Sprite));
            _iconHandles.Add(locations);
            locations.Completed += handle =>
            {
                if (!image || handle.Status != AsyncOperationStatus.Succeeded || handle.Result.Count == 0) return;

                _iconHandles.Add(enemy.Icon.LoadAssetAsync(sprite =>
                {
                    if (!image || !sprite) return;
                    image.sprite = sprite;
                    image.color = Color.white;
                }));
            };
        }

        private void ReleaseIcons()
        {
            foreach (AsyncOperationHandle handle in _iconHandles)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            _iconHandles.Clear();
        }

        private void WriteOverride()
        {
            if (!_configs.IsLoaded || !_configs.TryGetTemplate(OverriddenId, out EnemyTemplate goblin))
            {
                ShowStatus($"<color=#ff6b6b>No template '{OverriddenId}' loaded.</color>");
                return;
            }

            // the same format a push publishes: a payload with one template, which replaces the baseline's by ID
            JObject payload = ConfigSerialization.Serialize(_typeMapping, new[] { goblin });
            JObject template = (JObject)payload[ConfigSerialization.TemplatesProperty][0];
            template[nameof(EnemyTemplate.DisplayName)] = "Goblin Warlord";
            template[nameof(EnemyTemplate.Health)] = goblin.Health * 4;
            template[nameof(EnemyTemplate.Damage)] = goblin.Damage + 3;

            Directory.CreateDirectory(Path.GetDirectoryName(OverridePath));
            File.WriteAllText(OverridePath, payload.ToString());
            Reload();
        }

        private void DeleteOverride()
        {
            if (File.Exists(OverridePath)) File.Delete(OverridePath);
            Reload();
        }

        private void OpenOverrideFolder()
        {
            string folder = Path.GetDirectoryName(OverridePath);
            Directory.CreateDirectory(folder);
            Application.OpenURL(new Uri(folder).AbsoluteUri);
        }

        private void ShowStatus(string message)
        {
            string file = File.Exists(OverridePath) ? "present" : "none";
            string status = $"{message}   Override file: {file}";
            if (_warning != null) status += $"\n<color=#ffb347>{_warning}</color>";
            _status.text = status + $"\n<size=20><color=#7f8796>{OverridePath}</color></size>";
        }

        private void BuildUI()
        {
            BasicsUI.CreateEventSystem(transform);
            RectTransform canvas = BasicsUI.CreateCanvas("Canvas", transform);
            RectTransform column = BasicsUI.Column(canvas, 1500f);

            BasicsUI.Label(column, "<b>Bestiary</b>", 44, TextAnchor.MiddleCenter);
            _status = BasicsUI.Label(column, "Loading…", 24);

            RectTransform buttons = BasicsUI.Row(column);
            BasicsUI.Button(buttons, "Reload", Reload);
            BasicsUI.Button(buttons, "Write example override", WriteOverride);
            BasicsUI.Button(buttons, "Open override folder", OpenOverrideFolder);
            BasicsUI.Button(buttons, "Delete override", DeleteOverride);

            _entries = BasicsUI.Column(column, 0f, name: "Entries");
            VerticalLayoutGroup layout = _entries.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 8, 0);
        }

        // forwards everything to the console and remembers the last warning (e.g. a broken override file)
        private sealed class WarningRecorder : ILogHandler
        {
            private readonly Action<string> _onWarning;

            public WarningRecorder(Action<string> onWarning)
            {
                _onWarning = onWarning;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (logType == LogType.Warning) _onWarning(string.Format(format, args));
                Debug.unityLogger.logHandler.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, Object context)
            {
                Debug.unityLogger.logHandler.LogException(exception, context);
            }
        }
    }
}
