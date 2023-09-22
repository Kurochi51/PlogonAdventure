using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;

namespace PlogonAdventure.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration config;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly MainWindow mainWindow;

    public ConfigWindow(DalamudPluginInterface _pluginInterface,Configuration _config, MainWindow _mainWindow) : base("PA Config Window")
    {
        config = _config;
        pluginInterface = _pluginInterface;
        mainWindow = _mainWindow;
        Size = new(300, 200);
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("This is the config window hehe.");
        var enable = config.isEnabled;
        if (ImGui.Checkbox("Enable Plugin", ref enable))
        {
            config.isEnabled = enable;
        }
        DrawCloseButton();
    }

    public override void OnClose()
    {
        config.Save(pluginInterface);
    }

    private void DrawCloseButton()
    {
        var originPos = ImGui.GetCursorPos();
        // Place a button in the bottom left
        ImGui.SetCursorPosX(10f);
        ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight() - 5f);
        if (ImGui.Button("Debug"))
        {
            mainWindow.Toggle();
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
