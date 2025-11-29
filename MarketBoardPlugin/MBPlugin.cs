// <copyright file="MBPlugin.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Diagnostics.CodeAnalysis;
  using System.Globalization;
  using Dalamud.Game.ClientState.Objects.Enums;
  using Dalamud.Game.Command;
  using Dalamud.Game.Gui.ContextMenu;
  using Dalamud.Game.Text;
  using Dalamud.Interface.Windowing;
  using Dalamud.Plugin;
  using Dalamud.Plugin.Services;
  using FFXIVClientStructs.FFXIV.Client.UI.Agent;
  using Lumina.Excel.Sheets;

  using MarketBoardPlugin.GUI;
  using MarketBoardPlugin.Helpers;
  using MarketBoardPlugin.Models.ShoppingList;

  /// <summary>
  /// The entry point of the plugin.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Plugin entry point")]
  public class MBPlugin : IDalamudPlugin
  {
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
    /// <param name="pluginInterface">The Dalamud plugin interface.</param>
    /// <param name="dataManager">The data manager.</param>
    /// <param name="commandManager">The command manager.</param>
    /// <param name="framework">The framework.</param>
    /// <param name="clientState">The client state.</param>
    /// <param name="gameGui">The game GUI.</param>
    /// <param name="textureProvider">The texture provider.</param>
    /// <param name="log">The plugin log.</param>
    /// <param name="contextMenu">The context menu.</param>
    public MBPlugin(
      IDalamudPluginInterface pluginInterface,
      IDataManager dataManager,
      ICommandManager commandManager,
      IFramework framework,
      IClientState clientState,
      IGameGui gameGui,
      ITextureProvider textureProvider,
      IPluginLog log,
      IContextMenu contextMenu)
    {
      this.PluginInterface = pluginInterface;
      this.DataManager = dataManager;
      this.CommandManager = commandManager;
      this.Framework = framework;
      this.ClientState = clientState;
      this.GameGui = gameGui;
      this.TextureProvider = textureProvider;
      this.Log = log;
      this.ContextMenu = contextMenu;

      this.UniversalisClient = new UniversalisClient(this);

      this.Config = this.PluginInterface.GetPluginConfig() as MBPluginConfig ?? new MBPluginConfig();

      this.marketBoardWindow = new MarketBoardWindow(this);
      this.marketBoardConfigWindow = new MarketBoardConfigWindow(this);
      this.marketBoardShoppingListWindow = new MarketBoardShoppingListWindow(this);

      this.windowSystem.AddWindow(this.marketBoardWindow);
      this.windowSystem.AddWindow(this.marketBoardConfigWindow);
      this.windowSystem.AddWindow(this.marketBoardShoppingListWindow);

      // Set up command handlers
      this.CommandManager.AddHandler("/pmb", new CommandInfo(this.OnOpenMarketBoardCommand)
      {
        HelpMessage = "Open the market board window.",
      });

      this.PluginInterface.UiBuilder.Draw += this.DrawUi;
      this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
      this.PluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;

      // Set up context menu
      this.ContextMenu.OnMenuOpened += this.OnContextMenuOpened;

      // Set up number format
      if (this.NumberFormatInfo != null)
      {
        this.NumberFormatInfo.CurrencySymbol = SeIconChar.Gil.ToIconString();
        this.NumberFormatInfo.CurrencyDecimalDigits = 0;
      }

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
    public NumberFormatInfo NumberFormatInfo { get; init; } = (CultureInfo.CurrentCulture.NumberFormat.Clone() as NumberFormatInfo)!;

    /// <summary>
    /// Gets the Dalamud plugin interface.
    /// </summary>
    public IDalamudPluginInterface PluginInterface { get; init; }

    /// <summary>
    /// Gets the data manager.
    /// </summary>
    public IDataManager DataManager { get; init; }

    /// <summary>
    /// Gets the command manager.
    /// </summary>
    public ICommandManager CommandManager { get; init; }

    /// <summary>
    /// Gets the framework.
    /// </summary>
    public IFramework Framework { get; init; }

    /// <summary>
    /// Gets the client state.
    /// </summary>
    public IClientState ClientState { get; init; }

    /// <summary>
    /// Gets the game GUI.
    /// </summary>
    public IGameGui GameGui { get; init; }

    /// <summary>
    /// Gets the texture provider.
    /// </summary>
    public ITextureProvider TextureProvider { get; init; }

    /// <summary>
    /// Gets the plugin log.
    /// </summary>
    public IPluginLog Log { get; init; }

    /// <summary>
    /// Gets the context menu.
    /// </summary>
    public IContextMenu ContextMenu { get; init; }

    /// <summary>
    /// Gets the Universalis client used for accessing market board data.
    /// </summary>
    public UniversalisClient UniversalisClient { get; init; }

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Resets the market data.
    /// </summary>
    public void ResetMarketData()
    {
      this.marketBoardWindow.ResetMarketData();
    }

    /// <summary>
    /// Opens the main UI.
    /// </summary>
    public void OpenMainUi()
    {
      this.marketBoardWindow.IsOpen = true;
    }

    /// <summary>
    /// Opens the config UI.
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
        this.PluginInterface.SavePluginConfig(this.Config);

        // Remove windows
        this.windowSystem.RemoveAllWindows();
        this.marketBoardWindow.Dispose();

        // Remove command handlers
        this.CommandManager.RemoveHandler("/pmb");

        // Remove context menu handler
        this.ContextMenu.OnMenuOpened -= this.OnContextMenuOpened;
      }

      this.isDisposed = true;
    }

    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
      if (!this.Config.ContextMenuIntegration)
      {
        return;
      }

      uint itemId;

      if (args.MenuType == ContextMenuType.Inventory)
      {
        itemId = (args.Target as MenuTargetInventory)?.TargetItem?.BaseItemId ?? 0u;
      }
      else
      {
        itemId = this.GetItemIdFromAgent(args.AddonName);

        if (itemId == 0u)
        {
          this.Log.Warning("Failed to get item ID from agent {0}. Attempting hovered item.", args.AddonName ?? "null");
          itemId = (uint)this.GameGui.HoveredItem % 500000;
        }
      }

      if (itemId == 0u)
      {
        this.Log.Warning("Failed to get item ID");
        return;
      }

      var item = this.DataManager.Excel.GetSheet<Item>().GetRowOrDefault(itemId);

      if (!item.HasValue)
      {
        this.Log.Warning("Failed to get item data for item ID {0}", itemId);
        return;
      }

      args.AddMenuItem(new MenuItem
      {
        Name = "Search in Market Board",
        OnClicked = this.GetMenuItemClickedHandler(itemId),
        Prefix = SeIconChar.BoxedLetterM,
        PrefixColor = 48,
        IsEnabled = !item.Value.IsUntradable,
      });
    }

    private unsafe uint GetItemIdFromAgent(string? addonName)
    {
      var itemId = addonName switch
      {
        "ChatLog" => AgentChatLog.Instance()->ContextItemId,
        "GatheringNote" => *(uint*)((IntPtr)AgentGatheringNote.Instance() + 0xA0),
        "GrandCompanySupplyList" => *(uint*)((IntPtr)AgentGrandCompanySupply.Instance() + 0x54),
        "ItemSearch" => (uint)AgentContext.Instance()->UpdateCheckerParam,
        "RecipeNote" => AgentRecipeNote.Instance()->ContextMenuResultItemId,
        _ => 0u,
      };

      return itemId % 500000;
    }

    private Action<IMenuItemClickedArgs> GetMenuItemClickedHandler(uint itemId)
    {
      return (IMenuItemClickedArgs args) =>
      {
        try
        {
          this.marketBoardWindow.IsOpen = true;
          this.marketBoardWindow.ChangeSelectedItem(itemId);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
          this.Log.Error(ex, "Failed on context menu for itemId" + itemId);
        }
      };
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
