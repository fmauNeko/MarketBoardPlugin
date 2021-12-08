// <copyright file="MBPlugin.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Diagnostics.CodeAnalysis;

  using Dalamud.Data;
  using Dalamud.Game;
  using Dalamud.Game.ClientState;
  using Dalamud.Game.Command;
  using Dalamud.Game.Gui;

  using Dalamud.IoC;
  using Dalamud.Plugin;

  using Lumina.Excel.GeneratedSheets;

  using MarketBoardPlugin.GUI;

  using XivCommon;
  using XivCommon.Functions.ContextMenu;
  using XivCommon.Functions.ContextMenu.Inventory;

  /// <summary>
  /// The entry point of the plugin.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Plugin entry point")]
  public class MBPlugin : IDalamudPlugin
  {
    private readonly XivCommonBase xivCommon;
    private readonly string contextMenuSearchString;

    private readonly MarketBoardWindow marketBoardWindow;

    private readonly MBPluginConfig config;

    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MBPlugin"/> class.
    /// This is the plugin's entry point.
    /// </summary>
    public MBPlugin()
    {
      this.config = (MBPluginConfig)PluginInterface.GetPluginConfig() ?? new MBPluginConfig();

      this.marketBoardWindow = new MarketBoardWindow(this.config);

      // Set up command handlers
      CommandManager.AddHandler("/pmb", new CommandInfo(this.OnOpenMarketBoardCommand)
      {
        HelpMessage = "Open the market board window.",
      });

      PluginInterface.UiBuilder.Draw += this.BuildMarketBoardUi;

      this.contextMenuSearchString = Data?.Excel?.GetSheet<Addon>()?.GetRow(4379)?.Text?.RawString ?? "Search for Item";

      this.xivCommon = new XivCommonBase(Hooks.ContextMenu);

      this.xivCommon.Functions.ContextMenu.OpenInventoryContextMenu += this.ContextMenuOnOpenInventoryContextMenu;
      this.xivCommon.Functions.ContextMenu.OpenContextMenu += this.ContextMenuOnOpenContextMenu;

#if DEBUG
      this.marketBoardWindow.IsOpen = true;
#endif
    }

    /// <summary>
    /// Gets the plugin's name.
    /// </summary>
    public string Name => "Market Board plugin";

    [PluginService]
    internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static DataManager Data { get; private set; } = null!;

    [PluginService]
    internal static CommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static Framework Framework { get; private set; } = null!;

    [PluginService]
    internal static ClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static GameGui GameGui { get; private set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">A value indicating whether we are disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (this.isDisposed)
      {
        return;
      }

      if (disposing)
      {
        // Save config
        PluginInterface.SavePluginConfig(this.config);

        // Remove command handlers
        PluginInterface.UiBuilder.Draw -= this.BuildMarketBoardUi;
        CommandManager.RemoveHandler("/pmb");
        PluginInterface.Dispose();
        this.marketBoardWindow.Dispose();

        // Dispose of xivCommon
        this.xivCommon.Functions.ContextMenu.OpenInventoryContextMenu -= this.ContextMenuOnOpenInventoryContextMenu;
        this.xivCommon.Functions.ContextMenu.OpenContextMenu -= this.ContextMenuOnOpenContextMenu;
        this.xivCommon.Dispose();
      }

      this.isDisposed = true;
    }

    private void ContextMenuOnOpenContextMenu(ContextMenuOpenArgs args)
    {
      if (!this.config.ContextMenuIntegration)
      {
        return;
      }

      var i = (uint)(GameGui.HoveredItem % 500000);

      var item = Data.Excel.GetSheet<Item>()?.GetRow(i);
      if (item == null)
      {
        return;
      }

      if (item.IsUntradable)
      {
        return;
      }

      var index = args.Items.FindIndex(a => a is NativeContextMenuItem n && n.Name.TextValue == this.contextMenuSearchString);
      if (index < 0)
      {
        return;
      }

      args.Items.Add(new NormalContextMenuItem($"Search with Market Board Plugin", (_) =>
      {
        this.marketBoardWindow.IsOpen = true;
        this.marketBoardWindow.ChangeSelectedItem(i);
      }));
    }

    private void ContextMenuOnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
      if (!this.config.ContextMenuIntegration)
      {
        return;
      }

      var item = Data.Excel.GetSheet<Item>()?.GetRow(args.ItemId);
      if (item == null)
      {
        return;
      }

      if (item.IsUntradable)
      {
        return;
      }

      var index = args.Items.FindIndex(a => a is NativeContextMenuItem n && n.Name.TextValue == this.contextMenuSearchString);
      if (index < 0)
      {
        return;
      }

      args.Items.Insert(index + 1, new InventoryContextMenuItem("Search with Market Board Plugin", selectedArgs =>
      {
        this.marketBoardWindow.IsOpen = true;
        this.marketBoardWindow.ChangeSelectedItem(selectedArgs.ItemId);
      }));
    }

    private void OnOpenMarketBoardCommand(string command, string arguments)
    {
      if (!string.IsNullOrEmpty(arguments))
      {
        if (uint.TryParse(arguments, out var itemId))
        {
          this.marketBoardWindow.ChangeSelectedItem(itemId);
          this.marketBoardWindow.IsOpen = true;
        }
        else
        {
          this.marketBoardWindow.SearchString = arguments;
          this.marketBoardWindow.IsOpen = true;
        }
      }
      else
      {
        this.marketBoardWindow.IsOpen = !this.marketBoardWindow.IsOpen;
      }
    }

    private void BuildMarketBoardUi()
    {
      if (this.marketBoardWindow != null && this.marketBoardWindow.IsOpen)
      {
        this.marketBoardWindow.IsOpen = this.marketBoardWindow.Draw();
      }
    }
  }
}
