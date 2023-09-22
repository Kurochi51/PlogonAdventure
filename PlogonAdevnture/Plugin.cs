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
using System.Linq;
using System;
using Dalamud.Logging;
using static Lumina.Data.Parsing.Uld.NodeData;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

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

        public List<AtkResNode> nodeList { get; set; } = null!;
        public List<AtkResNode> textNodeList {  get; set; } = null!;
        public IDictionary<uint, NodeType> nodeDictionary { get; private set; } = null!;
        public IDictionary<uint, string?> textNodeDictionary { get; private set; } = null!;
        public bool addonAvailable { get; set; }
        private readonly string lookupAddonName = "CharacterStatus";

        public Plugin(DalamudPluginInterface _pluginInterface,
            Framework _framework,
            ICommandManager _commandManager,
            IAddonLifecycle _addonLifecycle)
        {
            pluginInterface = _pluginInterface;
            framework = _framework;
            commandManager = _commandManager;
            addonLifecycle = _addonLifecycle;
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
            addonLifecycle.RegisterListener(AddonEvent.PostSetup, lookupAddonName, OnPostSetup);
            addonLifecycle.RegisterListener(AddonEvent.PreFinalize, lookupAddonName, OnPreFinalize);
        }

        private unsafe void OnPostSetup(AddonEvent eventType, AddonArgs addonInfo)
        {
            addonAvailable = true;
            var addon = (AtkUnitBase*)addonInfo.Addon;
            var addonName = addonInfo.AddonName;
            var addonNodeList = addon->UldManager.NodeList;
            var nodeAmount = addon->UldManager.NodeListCount;
            if (nodeList is null || textNodeList is null || nodeList.Count == 0 || textNodeList.Count == 0)
            {
                nodeList = new List<AtkResNode>();
                nodeDictionary = new Dictionary<uint, NodeType>();
                textNodeList = new List<AtkResNode>();
                textNodeDictionary = new Dictionary<uint, string?>();
                for (ushort i = 0; i < nodeAmount; i++)
                {
                    var currentNode = addonNodeList[i];
                    var currentNodeID = addonNodeList[i]->NodeID;
                    var nodeType = currentNode->Type;
                    nodeList.Add(*currentNode);
                    if (!nodeDictionary.ContainsKey(currentNodeID))
                    {
                        nodeDictionary.Add(currentNodeID, nodeType);
                    }
                    if (nodeType == NodeType.Text)
                    {
                        var textNode = currentNode->GetAsAtkTextNode();
                        textNodeList.Add(*currentNode);
                        var text = MemoryHelper.ReadStringNullTerminated((nint)textNode->GetText());
                        if (string.IsNullOrEmpty(text))
                        {
                            PluginLog.Warning("Payload information about child node {id}", currentNodeID);
                            var nodeSeStringBytes = new byte[textNode->NodeText.BufUsed];
                            for (var byteIndex = 0L; byteIndex < textNode->NodeText.BufUsed; byteIndex++)
                            {
                                nodeSeStringBytes[byteIndex] = textNode->NodeText.StringPtr[byteIndex];
                            }
                            var seString = SeString.Parse(nodeSeStringBytes);
                            for (var payloadIndex = 0; payloadIndex < seString.Payloads.Count; payloadIndex++)
                            {
                                var payload = seString.Payloads[payloadIndex];
                                if (payload is TextPayload tp && payload.Type == PayloadType.RawText)
                                {
                                    PluginLog.Debug($"cursed information here: {tp.Text}");
                                }
                            }
                        }
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
                        var childID = (currentNodeID*10)+ childNode->NodeID;
                        nodeList.Add(*childNode);
                        if (!nodeDictionary.ContainsKey(childID))
                        {
                            nodeDictionary.Add(childID, childNode->Type);
                        }
                        if (childNode->Type == NodeType.Text)
                        {
                            var childTextNode = childNode->GetAsAtkTextNode();
                            var childText = MemoryHelper.ReadStringNullTerminated((nint)childTextNode->GetText());
                            if (string.IsNullOrEmpty(childText))
                            {
                                PluginLog.Warning("Payload information about child node {id}", childID);
                                var childSeStringBytes = new byte[childTextNode->NodeText.BufUsed];
                                for (var byteIndex = 0L; byteIndex < childTextNode->NodeText.BufUsed; byteIndex++)
                                {
                                    childSeStringBytes[byteIndex] = childTextNode->NodeText.StringPtr[byteIndex];
                                }
                                var childSeString = SeString.Parse(childSeStringBytes);
                                for (var childPIndex = 0; childPIndex < childSeString.Payloads.Count; childPIndex++)
                                {
                                    var childPayload = childSeString.Payloads[childPIndex];
                                    if (childPayload is TextPayload childTP && childPayload.Type == PayloadType.RawText)
                                    {
                                        PluginLog.Debug($"child node cursed information here: {childTP.Text}");
                                    }
                                }
                            }
                            textNodeList.Add(*childNode);
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
                var text = addon->GetNodeById(31)->GetComponent()->GetTextNodeById(2)->GetAsAtkTextNode()->GetText();
                var textAddress = ((nint)text).ToString("X");
                MainWindow.info2 = $"textNodeConverted->GetText() is {textAddress}";
                var actualText = MemoryHelper.ReadStringNullTerminated((nint)text);
                MainWindow.info3 = $"god help this poor soul: {actualText}";
                MainWindow.info4 = $"Amount of Nodes found: {nodeList.Count}";
            }
        }

        private unsafe void OnPreFinalize(AddonEvent eventType, AddonArgs addonInfo)
        {
            addonAvailable = false;
            nodeList?.Clear();
            nodeList = null!;
            nodeDictionary?.Clear();
            nodeDictionary = null!;
            textNodeList?.Clear();
            textNodeList = null!;
            textNodeDictionary?.Clear();
            textNodeDictionary = null!;
        }

        private unsafe void OnFrameworkUpdate(Framework framework)
        {
            MainWindow.IsOpen = config.isEnabled;
        }

        public void Dispose()
        {
            nodeList = null!;
            nodeDictionary = null!;
            textNodeList = null!;
            textNodeDictionary = null!;
            WindowSystem.RemoveAllWindows();
            commandManager.RemoveHandler(configdName);
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            framework.Update -= OnFrameworkUpdate;
            addonLifecycle.UnregisterListener(AddonEvent.PostSetup, lookupAddonName, OnPostSetup);
            addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, lookupAddonName, OnPreFinalize);
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
