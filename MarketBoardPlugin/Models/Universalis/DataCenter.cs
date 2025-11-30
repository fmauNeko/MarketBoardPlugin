// <copyright file="DataCenter.cs" company="MTVirux">
// Copyright (c) MTVirux. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Text.Json.Serialization;

  /// <summary>
  /// A model representing a data center from Universalis.
  /// </summary>
  public partial class DataCenter
  {
    /// <summary>
    /// Gets or sets the name of the data center.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the region of the data center.
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of worlds in the data center.
    /// </summary>
    [JsonPropertyName("worlds")]
    public IReadOnlyCollection<uint> Worlds { get; set; } = [];
  }
}
