using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PlogonAdventure.Windows;
using Dalamud.Plugin.Services;
using Dalamud.Game;
using Dalamud.Game.Addon;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Memory;
using System.Collections.Generic;
using System;
using Dalamud.Logging;
using System.Runtime.InteropServices;
using System.Linq;
using static Lumina.Data.Parsing.Uld.NodeData;

namespace PlogonAdventure
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Plogon Adevnture";
        private readonly string configdName = "/pga";

        public WindowSystem WindowSystem { get; } = new("PlogonAdventure");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }
        private DebugWindow DebugWindow { get; init; }

        private readonly Configuration config;
        private readonly DalamudPluginInterface pluginInterface;
        private readonly Framework framework;
        private readonly ICommandManager commandManager;
        private readonly IAddonLifecycle addonLifecycle;
        private readonly IGameGui gameGui;

        public IDictionary<string, NodeType> nodeDictionary { get; private set; } = null!;
        public bool addonAvailable { get; private set; }
        public int textNodes { get; private set; }
        private readonly string lookupAddonName = "CharacterStatus";
        private readonly string altAddonName = "Character";

        public Plugin(DalamudPluginInterface _pluginInterface,
            Framework _framework,
            ICommandManager _commandManager,
            IAddonLifecycle _addonLifecycle,
            IGameGui _gameGui)
        {
            pluginInterface = _pluginInterface;
            framework = _framework;
            commandManager = _commandManager;
            addonLifecycle = _addonLifecycle;
            gameGui = _gameGui;
            config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            DebugWindow = new DebugWindow();
            ConfigWindow = new ConfigWindow(pluginInterface, config, DebugWindow);
            MainWindow = new MainWindow(pluginInterface, config, this, DebugWindow);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(DebugWindow);

            commandManager.AddHandler(configdName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the config window."
            });

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            framework.Update += OnFrameworkUpdate;
            addonLifecycle.RegisterListener(AddonEvent.PostDraw, altAddonName, OnPostDraw);
            addonLifecycle.RegisterListener(AddonEvent.PreFinalize, altAddonName, OnPreFinalize);
        }

        private unsafe void OnPostDraw(AddonEvent eventType, AddonArgs addonInfo)
        {
            var characterStatus = (AtkUnitBase*)gameGui.GetAddonByName(lookupAddonName);
#pragma warning disable S2589
            if (characterStatus is null || characterStatus->RootNode is null || characterStatus->RootNode->ChildNode is null || characterStatus->UldManager.NodeList is null)
            {
                return;
            }
#pragma warning restore S2589
            addonAvailable = true;
            var nodeDictionaryAvailable = false;
            if (nodeDictionary is null || nodeDictionary.Count == 0)
            {
                nodeDictionaryAvailable = true;
                PluginLog.Debug("nodeDictionary being built.");
                nodeDictionary = new Dictionary<string, NodeType>();
            }
            DrawNode(&characterStatus->UldManager, nodeDictionaryAvailable);
            MainWindow.info = lookupAddonName + " addon is visible? " + characterStatus->IsVisible.ToString();
            var text = characterStatus->GetNodeById(31)->GetComponent()->GetTextNodeById(2)->GetAsAtkTextNode();
            var textNode = characterStatus->GetNodeById(4)->GetAsAtkTextNode();
            var textAddress = ((nint)text).ToString("X");
            var textNodeAddress = ((nint)textNode).ToString("X");
            MainWindow.infoLines.Add("textNode address is: " + textNodeAddress + " and text address is: " + textAddress);
            var actualText = MemoryHelper.ReadStringNullTerminated((nint)text->GetText());
            var actualTextAgain = MemoryHelper.ReadStringNullTerminated((nint)textNode->GetText());
            MainWindow.infoLines.Add("random text is: " + actualText + " and " + actualTextAgain);
            MainWindow.infoLines.Add("Amount of nodes found: " + nodeDictionary.Count);
        }

        private void OnPreFinalize(AddonEvent eventType, AddonArgs addonInfo)
        {
            addonAvailable = false;
            nodeDictionary?.Clear();
            nodeDictionary = null!;
            textNodes = 0;
        }

        private unsafe void DrawNode(AtkUldManager* node, bool buildDictionary)
        {
            foreach (var index in Enumerable.Range(0, node->NodeListCount))
            {
                var subNode = node->NodeList[index];
                if (buildDictionary && !nodeDictionary!.ContainsKey(((nint)subNode).ToString("X")))
                {
                    nodeDictionary.Add(((nint)subNode).ToString("X"), subNode->Type);
                }
                if ((int)subNode->Type > 1000)
                {
                    var componentNode = subNode->GetComponent();
                    if (componentNode is not null)
                    {
                        DrawNode(&componentNode->UldManager, buildDictionary);
                    }
                }

                if (subNode->Type is NodeType.Text)
                {
                    var textNode = (AtkTextNode*)subNode;
                    textNodes++;
                    DebugWindow.debugLines.Add(((nint)subNode).ToString("X") + "       " + textNode->NodeText);
                }
            }
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            MainWindow.IsOpen = config.isEnabled;
            textNodes = 0;
            addonAvailable = false;
        }

        public void Dispose()
        {
            nodeDictionary = null!;
            WindowSystem.RemoveAllWindows();
            commandManager.RemoveHandler(configdName);
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            framework.Update -= OnFrameworkUpdate;
            addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, altAddonName, OnPreFinalize);
            addonLifecycle.UnregisterListener(AddonEvent.PostDraw, altAddonName, OnPostDraw);
        }

        private void OnCommand(string command, string args)
        {
            ConfigWindow.Toggle();
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.Toggle();
        }
    }
}
