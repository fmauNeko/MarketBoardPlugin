// <copyright file="ItemSearchPagination.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.XIVAPI
{
  using Newtonsoft.Json;

  /// <summary>
  /// A model representing an item search pagination from XIVAPI.
  /// </summary>
  public class ItemSearchPagination
  {
    /// <summary>
    /// Gets or sets the page.
    /// </summary>
    [JsonProperty("Page")]
    public long Page { get; set; }

    /// <summary>
    /// Gets or sets the next page.
    /// </summary>
    [JsonProperty("PageNext")]
    public long PageNext { get; set; }

    /// <summary>
    /// Gets or sets the previous page.
    /// </summary>
    [JsonProperty("PagePrev")]
    public long PagePrev { get; set; }

    /// <summary>
    /// Gets or sets the total count of pages.
    /// </summary>
    [JsonProperty("PageTotal")]
    public long PageTotal { get; set; }

    /// <summary>
    /// Gets or sets the result count.
    /// </summary>
    [JsonProperty("Results")]
    public long Results { get; set; }

    /// <summary>
    /// Gets or sets the result count per page.
    /// </summary>
    [JsonProperty("ResultsPerPage")]
    public long ResultsPerPage { get; set; }

    /// <summary>
    /// Gets or sets the total result count.
    /// </summary>
    [JsonProperty("ResultsTotal")]
    public long ResultsTotal { get; set; }
  }
}
