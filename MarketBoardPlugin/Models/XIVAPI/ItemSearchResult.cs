// <copyright file="ItemSearchResult.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.XIVAPI
{
  using System.Diagnostics.CodeAnalysis;

  using Newtonsoft.Json;

  /// <summary>
  /// A model representing an item search result from XIVAPI.
  /// </summary>
  public class ItemSearchResult
  {
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    [JsonProperty("ID")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    [JsonProperty("Icon")]
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [JsonProperty("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    [JsonProperty("Url")]
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Not an URI")]
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the URL type.
    /// </summary>
    [JsonProperty("UrlType")]
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Not an URI")]
    public string UrlType { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    [JsonProperty("_")]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the score.
    /// </summary>
    [JsonProperty("_Score")]
    public long Score { get; set; }
  }
}
