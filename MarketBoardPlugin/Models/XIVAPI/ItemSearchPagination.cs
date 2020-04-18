// <copyright file="ItemSearchPagination.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.XIVAPI
{
  using Newtonsoft.Json;

  public class ItemSearchPagination
  {
    [JsonProperty("Page")]
    public long Page { get; set; }

    [JsonProperty("PageNext")]
    public object PageNext { get; set; }

    [JsonProperty("PagePrev")]
    public object PagePrev { get; set; }

    [JsonProperty("PageTotal")]
    public long PageTotal { get; set; }

    [JsonProperty("Results")]
    public long Results { get; set; }

    [JsonProperty("ResultsPerPage")]
    public long ResultsPerPage { get; set; }

    [JsonProperty("ResultsTotal")]
    public long ResultsTotal { get; set; }
  }
}
