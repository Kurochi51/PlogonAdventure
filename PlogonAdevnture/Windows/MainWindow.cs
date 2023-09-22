using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Windowing;
using Dalamud.Interface.Raii;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace PlogonAdventure.Windows;

public class MainWindow : Window
{
    private readonly Configuration config;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly Plugin plugin;
    private readonly DebugWindow debugWindow;
    public string info { get; set; } = "wut";
    public static List<string> infoLines { get; set; } = new();
    private float coulmn1, coulmn2;
    public MainWindow(DalamudPluginInterface _pluginInterface, Configuration _config, Plugin _plugin, DebugWindow _debugWindow) : base("PA Main Window")
    {
        config = _config;
        pluginInterface = _pluginInterface;
        plugin = _plugin;
        debugWindow = _debugWindow;
        var resolution = ImGui.GetMainViewport().Size;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 300),
            MaximumSize = resolution,
        };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("This is the main window hehe.");
        ImGui.TextUnformatted(info);
        foreach (var line in infoLines)
        {
            ImGui.TextUnformatted(line);
        }
        infoLines.Clear();
        var Table1Column1 = " NodeAddress";
        var Table1Column2 = "NodeType";
        if (plugin.addonAvailable)
        {
            ImGui.Text($"{plugin.textNodes} of {plugin.nodeDictionary.Count} are text nodes");
            DetermineColumnWidth(Table1Column1, Table1Column2, plugin.nodeDictionary, ref coulmn1, ref coulmn2);
            using var scrollArea = ImRaii.Child("ScrollArea", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 40f), border: true);
            DrawTable("NodeListTable", Table1Column1, Table1Column2, coulmn1, coulmn2, plugin.nodeDictionary);
        }
        DrawButtons();
    }

    private static void DetermineColumnWidth(string column1, string column2, IDictionary<string, NodeType> dictionary, ref float column1Width, ref float column2Width)
    {
        column1Width = ImGui.CalcTextSize(column1).X;
        column2Width = ImGui.CalcTextSize(column2).X;
        foreach (var item in dictionary)
        {
            var keyWidth = ImGui.CalcTextSize(item.Key.ToString());
            if (keyWidth.X > column1Width)
            {
                column1Width = keyWidth.X;
            }
            var ValueWidth = ImGui.CalcTextSize(item.Value.ToString());
            if (ValueWidth.X > column2Width)
            {
                column2Width = ValueWidth.X;
            }
        }
    }

    private static void DrawTable(string id, string column1, string column2, float column1Width, float column2Width, IDictionary<string, NodeType> dictionary)
    {
        using var table = ImRaii.Table(id, 2, ImGuiTableFlags.None);

        ImGui.TableSetupColumn(column1, ImGuiTableColumnFlags.WidthFixed, column1Width);
        ImGui.TableSetupColumn(column2, ImGuiTableColumnFlags.WidthFixed, column2Width);
        ImGui.TableHeadersRow();
        ImGui.TableNextRow();

        for (var i = 0; i < dictionary.Count; i++)
        {
            var entry = dictionary.ElementAt(i);
            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"{entry.Key}");

            ImGui.TableSetColumnIndex(1);
            ImGui.Text($"{entry.Value}");

            if (i + 1 < dictionary.Count)
            {
                ImGui.TableNextRow();
            }
        }
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
    }
    public override void OnClose()
    {
        config.Save(pluginInterface);
    }
}
