using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace PlogonAdventure.Windows;

public class DebugWindow : Window
{
    public static List<string> debugLines { get; set; } = new();

    public DebugWindow() : base("Debug")
    {
        var resolution = ImGui.GetMainViewport().Size;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 300),
            MaximumSize = resolution,
        };
    }

    public override void Draw()
    {
        foreach (var line in debugLines)
        {
            ImGui.TextUnformatted(line);
        }
        debugLines.Clear();
    }
}
