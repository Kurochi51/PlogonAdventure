using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using System.Numerics;

namespace PlogonAdventure.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration config;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly DebugWindow debugWindow;

    public ConfigWindow(DalamudPluginInterface _pluginInterface,Configuration _config, DebugWindow _debugWindow) : base("PA Config Window")
    {
        config = _config;
        pluginInterface = _pluginInterface;
        debugWindow = _debugWindow;
        var resolution = ImGui.GetMainViewport().Size;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 150),
            MaximumSize = resolution,
        };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("This is the config window hehe.");
        var enable = config.isEnabled;
        if (ImGui.Checkbox("Enable Plugin", ref enable))
        {
            config.isEnabled = enable;
        }
        DrawButtons();
    }

    public override void OnClose()
    {
        config.Save(pluginInterface);
    }

    private void DrawButtons()
    {
        var originPos = ImGui.GetCursorPos();
        // Place a button in the bottom left
        ImGui.SetCursorPosX(10f);
        ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight() - 5f);
        if (ImGui.Button("Debug"))
        {
            debugWindow.Toggle();
        }
        ImGui.SetCursorPos(originPos);
        // Place a button in the bottom right + some padding / extra space
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("Close").X - 10f);
        ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight() - 5f);
        if (ImGui.Button("Close"))
        {
            config.Save(pluginInterface);
            IsOpen = false;
        }
        ImGui.SetCursorPos(originPos);
    }
}
