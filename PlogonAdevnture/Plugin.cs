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

        public IDictionary<uint, NodeType> nodeDictionary { get; private set; } = null!;
        public IDictionary<uint, string?> textNodeDictionary { get; private set; } = null!;
        public bool addonAvailable { get; private set; }
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
                        if (string.IsNullOrEmpty(text))
                        {
                            text = "Empty text node at " + ((nint)textNode).ToString("X");
                            var altText = Marshal.PtrToStringAnsi(new IntPtr(textNode->NodeText.StringPtr));
                            text = text + " possible text? " + altText ?? "null";
                            /*PluginLog.Warning("Payload information about child node {id}", currentNodeID);
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
                            }*/
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
                        if (!nodeDictionary.ContainsKey(childID))
                        {
                            nodeDictionary.Add(childID, childNode->Type);
                        }
                        if (childNode->Type == NodeType.Text)
                        {
                            var childTextNode = (AtkTextNode*)childNode;
                            var childText = MemoryHelper.ReadStringNullTerminated((nint)childTextNode->GetText());
                            if (string.IsNullOrEmpty(childText))
                            {
                                childText = "Empty child text node at " + ((nint)childTextNode).ToString("X");
                                var altText = Marshal.PtrToStringAnsi(new IntPtr(childTextNode->NodeText.StringPtr));
                                childText = childText + " possible text? " + altText ?? "null";
                                /*PluginLog.Warning("Payload information about child node {id}", childID);
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
                                }*/
                            }
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
                //var text = addon->GetNodeById(31)->GetComponent()->GetTextNodeById(2)->GetAsAtkTextNode()->GetText();
                var textNode = addon->GetNodeById(4)->GetAsAtkTextNode();
                var textNodeAddress = ((nint)textNode).ToString("X");
                MainWindow.info2 = $"text address is: {textNodeAddress}";
                var text = MemoryHelper.ReadStringNullTerminated((nint)textNode->GetText());
                var altText = Marshal.PtrToStringAnsi(new IntPtr(textNode->NodeText.StringPtr));
                var yetAnotherText = textNode->NodeText.ToString();
                MainWindow.info3 = $"actual text is: {text} or {altText} or {yetAnotherText}";
                MainWindow.info4 = $"Amount of Nodes found: {nodeDictionary.Count}";
            }
        }

        private unsafe void OnPreFinalize(AddonEvent eventType, AddonArgs addonInfo)
        {
            addonAvailable = false;
            nodeDictionary?.Clear();
            nodeDictionary = null!;
            textNodeDictionary?.Clear();
            textNodeDictionary = null!;
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
