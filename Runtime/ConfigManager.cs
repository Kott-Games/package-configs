using System.Collections.Generic;
using Kott.Scopes;

namespace Kott.Configs
{
    public abstract class ConfigManager : Manager<ConfigManager>
    {
        // public override async Task InitializeAsync()
        // {
        //     Debug.Log($"Template: {GetCollection<Template>().Select(t => t.FormatID()).JoinString(", ")}");
        //     Debug.Log($"GameSettingsTemplate: {GetCollection<GameSettingsTemplate>().Select(t => t.FormatID()).JoinString(", ")}");
        //     Debug.Log($"TowerTemplate: {GetCollection<TowerTemplate>().Select(t => t.FormatID()).JoinString(", ")}");
        //     Debug.Log($"SpecialTowerTemplate: {GetCollection<SpecialTowerTemplate>().Select(t => t.FormatID()).JoinString(", ")}");
        //     Debug.Log($"BuildingTemplate: {GetCollection<BuildingTemplate>().Select(t => t.FormatID()).JoinString(", ")}");
        // 
        //     Debug.Log($"GameSettingsTemplate default: {GetTemplate<GameSettingsTemplate>("game_settings:default").FormatID()}");
        //     Debug.Log($"BuildingTemplate kanon: {GetTemplate<BuildingTemplate>("building:kanon").FormatID()}");
        //     Debug.Log($"BuildingTemplate small_hut: {GetTemplate<BuildingTemplate>("building:small_hut").FormatID()}");
        // 
        //     await base.InitializeAsync();
        // }

        public abstract IReadOnlyList<T> GetCollection<T>() where T : Template;
        public abstract T GetTemplate<T>(string key) where T : Template;
    }
}