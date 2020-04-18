// <copyright file="MarketDataRecentHistory.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using Newtonsoft.Json;

  public class MarketDataRecentHistory
  {
    [JsonProperty("buyerName")]
    public string BuyerName { get; set; }

    [JsonProperty("hq")]
    public bool Hq { get; set; }

    [JsonProperty("pricePerUnit")]
    public long PricePerUnit { get; set; }

    [JsonProperty("quantity")]
    public long Quantity { get; set; }

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("total")]
    public long Total { get; set; }

    [JsonProperty("worldName")]
    public string WorldName { get; set; }
  }
}
