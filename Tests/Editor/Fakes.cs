using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kott.Utilities.Progress;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Kott.Configs.Tests
{
    /// <summary>Loader returning a fixed payload, a failure, or a task the test completes.</summary>
    internal sealed class FakeLoader : ConfigLoader
    {
        private readonly Func<CancellationToken, Task<string>> _load;

        public FakeLoader(string json, ConfigLoaderPolicy policy = ConfigLoaderPolicy.Required)
            : this(_ => Task.FromResult(json), policy)
        { }

        public FakeLoader(Func<CancellationToken, Task<string>> load, ConfigLoaderPolicy policy = ConfigLoaderPolicy.Required)
        {
            _load = load;
            Policy = policy;
        }

        public CancellationToken Token { get; private set; }

        public static FakeLoader Failing(Exception exception, ConfigLoaderPolicy policy = ConfigLoaderPolicy.Required)
        {
            return new FakeLoader(_ => Task.FromException<string>(exception), policy);
        }

        public override Task<string> LoadAsync(IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return _load(cancellationToken);
        }
    }

    /// <summary>Builds payload JSON for tests.</summary>
    internal static class TestPayloads
    {
        public static ConfigTypeMapping Mapping()
        {
            return ConfigTypeMapping.Create(typeof(TestItemTemplate), typeof(TestWeaponTemplate),
                typeof(TestStatTemplate), typeof(TestPinnedTemplate), typeof(TestUnusedTemplate));
        }

        public static string Payload(params JObject[] templates)
        {
            return new JObject
            {
                { ConfigSerialization.FormatVersionProperty, ConfigSerialization.FormatVersion },
                { ConfigSerialization.TemplatesProperty, new JArray(templates.Cast<object>().ToArray()) },
            }.ToString();
        }

        public static JObject Item(string id, int damage = 0, string upgrade = null, string type = "TestItem")
        {
            JObject item = new() { { "Type", type }, { "ID", id }, { "Damage", damage } };
            if (upgrade != null) item.Add("Upgrade", upgrade);
            return item;
        }

        public static JObject Stat(string id, float value)
        {
            return new JObject { { "Type", "TestStat" }, { "ID", id }, { "Value", value } };
        }
    }

    /// <summary>
    ///     Records logs so tests can check expected warnings and errors without printing them to the console.
    /// </summary>
    internal sealed class RecordingLogHandler : ILogHandler
    {
        private readonly List<(LogType Type, string Message)> _entries = new();
        private readonly List<(LogType Type, string Pattern)> _expectations = new();

        public RecordingLogHandler()
        {
            Logger = new Logger(this);
        }

        public ILogger Logger { get; }

        public IReadOnlyList<(LogType Type, string Message)> Entries => _entries;

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            _entries.Add((logType, string.Format(format, args)));
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            _entries.Add((LogType.Exception, exception.ToString()));
        }

        /// <summary>Expects a log of <paramref name="type" /> matching <paramref name="pattern" /> by the end of the test.</summary>
        public void Expect(LogType type, string pattern)
        {
            _expectations.Add((type, pattern));
        }

        /// <summary>Fails on missing expected logs, and on unexpected warnings or errors.</summary>
        public void Verify()
        {
            List<(LogType Type, string Message)> remaining = new(_entries);
            foreach ((LogType type, string pattern) in _expectations)
            {
                int index = remaining.FindIndex(entry => entry.Type == type && Regex.IsMatch(entry.Message, pattern));
                Assert.That(index, Is.GreaterThanOrEqualTo(0),
                    $"Expected {type} log matching '{pattern}'. Logged: {Describe(_entries)}");
                remaining.RemoveAt(index);
            }

            remaining.RemoveAll(entry => entry.Type == LogType.Log);
            Assert.That(remaining, Is.Empty, $"Unexpected logs: {Describe(remaining)}");
        }

        private static string Describe(List<(LogType Type, string Message)> entries)
        {
            return entries.Count == 0
                ? "none"
                : string.Join(" | ", entries.ConvertAll(entry => $"[{entry.Type}] {entry.Message}"));
        }
    }
}
