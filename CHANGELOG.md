# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- `Kott.Configs.Testing` now compiles in the editor when the package is installed from a registry or git URL
  without being listed in `testables`. The imported sample tests failed with a missing `Kott.Configs.Testing`
  namespace.

## [1.0.0] - 2026-10-29

First public release. Earlier, unversioned states of the package are not compatible.

### Added
- **Config sets** (`ConfigSet` asset): the templates of their folders (template assets and `*.json` template
  files), a `ConfigTypeMapping`, and publish targets. Several independent sets per project (game, samples, test
  fixtures); overlapping set folders are an error.
- **JSON template files**: flat arrays of templates with explicit `Type` and `ID`, referencing templates by ID and
  assets by address, read strictly (unknown fields and types are errors with line numbers). They may reference asset
  templates; identical data in an asset and a JSON entry bakes to identical payload entries.
- **Pipeline** (`ConfigPipeline.Build/Push/ValidateAll`, also from code or CI): collect → resolve IDs and
  references → validate → bake a deterministic payload (`{ "formatVersion": 1, "templates": [ … ] }`, ordered by
  ID) → publish. Validation reports every issue at once (`ConfigIssue`, with context object, JSON line and optional
  fix): duplicate or missing IDs, dangling and cross-set references, reference type mismatches, unknown fields and
  types, ambiguous or colliding type identifiers, unaddressable assets, sub-asset templates, enum gaps, templates
  in no set.
- Publish targets (`ConfigPublishTarget`): `FilePublishTarget` (a `TextAsset` for the build, `StreamingAssets`, any
  path) and `RemoteConfigPublishTarget` (writes the versioned entry into a `.rc` file, keeps other entries,
  optionally deploys it). Custom targets by subclassing.
- **`ConfigProvider`**: loads payloads from an ordered list of loaders, merges them per template ID (later loaders
  win), resolves references after the merge and offers typed lookups (`IConfigProvider`): `GetTemplate<T>(id)`,
  `TryGetTemplate`, `GetCollection<T>()` including subtypes, `GetTemplate<T>(enumValue)`. Owns the created
  templates; a failed reload keeps the previous data.
- Loaders (`ConfigLoader`, each `Required` or `Optional`, with timeout and progress): `TextAssetLoader`,
  `FileLoader` (StreamingAssets, persistent data, absolute path or URL; optional skip if missing),
  `RemoteConfigLoader`, and `EditorLoader` (bakes a set in memory on load, so play mode needs no push).
- `[TemplateType("…")]` pins a template's type identifier, so renaming the class doesn't break published data.
- `[TemplateEnum(typeof(TEnum))]` maps enum values to templates (`"<group>+<value name>"` or `IdFormat`).
- `TemplateAssetReference<T>.Address`.
- Config window (*Tools > Configs > Open Window*): set selector, Validate / Validate All / Push, and a health view
  with severity filters, click to select, double click to open (JSON files at the line) and fix buttons. The
  template browser works per set.
- `Kott.Configs.Testing`: `InMemoryConfigProvider` (templates built in code) and, with Scopes,
  `InMemoryConfigManager`.
- "Configs Basics" sample: a bestiary from one config set with asset and JSON templates, subtypes, template and
  sprite references, an enum mapping, and a baseline plus an override file to edit while playing; with tests.

### Changed
- **Breaking:** Scopes, Remote Config and Deployment are optional. Their integrations are separate assemblies
  (`Kott.Configs.Scopes`, `Kott.Configs.RemoteConfig`, `Kott.Configs.RemoteConfig.Editor`), enabled when the
  package is installed.
- **Breaking:** `ConfigManager` (Scopes) is one manager configured with a type mapping and a loader list, injected
  as `IConfigManager` (now a Scopes manager contract). It replaces `LocalConfigManager` and `RemoteConfigManager`
  and logs through `Manager.Logger` instead of `Debug`; the payload is no longer logged.
- **Breaking:** the payload is an object with `formatVersion` and `templates`. Bare arrays are still read; newer
  format versions are rejected with a clear error.
- **Breaking:** default values are written and read (`DefaultValueHandling.Include`): an explicit `0`, `false` or
  `null` in data no longer falls back to the field initializer.
- **Breaking:** editor windows renamed and made internal (`MainConfigWindow` → `ConfigWindow`, `TableWindow` →
  `TemplateBrowserWindow`); asset menu *Configs > Config Set*.
- `TemplateReference` drawer is a plain object field; it no longer creates sub-asset templates.
- Asset references are made addressable by a validation fix instead of silently on export.

### Removed
- **Breaking:** the implicit global template set and the editor caches `ConfigStructure` and `RuntimeConfigData`
  (with "use local config data"; use `EditorLoader`), `ConfigStructureUtility`, `TemplateReferenceUtility`,
  `RCJsonAPI` and copy-to-clipboard export (use publish targets).
- **Breaking:** `ConfigSerializationUtility` (use `ConfigSerialization`), `TemplateDescription`,
  `CreateDataWrapper<T>`, `TemplateExtensions.FormatID`.
- Sub-asset templates (now reported as errors).

### Fixed
- The Remote Config loader read the unversioned key and always got zero templates; loader and publish target now
  share `ConfigVersionUtility.GetTemplateDataKey()`.
- A failed fetch, a non-array payload, unknown types and duplicate IDs raise descriptive `ConfigException`s instead
  of null references or silent empty results. Dangling references are reported with both IDs.
- Remote Config fetches use `FetchConfigsAsync`; the never-unsubscribed `FetchCompleted` handler is gone.
- Converting a `null` `TemplateReference<T>` to its template returns `null` instead of throwing.
