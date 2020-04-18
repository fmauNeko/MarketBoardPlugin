// <copyright file="MarketDataResponse.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  public class MarketDataResponse
  {
    [JsonProperty("dcName")]
    public string DcName { get; set; }

    [JsonProperty("itemID")]
    public long ItemId { get; set; }

    [JsonProperty("lastUploadTime")]
    public long LastUploadTime { get; set; }

    [JsonProperty("listings")]
    public List<MarketDataListing> Listings { get; }

    [JsonProperty("recentHistory")]
    public List<MarketDataRecentHistory> RecentHistory { get; }

    [JsonProperty("averagePrice")]
    public double AveragePrice { get; set; }

    [JsonProperty("averagePriceNQ")]
    public double AveragePriceNq { get; set; }

    [JsonProperty("averagePriceHQ")]
    public double AveragePriceHq { get; set; }

    [JsonProperty("saleVelocity")]
    public double SaleVelocity { get; set; }

    [JsonProperty("saleVelocityNQ")]
    public double SaleVelocityNq { get; set; }

    [JsonProperty("saleVelocityHQ")]
    public double SaleVelocityHq { get; set; }

    [JsonProperty("stackSizeHistogram")]
    public Dictionary<string, long> StackSizeHistogram { get; }

    [JsonProperty("stackSizeHistogramNQ")]
    public Dictionary<string, long> StackSizeHistogramNq { get; }

    [JsonProperty("stackSizeHistogramHQ")]
    public Dictionary<string, long> StackSizeHistogramHq { get; }
  }
}
