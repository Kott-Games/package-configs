# Configs Basics

A bestiary built from one config set: enemies, bosses, items and elements as template assets plus a JSON file,
baked into one payload and loaded at runtime from two layers. Change the data while playing and reload,
without a new build.

Works without Scopes: the scene loads the configs with a plain `ConfigProvider`. UI is uGUI built in code, so
the scene stays tiny and the code stays the point.

## Requirements

- uGUI (`com.unity.ugui`).
- Active Input Handling set to *Input Manager (Old)* or *Both* (Project Settings > Player), for the buttons.

## Run it

Open `Scenes/ConfigsBasics.unity` and press Play. The enemies show their element color instead of an icon
until the setup below is done.

## Setup for icons (and your own changes)

Icons are `TemplateAssetReference<Sprite>`s, which are Addressables addresses in the payload. A sample can't
bring Addressables entries along, so:

1. **Tools > Configs > Open Window**, pick `ConfigsBasics`, press **Validate**. Three errors "… is not
   addressable": press **Fix** on each (adds the sprite to the default Addressables group).
2. Press **Push**. The set is baked again and published to `Configs/ConfigsBasics.json`, now with the icons'
   addresses. Play again.

Push after every change to the template assets or `Swarm.json`: the scene loads the published payload, not the
assets.

## Tuning without a rebuild

The scene's `BestiaryDemo` creates a provider with two loaders:

```csharp
new ConfigProvider(typeMapping, new ConfigLoader[]
{
    new TextAssetLoader(baseline),                                        // ConfigsBasics.json, in the build
    new FileLoader(FileLoader.PathRoot.PersistentData, "ConfigsBasics/overrides.json", skipIfMissing: true)
    {
        Policy = ConfigLoaderPolicy.Optional,                             // a broken file is skipped with a warning
    },
});
```

Later loaders replace templates of earlier ones **by ID**, whole templates at a time. In play mode:

1. **Write example override** writes `overrides.json` to the persistent data path (path shown on screen) with
   one template: the Goblin as "Goblin Warlord", stronger. It reloads: the Goblin is replaced, everything
   referencing it still works.
2. **Open override folder**, edit the file (e.g. its `Health`), save, press **Reload**.
3. Break the JSON on purpose and reload: the override is skipped with a warning, the baseline stays.
4. **Delete override** goes back to the baseline.

The same layering works with a `RemoteConfigLoader` (Unity Remote Config) or your own `ConfigLoader` instead of
the file, e.g. for live tuning.

## The data

```
Configs/                         config set ConfigsBasics (its folder = the folder of the set)
  ConfigsBasics.asset            publish target: File → ConfigsBasics.json next to the set
  ConfigsBasicsTypeMapping.asset written by push; the scene references it
  ConfigsBasics.json             the published payload (excluded from the set's JSON files)
  Elements/  Fire, Ice, Poison   ElementTemplate, enum mapped to Element
  Items/     Coin, Fang, DragonScale
  Enemies/   Goblin, Wolf        EnemyTemplate, with icons
             Dragon              BossTemplate : EnemyTemplate
             Swarm.json          Rat, Bat and the boss Rat King as JSON templates
```

New templates: *Assets > Create > Configs Basics > �* anywhere under `Configs/`, then push.

## What to look at

| Feature | Where |
|---|---|
| Template classes; IDs from group + asset name (`Enemy+Goblin`) | `Scripts/Templates`, `[TemplateGroup]` |
| Subtypes: `GetCollection<EnemyTemplate>()` returns bosses too; bosses share the `Enemy` group | `BossTemplate`, `Bestiary.Build` |
| `TemplateReference<T>`: object references in assets, IDs in JSON and payloads | `EnemyTemplate.Loot`, `BossTemplate.Minion`, `Swarm.json` |
| JSON templates referencing assets (`Item+Coin`) and each other (`Rat King` → `Enemy+Rat`) | `Swarm.json` |
| `TemplateAssetReference<Sprite>` loaded through Addressables | `EnemyTemplate.Icon`, `BestiaryDemo.LoadIcon` |
| Enum mapping: `GetTemplate<ElementTemplate>(Element.Fire)` | `ElementTemplate`, `Bestiary.Build` |
| Config set, validation with fixes, push to a file | `Configs/ConfigsBasics.asset`, *Tools > Configs* |
| Layered loaders, optional policy, reload, serializing a template | `BestiaryDemo` |
| Browsing all templates of the set by type | *Tools > Configs > Open Window* > Browse Templates |

To iterate on the assets without pushing, add an `EditorLoader` for the set as the last loader: it bakes the set
in memory whenever the provider loads (editor only; loads nothing in a player).

## With Scopes

With Scopes installed, the same loaders go into a `ConfigManager` (in a scope definition's manager list,
configured in the inspector), and managers get the configs by injection:

```csharp
[Inject] private IConfigManager _configs;

List<BestiaryEntry> entries = Bestiary.Build(_configs);   // IConfigManager is an IConfigProvider
```

## Tests

- `Tests/Editor/BestiaryTests`: unit tests of the game logic with `InMemoryConfigProvider`
  (Kott.Configs.Testing): templates created in code, no assets.
- `Tests/Editor/TestSetTests`: bakes the small config set in `Tests/Editor/TestConfigs` (assets + a JSON file)
  like a push, loads the payload like the game and checks references across assets and JSON. It has its own
  set, so editing the sample's data doesn't break it.
