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
    public string info { get; set; } = "wut";
    public string info2 { get; set; } = "wut";
    public string info3 { get; set; } = "wut";
    public string info4 { get; set; } = "wut";
    private float coulmn1, coulmn2, column3, column4;
    public MainWindow(DalamudPluginInterface _pluginInterface, Configuration _config, Plugin _plugin) : base("PA Main Window")
    {
        config = _config;
        pluginInterface = _pluginInterface;
        plugin = _plugin;
        Size = new(550, 500);
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("This is the main window hehe.");
        ImGui.Text(info);
        ImGui.Text(info2);
        ImGui.Text(info3);
        ImGui.Text(info4);
        var Table1Column1 = " NodeID";
        var Table1Column2 = "NodeType";
        var Table2Column1 = " NodeID";
        var Table2Column2 = "Text";
        if (plugin.addonAvailable)
        {
            ImGui.Text($"{plugin.textNodeDictionary.Count} of {plugin.nodeDictionary.Count} are text nodes");
            DetermineColumnWidth(Table1Column1, Table1Column2, plugin.nodeDictionary, ref coulmn1, ref coulmn2);
            DetermineTextColumnWidth(Table2Column1, Table2Column2, plugin.textNodeDictionary, ref column3, ref column4);
            using (var scrollArea = ImRaii.Child("ScrollArea", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 40f), border: true))
            {
                DrawTable("NodeListTable", Table1Column1, Table1Column2, coulmn1, coulmn2, plugin.nodeDictionary);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawTextTable("TextListTable", Table2Column1, Table2Column2, column3, column4, plugin.textNodeDictionary);
            }
        }
    }

    private static void DetermineColumnWidth(string column1, string column2, IDictionary<uint, NodeType> dictionary1, ref float column1Width, ref float column2Width)
    {
        column1Width = ImGui.CalcTextSize(column1).X;
        column2Width = ImGui.CalcTextSize(column2).X;
        foreach (var item in dictionary1)
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

    private static void DrawTable(string id, string column1, string column2, float column1Width, float column2Width, IDictionary<uint, NodeType> dictionary)
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

    private static void DetermineTextColumnWidth(string column1, string column2, IDictionary<uint, string?> dictionary1, ref float column1Width, ref float column2Width)
    {
        column1Width = ImGui.CalcTextSize(column1).X;
        column2Width = ImGui.CalcTextSize(column2).X;
        foreach (var item in dictionary1)
        {
            var keyWidth = ImGui.CalcTextSize(item.Key.ToString());
            if (keyWidth.X > column1Width)
            {
                column1Width = keyWidth.X;
            }
            var ValueWidth = ImGui.CalcTextSize(item.Value?.ToString());
            if (ValueWidth.X > column2Width)
            {
                column2Width = ValueWidth.X;
            }
        }
    }

    private static void DrawTextTable(string id, string column1, string column2, float column1Width, float column2Width, IDictionary<uint, string?> dictionary)
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

    public override void OnClose()
    {
        config.Save(pluginInterface);
    }
}
