// <copyright file="MarketBoardConfigWindow.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Numerics;
  using Dalamud.Interface.Windowing;
  using ImGuiNET;
  using OtterGui;

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
      : base("Market Board Config")
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

      this.Checkbox("Hide Ko-Fi button", "Toggles whether the Ko-Fi button should be hidden", this.Plugin.Config.KofiHidden, (v) => this.Plugin.Config.KofiHidden = v);

      var itemRefreshTimeout = this.Plugin.Config.ItemRefreshTimeout;
      ImGui.Text("Item buffer Timeout (ms) :");
      ImGui.InputInt("###refreshTimeout", ref itemRefreshTimeout);
      if (this.Plugin.Config.ItemRefreshTimeout != itemRefreshTimeout)
      {
        this.Plugin.Config.ItemRefreshTimeout = itemRefreshTimeout;
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
      }
    }

    private void Checkbox(string label, string description, bool oldValue, Action<bool> setter)
    {
      if (ImGuiUtil.Checkbox(label, description, oldValue, setter))
      {
        this.Plugin.PluginInterface.SavePluginConfig(this.Plugin.Config);
      }
    }
  }
}
