using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs
{
    /// <summary>
    ///     Issue list of the config window: the result of validating one set or all sets, with severity filter,
    ///     click-to-select (double click opens JSON files at the line) and Fix buttons.
    /// </summary>
    internal sealed class ConfigHealthView
    {
        private List<ConfigIssue> _issues;
        private ConfigSet _set;
        private bool _all;
        private string _title;

        private bool _showErrors = true;
        private bool _showWarnings = true;
        private Vector2 _scroll;
        private ConfigIssue _pendingFix;

        private GUIContent _errorIcon;
        private GUIContent _warningIcon;

        /// <summary>Builds <paramref name="set" /> without publishing and shows its issues.</summary>
        public void Validate(ConfigSet set)
        {
            _set = set;
            _all = false;
            Refresh();
        }

        /// <summary>Validates every set and shows the issues, including templates in no set.</summary>
        public void ValidateAll()
        {
            _set = null;
            _all = true;
            Refresh();
        }

        /// <summary>Shows issues of an operation that already built <paramref name="set" />, e.g. a push.</summary>
        public void Show(ConfigSet set, IEnumerable<ConfigIssue> issues, string action)
        {
            _set = set;
            _all = false;
            SetIssues(issues, $"{action} '{set.name}'");
        }

        public void OnGUI()
        {
            _errorIcon ??= EditorGUIUtility.IconContent("console.erroricon.sml");
            _warningIcon ??= EditorGUIUtility.IconContent("console.warnicon.sml");

            if (_issues == null)
            {
                EditorGUILayout.HelpBox("Validate a config set to list its problems here.", MessageType.Info);
                return;
            }

            int errors = _issues.Count(issue => issue.Severity == ConfigIssueSeverity.Error);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_title, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                _showErrors = GUILayout.Toggle(_showErrors, Label(_errorIcon, errors.ToString()),
                    EditorStyles.toolbarButton);
                _showWarnings = GUILayout.Toggle(_showWarnings,
                    Label(_warningIcon, (_issues.Count - errors).ToString()),
                    EditorStyles.toolbarButton);
            }

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems found.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (ConfigIssue issue in _issues)
            {
                bool isError = issue.Severity == ConfigIssueSeverity.Error;
                if (isError ? !_showErrors : !_showWarnings) continue;

                DrawIssue(issue, isError ? _errorIcon : _warningIcon);
            }

            EditorGUILayout.EndScrollView();

            if (_pendingFix != null)
            {
                ConfigIssue issue = _pendingFix;
                _pendingFix = null;
                issue.Fix();
                Refresh();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawIssue(ConfigIssue issue, GUIContent icon)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(icon, GUILayout.Width(20f));
                string text = issue.Line > 0 ? $"{issue.Message} (line {issue.Line})" : issue.Message;
                GUILayout.Label(text, EditorStyles.wordWrappedLabel);
                if (issue.Fix != null && GUILayout.Button("Fix", GUILayout.Width(40f))) _pendingFix = issue;
            }

            Rect rect = GUILayoutUtility.GetLastRect();
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition))
            {
                return;
            }

            if (!issue.Context) return;

            if (current.clickCount == 2)
            {
                AssetDatabase.OpenAsset(issue.Context, issue.Line > 0 ? issue.Line : -1);
            }
            else
            {
                Selection.activeObject = issue.Context;
                EditorGUIUtility.PingObject(issue.Context);
            }

            current.Use();
        }

        private void Refresh()
        {
            if (_all)
            {
                SetIssues(ConfigPipeline.ValidateAll(), "Validated all config sets");
            }
            else if (_set)
            {
                SetIssues(ConfigPipeline.Build(_set).Issues, $"Validated '{_set.name}'");
            }
            else
            {
                _issues = null;
            }
        }

        private void SetIssues(IEnumerable<ConfigIssue> issues, string title)
        {
            _issues = issues.ToList();
            _title = $"{title} at {DateTime.Now:HH:mm:ss}";
        }

        private static GUIContent Label(GUIContent icon, string text)
        {
            return new GUIContent(text, icon.image);
        }
    }
}
