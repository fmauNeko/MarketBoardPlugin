// <copyright file="MarketDataResponse.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Text.Json.Serialization;

  /// <summary>
  /// A model representing a market data response from Universalis.
  /// </summary>
  public class MarketDataResponse
  {
    /// <summary>
    /// Gets or sets the name of the datacenter when querying a datacenter scope.
    /// </summary>
    [JsonPropertyName("dcName")]
    public string? DcName { get; set; }

    /// <summary>
    /// Gets or sets the name of the region when querying a region scope.
    /// </summary>
    [JsonPropertyName("regionName")]
    public string? RegionName { get; set; }

    /// <summary>
    /// Gets or sets the name of the world when querying a single world.
    /// </summary>
    [JsonPropertyName("worldName")]
    public string? WorldName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the world when querying a single world.
    /// </summary>
    [JsonPropertyName("worldID")]
    public int? WorldId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonPropertyName("itemID")]
    public long ItemId { get; set; }

    /// <summary>
    /// Gets or sets the last upload time.
    /// </summary>
    [JsonPropertyName("lastUploadTime")]
    public long LastUploadTime { get; set; }

    /// <summary>
    /// Gets or sets the listings.
    /// </summary>
    [JsonPropertyName("listings")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataListing> Listings { get; set; } = new List<MarketDataListing>();

    /// <summary>
    /// Gets or sets the recent history.
    /// </summary>
    [JsonPropertyName("recentHistory")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataRecentHistory> RecentHistory { get; set; } = new List<MarketDataRecentHistory>();

    /// <summary>
    /// Gets or sets the current weighted average price.
    /// </summary>
    [JsonPropertyName("currentAveragePrice")]
    public double CurrentAveragePrice { get; set; }

    /// <summary>
    /// Gets or sets the current weighted average price of the NQ items.
    /// </summary>
    [JsonPropertyName("currentAveragePriceNQ")]
    public double CurrentAveragePriceNq { get; set; }

    /// <summary>
    /// Gets or sets the current weighted average price of the HQ items.
    /// </summary>
    [JsonPropertyName("currentAveragePriceHQ")]
    public double CurrentAveragePriceHq { get; set; }

    /// <summary>
    /// Gets or sets the average price.
    /// </summary>
    [JsonPropertyName("averagePrice")]
    public double AveragePrice { get; set; }

    /// <summary>
    /// Gets or sets the average price of the NQ items.
    /// </summary>
    [JsonPropertyName("averagePriceNQ")]
    public double AveragePriceNq { get; set; }

    /// <summary>
    /// Gets or sets the average price of the HQ items.
    /// </summary>
    [JsonPropertyName("averagePriceHQ")]
    public double AveragePriceHq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity.
    /// </summary>
    [JsonPropertyName("regularSaleVelocity")]
    public double SaleVelocity { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the NQ items.
    /// </summary>
    [JsonPropertyName("nqSaleVelocity")]
    public double SaleVelocityNq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the HQ items.
    /// </summary>
    [JsonPropertyName("hqSaleVelocity")]
    public double SaleVelocityHq { get; set; }

    /// <summary>
    /// Gets or sets the minimum price.
    /// </summary>
    [JsonPropertyName("minPrice")]
    public long MinPrice { get; set; }

    /// <summary>
    /// Gets or sets the minimum price of the NQ items.
    /// </summary>
    [JsonPropertyName("minPriceNQ")]
    public long MinPriceNq { get; set; }

    /// <summary>
    /// Gets or sets the minimum price of the HQ items.
    /// </summary>
    [JsonPropertyName("minPriceHQ")]
    public long MinPriceHq { get; set; }

    /// <summary>
    /// Gets or sets the maximum price.
    /// </summary>
    [JsonPropertyName("maxPrice")]
    public long MaxPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum price of the NQ items.
    /// </summary>
    [JsonPropertyName("maxPriceNQ")]
    public long MaxPriceNq { get; set; }

    /// <summary>
    /// Gets or sets the maximum price of the HQ items.
    /// </summary>
    [JsonPropertyName("maxPriceHQ")]
    public long MaxPriceHq { get; set; }

    /// <summary>
    /// Gets or sets the count of listings returned.
    /// </summary>
    [JsonPropertyName("listingsCount")]
    public int ListingsCount { get; set; }

    /// <summary>
    /// Gets or sets the count of recent history entries returned.
    /// </summary>
    [JsonPropertyName("recentHistoryCount")]
    public int RecentHistoryCount { get; set; }

    /// <summary>
    /// Gets or sets the number of units currently for sale.
    /// </summary>
    [JsonPropertyName("unitsForSale")]
    public long UnitsForSale { get; set; }

    /// <summary>
    /// Gets or sets the number of units recently sold.
    /// </summary>
    [JsonPropertyName("unitsSold")]
    public long UnitsSold { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dataset contains data.
    /// </summary>
    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }

    /// <summary>
    /// Gets or sets the fetch timestamp.
    /// </summary>
    public long FetchTimestamp { get; set; }

    /// <summary>
    /// Gets the stack size histogram.
    /// </summary>
    [JsonPropertyName("stackSizeHistogram")]
    public Dictionary<string, long> StackSizeHistogram { get; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets the stack size histogram of the NQ items.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramNQ")]
    public Dictionary<string, long> StackSizeHistogramNq { get; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets the stack size histogram of the HQ items.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramHQ")]
    public Dictionary<string, long> StackSizeHistogramHq { get; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets a map of world IDs to their last upload time when querying region or datacenter scopes.
    /// </summary>
    [JsonPropertyName("worldUploadTimes")]
    public Dictionary<string, long> WorldUploadTimes { get; } = new Dictionary<string, long>();
  }
}
