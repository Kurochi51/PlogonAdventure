using Dalamud.Configuration;
using Dalamud.Plugin;

namespace PlogonAdventure
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool isEnabled { get; set; } = false;
        public void Save(DalamudPluginInterface pi) => pi.SavePluginConfig(this);
    }
}
