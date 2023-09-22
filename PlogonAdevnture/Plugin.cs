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

namespace PlogonAdventure
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Plogon Adevnture";
        private readonly string configdName = "/pga";

        public WindowSystem WindowSystem { get; } = new("PlogonAdventure");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private readonly Configuration config;
        private readonly DalamudPluginInterface pluginInterface;
        private readonly Framework framework;
        private readonly ICommandManager commandManager;
        private readonly IAddonLifecycle addonLifecycle;
        private readonly IGameGui gameGui;

        public IDictionary<uint, NodeType> nodeDictionary { get; private set; } = null!;
        public IDictionary<uint, string?> textNodeDictionary { get; private set; } = null!;
        public bool addonAvailable { get; private set; }
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

            MainWindow = new MainWindow(pluginInterface, config, this);
            ConfigWindow = new ConfigWindow(pluginInterface, config, MainWindow);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

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
            if (characterStatus is null || characterStatus->RootNode is null || characterStatus->RootNode->ChildNode is null || characterStatus->UldManager.NodeList is null)
            {
                return;
            }
            addonAvailable = true;
            ProcessAddonNodes(characterStatus, lookupAddonName);
        }

        private unsafe void OnPreFinalize(AddonEvent eventType, AddonArgs addonInfo)
        {
            addonAvailable = false;
            nodeDictionary?.Clear();
            nodeDictionary = null!;
            textNodeDictionary?.Clear();
            textNodeDictionary = null!;
        }

        private unsafe void ProcessAddonNodes(AtkUnitBase* addon, string addonName)
        {
            var addonNodeList = addon->UldManager.NodeList;
            var nodeAmount = addon->UldManager.NodeListCount;
            if (nodeDictionary is null || textNodeDictionary is null || nodeDictionary.Count == 0 || textNodeDictionary.Count == 0)
            {
                nodeDictionary = new Dictionary<uint, NodeType>();
                textNodeDictionary = new Dictionary<uint, string?>();
                for (ushort i = 0; i < nodeAmount; i++)
                {
                    var currentNode = addonNodeList[i];
                    var currentNodeID = addonNodeList[i]->NodeID;
                    var nodeType = currentNode->Type;
                    if (!nodeDictionary.ContainsKey(currentNodeID))
                    {
                        nodeDictionary.Add(currentNodeID, nodeType);
                    }
                    if (nodeType == NodeType.Text)
                    {
                        var textNode = (AtkTextNode*)currentNode;
                        var text = MemoryHelper.ReadStringNullTerminated((nint)textNode->GetText());
                        if (!textNodeDictionary.ContainsKey(currentNodeID))
                        {
                            textNodeDictionary.Add(currentNodeID, text);
                        }
                    }
                    if ((int)nodeType < 1000)
                    {
                        continue;
                    }
                    var componentNode = currentNode->GetAsAtkComponentNode();
                    var componentUldManager = componentNode->Component->UldManager;
                    var objectInfo = (AtkUldComponentInfo*)componentUldManager.Objects;
                    if (objectInfo == null)
                    {
                        continue;
                    }
                    var childCount = componentUldManager.NodeListCount;
                    var componentList = componentUldManager.NodeList;
                    for (var j = 0; j < childCount; j++)
                    {
                        var childNode = componentList[j];
                        var childID = (currentNodeID * 100) + childNode->NodeID;
                        if (!nodeDictionary.ContainsKey(childID))
                        {
                            nodeDictionary.Add(childID, childNode->Type);
                        }
                        if (childNode->Type == NodeType.Text)
                        {
                            var childTextNode = (AtkTextNode*)childNode;
                            var childText = MemoryHelper.ReadStringNullTerminated((nint)childTextNode->GetText());
                            if (!textNodeDictionary.ContainsKey(childID))
                            {
                                textNodeDictionary.Add(childID, childText);
                            }
                        }
                    }
                }
            }
            MainWindow.info = addonName + " addon is visible? " + addon->IsVisible.ToString();
            if (addonName.Equals("CharacterStatus"))
            {
                var text = addon->GetNodeById(31)->GetComponent()->GetTextNodeById(2)->GetAsAtkTextNode();
                var textNode = addon->GetNodeById(4)->GetAsAtkTextNode();
                var textAddress = ((nint)text).ToString("X");
                var textNodeAddress = ((nint)textNode).ToString("X");
                MainWindow.info2 = $"textNode address is: {textNodeAddress} and text address is: {textAddress}";
                var actualText = MemoryHelper.ReadStringNullTerminated((nint)text->GetText());
                var actualTextAgain = MemoryHelper.ReadStringNullTerminated((nint)textNode->GetText());
                MainWindow.info3 = $"random text is: {actualText} and {actualTextAgain}";
                MainWindow.info4 = $"Amount of Nodes found: {nodeDictionary.Count}";
            }
        }

        private unsafe void OnFrameworkUpdate(Framework framework)
        {
            MainWindow.IsOpen = config.isEnabled;
        }

        public void Dispose()
        {
            nodeDictionary = null!;
            textNodeDictionary = null!;
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
