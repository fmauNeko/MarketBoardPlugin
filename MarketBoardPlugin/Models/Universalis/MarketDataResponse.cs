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
    /// Gets or sets the name of the datacenter.
    /// </summary>
    [JsonPropertyName("dcName")]
    public required string DcName { get; set; }

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
    [JsonPropertyName("saleVelocity")]
    public double SaleVelocity { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the NQ items.
    /// </summary>
    [JsonPropertyName("saleVelocityNQ")]
    public double SaleVelocityNq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the HQ items.
    /// </summary>
    [JsonPropertyName("saleVelocityHQ")]
    public double SaleVelocityHq { get; set; }

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
  }
}
