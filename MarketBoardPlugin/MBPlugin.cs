// <copyright file="MBPlugin.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Globalization;
  using Dalamud.ContextMenu;
  using Dalamud.Game.Command;
  using Dalamud.Game.Text;
  using Dalamud.Game.Text.SeStringHandling;
  using Dalamud.Game.Text.SeStringHandling.Payloads;
  using Dalamud.Interface.Windowing;
  using Dalamud.IoC;
  using Dalamud.Plugin;
  using Dalamud.Plugin.Services;
  using Lumina.Excel.GeneratedSheets;

  using MarketBoardPlugin.GUI;
  using MarketBoardPlugin.Models.ShoppingList;

  /// <summary>
  /// The entry point of the plugin.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Plugin entry point")]
  public class MBPlugin : IDalamudPlugin
  {
    private readonly DalamudContextMenu contextMenuBase;

    private readonly InventoryContextMenuItem inventoryContextMenuItem;

    private readonly MarketBoardWindow marketBoardWindow;

    private readonly MarketBoardConfigWindow marketBoardConfigWindow;

    private readonly MarketBoardShoppingListWindow marketBoardShoppingListWindow;

    /// <summary>
    /// Gets the window system.
    /// </summary>
    private readonly WindowSystem windowSystem = new(typeof(MBPlugin).AssemblyQualifiedName);

    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MBPlugin"/> class.
    /// This is the plugin's entry point.
    /// </summary>
    public MBPlugin()
    {
      this.Config = PluginInterface.GetPluginConfig() as MBPluginConfig ?? new MBPluginConfig();

      this.marketBoardWindow = new MarketBoardWindow(this);
      this.marketBoardConfigWindow = new MarketBoardConfigWindow(this);
      this.marketBoardShoppingListWindow = new MarketBoardShoppingListWindow(this);

      this.windowSystem.AddWindow(this.marketBoardWindow);
      this.windowSystem.AddWindow(this.marketBoardConfigWindow);
      this.windowSystem.AddWindow(this.marketBoardShoppingListWindow);

      // Set up command handlers
      CommandManager.AddHandler("/pmb", new CommandInfo(this.OnOpenMarketBoardCommand)
      {
        HelpMessage = "Open the market board window.",
      });

      PluginInterface.UiBuilder.Draw += this.DrawUi;
      PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
      PluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;

      // Set up context menu
      this.contextMenuBase = new DalamudContextMenu(PluginInterface);
      this.inventoryContextMenuItem = new InventoryContextMenuItem(
        new SeString(new TextPayload("Search with Market Board Plugin")), this.OnSelectContextMenuItem, true);
      this.contextMenuBase.OnOpenInventoryContextMenu += this.OnContextMenuOpened;

      // Set up number format
      this.NumberFormatInfo.CurrencySymbol = SeIconChar.Gil.ToIconString();
      this.NumberFormatInfo.CurrencyDecimalDigits = 0;

#if DEBUG
      this.marketBoardWindow.IsOpen = true;
#endif
    }

    /// <summary>
    /// Gets the plugin's name.
    /// </summary>
    public static string Name => "Market Board plugin";

    /// <summary>
    /// Gets the plugin's configuration.
    /// </summary>
    public MBPluginConfig Config { get; private set; }

    /// <summary>
    /// Gets the shopping list.
    /// </summary>
    public IList<SavedItem> ShoppingList { get; init; } = new List<SavedItem>();

    /// <summary>
    /// Gets the number format info.
    /// </summary>
    public NumberFormatInfo NumberFormatInfo { get; init; } = CultureInfo.CurrentCulture.NumberFormat.Clone() as NumberFormatInfo;

    [PluginService]
    internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static IDataManager Data { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reset the market data.
    /// </summary>
    public void ResetMarketData()
    {
      this.marketBoardWindow.ResetMarketData();
    }

    /// <summary>
    /// Open the main UI.
    /// </summary>
    public void OpenMainUi()
    {
      this.marketBoardWindow.IsOpen = true;
    }

    /// <summary>
    /// Open the config UI.
    /// </summary>
    public void OpenConfigUi()
    {
      this.marketBoardConfigWindow.IsOpen = true;
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
        PluginInterface.SavePluginConfig(this.Config);

        // Remove windows
        this.windowSystem.RemoveAllWindows();
        this.marketBoardWindow.Dispose();

        // Remove command handlers
        CommandManager.RemoveHandler("/pmb");

        // Remove context menu handler
        this.contextMenuBase.OnOpenInventoryContextMenu -= this.OnContextMenuOpened;
        this.contextMenuBase.Dispose();
      }

      this.isDisposed = true;
    }

    private void OnContextMenuOpened(InventoryContextMenuOpenArgs args)
    {
      if (!this.Config.ContextMenuIntegration)
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

      if (args.ItemId == 0)
      {
        return;
      }

      args.AddCustomItem(this.inventoryContextMenuItem);
    }

    private void OnSelectContextMenuItem(InventoryContextMenuItemSelectedArgs args)
    {
      try
      {
        this.marketBoardWindow.IsOpen = true;
        this.marketBoardWindow.ChangeSelectedItem(args.ItemId);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed on context menu for itemId" + args.ItemId);
      }
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

    private void DrawUi()
    {
      this.windowSystem.Draw();
    }
  }
}
