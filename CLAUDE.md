# Packages/Configs

Git submodule (`Kott-Games/package-configs`). Core game-config framework, paired
with Unity's Remote Config Service. This is our own code, freely editable, but
changes need a commit in this submodule's own repo, then a pointer bump in the main
project (see root `CLAUDE.md` → Submodules).

## The idea

`Template` (see `Template.cs`, `TemplateDescription.cs`) is the base for a single
config item — authored in the editor as a ScriptableObject (see the concrete
`*Template` classes across `Assets/Scripts/Runtime/**` and data under
`Assets/Configs/`). Config items are **authoring-only / editor-only**: they are tied
to the currently checked-out project version and are not meant to ship inside a
build as raw assets.

Editor processing supports two kinds of linking directly via normal Unity object
references:
- **Config-to-config linking** — one template referencing another
  (`TemplateReference.cs`, `ITemplateReference.cs`, `TemplateReferenceConverter.cs`).
- **Addressable-asset linking** — a template referencing an Addressable asset
  (`TemplateAssetReference.cs`, `ITemplateAssetReference.cs`,
  `TemplateAssetReferenceConverter.cs`).

On demand, the full config set is serialized to JSON
(`ConfigSerializationUtility.cs`, using converters — including Unity-type JSON
converters from `Packages/Utilities/Runtime/JsonConverters`) and uploaded to the
Remote Config server, scoped to the checked-out project **version**
(`ConfigVersionUtility.cs`). Each build starts its own config branch, so configs can
keep being updated between builds without needing a new build to pick up tuning
changes — the running game just fetches the latest config version for its build.

## Runtime lookup

`IConfigManager` (`GetCollection<T>()`, `GetTemplate<T>(id)`) is the surface actual
game code should query — there are three interchangeable, standalone `Manager`
(Scopes) implementations behind it, verified against source:

- `LocalConfigManager.cs` — always reads from a serialized `RuntimeConfigData`
  asset, no network. Used for local/offline config testing.
- `RemoteConfigManager.cs` — always fetches from `RemoteConfigService` (Unity
  Remote Config), filtered by the current build's version key
  (`ConfigVersionUtility.GetTemplateDataKey()`), then extracts templates the same way.
- `ConfigManager.cs` — a single manager with both paths built in, switched by a
  `_useLocalConfigData` inspector checkbox (`Development` header): local when
  checked, remote fetch otherwise. This is the one actually wired into most scopes.
- All three group deserialized templates by type via `ConfigTypeMapping` (which
  also expands "base type" groupings, e.g. so `GetCollection<ItemTemplate>()` can
  return all item subtype instances), then expose them via `GetCollection<T>()` /
  `GetTemplate<T>(id)`.
- `RuntimeConfigData.cs`, `ConfigStructure.cs`, `ConfigTypeMapping.cs`,
  `CreateDataWrapper.cs` — serialization-side plumbing for the above.
- `Editor/Deploy/DeployUtility.cs` — editor tooling that opens Unity's Deployment
  Services window and pushes all deployment items (config templates included) to
  the Remote Config server for the current environment/version.
- `Attributes/TemplateGroupAttribute.cs` — class-level attribute grouping template
  types under a `groupID` for editor authoring UI.
- `Attributes/TemplateEnumAttribute.cs` — class-level attribute associating a
  template type with a C# enum type (the enum-to-template-variant pattern used by
  things like `StatType`/`TagType` in the Actor system).

## Working here

- If a task is "add a new kind of config item," add a new `Template` subclass next
  to the existing ones for that system (e.g. actor-related templates live near
  `Assets/Scripts/Runtime/Actors`), not inside this package — this package is the
  generic authoring/serialization/remote-config machinery, not game content.
- Remember: code that reads config values at runtime must go through
  `ConfigManager`, not assume the ScriptableObject asset is present in a build.
