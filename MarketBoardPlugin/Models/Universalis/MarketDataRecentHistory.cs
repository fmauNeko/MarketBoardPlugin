// <copyright file="MarketDataRecentHistory.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using Newtonsoft.Json;

  /// <summary>
  /// A model representing a market data recent history from Universalis.
  /// </summary>
  public class MarketDataRecentHistory
  {
    /// <summary>
    /// Gets or sets the name of the buyer.
    /// </summary>
    [JsonProperty("buyerName")]
    public string BuyerName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items are HQ.
    /// </summary>
    [JsonProperty("hq")]
    public bool Hq { get; set; }

    /// <summary>
    /// Gets or sets the price per unit.
    /// </summary>
    [JsonProperty("pricePerUnit")]
    public long PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [JsonProperty("quantity")]
    public long Quantity { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the total.
    /// </summary>
    [JsonProperty("total")]
    public long Total { get; set; }

    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    [JsonProperty("worldName")]
    public string WorldName { get; set; }
  }
}
