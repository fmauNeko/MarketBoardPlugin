// <copyright file="MarketDataResponse.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  /// <summary>
  /// A model representing a market data response from Universalis.
  /// </summary>
  public class MarketDataResponse
  {
    /// <summary>
    /// Gets or sets the name of the datacenter.
    /// </summary>
    [JsonProperty("dcName")]
    public string DcName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonProperty("itemID")]
    public long ItemId { get; set; }

    /// <summary>
    /// Gets or sets the last upload time.
    /// </summary>
    [JsonProperty("lastUploadTime")]
    public long LastUploadTime { get; set; }

    /// <summary>
    /// Gets the listings.
    /// </summary>
    [JsonProperty("listings")]
    public List<MarketDataListing> Listings { get; }

    /// <summary>
    /// Gets the recent history.
    /// </summary>
    [JsonProperty("recentHistory")]
    public List<MarketDataRecentHistory> RecentHistory { get; }

    /// <summary>
    /// Gets or sets the average price.
    /// </summary>
    [JsonProperty("averagePrice")]
    public double AveragePrice { get; set; }

    /// <summary>
    /// Gets or sets the average price of the NQ items.
    /// </summary>
    [JsonProperty("averagePriceNQ")]
    public double AveragePriceNq { get; set; }

    /// <summary>
    /// Gets or sets the average price of the HQ items.
    /// </summary>
    [JsonProperty("averagePriceHQ")]
    public double AveragePriceHq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity.
    /// </summary>
    [JsonProperty("saleVelocity")]
    public double SaleVelocity { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the NQ items.
    /// </summary>
    [JsonProperty("saleVelocityNQ")]
    public double SaleVelocityNq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the HQ items.
    /// </summary>
    [JsonProperty("saleVelocityHQ")]
    public double SaleVelocityHq { get; set; }

    /// <summary>
    /// Gets the stack size histogram.
    /// </summary>
    [JsonProperty("stackSizeHistogram")]
    public Dictionary<string, long> StackSizeHistogram { get; }

    /// <summary>
    /// Gets the stack size histogram of the NQ items.
    /// </summary>
    [JsonProperty("stackSizeHistogramNQ")]
    public Dictionary<string, long> StackSizeHistogramNq { get; }

    /// <summary>
    /// Gets the stack size histogram of the HQ items.
    /// </summary>
    [JsonProperty("stackSizeHistogramHQ")]
    public Dictionary<string, long> StackSizeHistogramHq { get; }
  }
}
