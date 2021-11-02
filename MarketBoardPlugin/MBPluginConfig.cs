// <copyright file="MBPluginConfig.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System.Collections.Generic;
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
    /// Gets or sets a value indicating whether cross world was selected.
    /// </summary>
    public bool CrossWorld { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 'Watch for hovered item' is enabled.
    /// </summary>
    public bool WatchForHovered { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of previously viewed items.
    /// </summary>
    public List<uint> History { get; set; } = new List<uint>();

    /// <summary>
    /// Gets or sets a value indicating whether the 'Search with Market Board Plugin' is added to game context menus.
    /// </summary>
    public bool ContextMenuIntegration { get; set; } = true;
  }
}
