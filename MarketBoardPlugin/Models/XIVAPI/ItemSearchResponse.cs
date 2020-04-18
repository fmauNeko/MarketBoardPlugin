// <copyright file="ItemSearchResponse.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.XIVAPI
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  /// <summary>
  /// A model representing an item search response from XIVAPI.
  /// </summary>
  public class ItemSearchResponse
  {
    /// <summary>
    /// Gets or sets the pagination.
    /// </summary>
    [JsonProperty("Pagination")]
    public ItemSearchPagination Pagination { get; set; }

    /// <summary>
    /// Gets the results.
    /// </summary>
    [JsonProperty("Results")]
    public List<ItemSearchResult> Results { get; }

    /// <summary>
    /// Gets or sets the speed in milliseconds.
    /// </summary>
    [JsonProperty("SpeedMs")]
    public long SpeedMs { get; set; }
  }
}
