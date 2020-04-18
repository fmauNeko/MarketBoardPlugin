// <copyright file="ItemSearchResultResponse.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.XIVAPI
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  public class ItemSearchResultResponse
  {
    [JsonProperty("Pagination")]
    public ItemSearchPagination Pagination { get; set; }

    [JsonProperty("Results")]
    public List<ItemSearchResult> Results { get; }

    [JsonProperty("SpeedMs")]
    public long SpeedMs { get; set; }
  }
}
