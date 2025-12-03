// <copyright file="MarketBoardConfigWindow.cs" company="MTVirux">
// Copyright (c) MTVirux. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Linq;
  using System.Numerics;
  using Dalamud.Bindings.ImGui;
  using Dalamud.Interface.Windowing;
  using MarketBoardPlugin.Helpers;

  /// <summary>
  /// The market board config window.
  /// </summary>
  public class MarketBoardConfigWindow : Window
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardConfigWindow"/> class.
    /// </summary>
    /// <param name="plugin">The <see cref="MBPlugin"/>.</param>
    public MarketBoardConfigWindow(MBPlugin plugin)
      : base("Market Terror Config")
    {
      this.Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize;
      this.Size = new Vector2(0, 0);

      this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    }

    private MBPlugin Plugin { get; init; }

    /// <inheritdoc/>
    public override void Draw()
    {
      this.Checkbox("Context menu integration", "Toggles whether context menu integration is enabled", this.Plugin.Config.ContextMenuIntegration, (v) => this.Plugin.Config.ContextMenuIntegration = v);

      this.Checkbox("Gil Icon Shown", "Toggles whether the Gil icon is shown", this.Plugin.Config.PriceIconShown, (v) => this.Plugin.Config.PriceIconShown = v);

      this.Checkbox("No Gil Sales Tax", "Toggles whether the Gil Sales Tax is included", this.Plugin.Config.NoGilSalesTax, (v) =>
      {
        this.Plugin.Config.NoGilSalesTax = v;
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
        this.Plugin.ResetMarketData();
      });

      this.Checkbox("Disable Recent History", "Toggles whether the recent history is disabled", this.Plugin.Config.RecentHistoryDisabled, (v) => this.Plugin.Config.RecentHistoryDisabled = v);

      this.Checkbox("Watch for hovered item", "Automatically select the item hovered in any of the in-game inventory window after 1 second.", this.Plugin.Config.WatchForHovered, (v) => this.Plugin.Config.WatchForHovered = v);

      // Auto-teleport to world setting (only enabled if Lifestream is installed)
      var lifestreamInstalled = this.Plugin.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "Lifestream");
      if (!lifestreamInstalled)
      {
        ImGui.BeginDisabled();
      }

      this.Checkbox("Auto-teleport to world", lifestreamInstalled ? "Automatically teleport to the listing's world when clicked (requires Lifestream plugin)" : "Automatically teleport to the listing's world when clicked (Lifestream plugin not installed)", this.Plugin.Config.AutoTeleportToWorld, (v) => this.Plugin.Config.AutoTeleportToWorld = v);

      if (!lifestreamInstalled)
      {
        ImGui.EndDisabled();
      }


      this.Checkbox("Clipboard notifications", "Show a chat message when something is copied to the clipboard", this.Plugin.Config.ClipboardNotificationsEnabled, (v) => this.Plugin.Config.ClipboardNotificationsEnabled = v);
      
      this.Checkbox("Hide SeaOfTerror Repo button", "Toggles whether the SeaOfTerror Repo button should be hidden", this.Plugin.Config.KofiHidden, (v) => this.Plugin.Config.KofiHidden = v);

      var itemRefreshTimeout = this.Plugin.Config.ItemRefreshTimeout;
      ImGui.Text("Item buffer Timeout (ms) :");
      ImGui.InputInt("###refreshTimeout", ref itemRefreshTimeout);
      if (this.Plugin.Config.ItemRefreshTimeout != itemRefreshTimeout)
      {
        this.Plugin.Config.ItemRefreshTimeout = itemRefreshTimeout;
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
      }

      var listingCount = this.Plugin.Config.ListingCount;
      ImGui.Text("Listing count :");
      ImGui.InputInt("###listingCount", ref listingCount);
      if (this.Plugin.Config.ListingCount != listingCount)
      {
        this.Plugin.Config.ListingCount = listingCount;
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
      }

      var historyCount = this.Plugin.Config.HistoryCount;
      ImGui.Text("History count :");
      ImGui.InputInt("###historyCount", ref historyCount);
      if (this.Plugin.Config.HistoryCount != historyCount)
      {
        this.Plugin.Config.HistoryCount = historyCount;
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
      }
    }

    private void Checkbox(string label, string description, bool oldValue, Action<bool> setter)
    {
      if (Utilities.Checkbox(label, description, oldValue, setter))
      {
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
      }
    }
  }
}
