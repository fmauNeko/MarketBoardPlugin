// <copyright file="MBPluginConfig.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using Dalamud.Configuration;

  /// <summary>
  /// Configuration for MBPlugin.
  /// </summary>
  public class MBPluginConfig : IPluginConfiguration
  {
    /// <summary>
    /// Gets or sets the version of the config file.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether cross data center was selected.
    /// </summary>
    public bool CrossDataCenter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cross world was selected.
    /// </summary>
    public bool CrossWorld { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 'Watch for hovered item' is enabled.
    /// </summary>
    public bool WatchForHovered { get; set; } = true;

    /// <summary>
    /// Gets the list of previously viewed items.
    /// </summary>
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "RemoveAll and RemoveRange required")]
    public List<uint> History { get; } = new List<uint>();

    /// <summary>
    /// Gets or sets a value indicating whether the 'Search with Market Board Plugin' is added to game context menus.
    /// </summary>
    public bool ContextMenuIntegration { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether 'PriceIconShown' is enabled.
    /// </summary>
    public bool PriceIconShown { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether 'NoGilSalesTax' is enabled.
    /// </summary>
    public bool NoGilSalesTax { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Ko-Fi button has been hidden.
    /// </summary>
    public bool KofiHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the maximum size of the Market item buffer.
    /// </summary>
    public int MarketBufferSize { get; set; } = 10;

    /// <summary>
    ///  Gets or sets a value indicating the number of ms an item can be cached.
    /// </summary>
    public int ItemRefreshTimeout { get; set; } = 30000;

    /// <summary>
    ///  Gets or sets a value indicating whether the recent history menu is disabled or not.
    /// </summary>
    public bool RecentHistoryDisabled { get; set; }

    /// <summary>
    /// Gets the favorite items.
    /// </summary>
    public IEnumerable<uint> Favorites { get; }
  }
}
