// <copyright file="MarketDataListing.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  public class MarketDataListing
  {
    [JsonProperty("creatorID")]
    public string CreatorId { get; set; }

    [JsonProperty("creatorName")]
    public string CreatorName { get; set; }

    [JsonProperty("hq")]
    public bool Hq { get; set; }

    [JsonProperty("isCrafted")]
    public bool IsCrafted { get; set; }

    [JsonProperty("lastReviewTime")]
    public long LastReviewTime { get; set; }

    [JsonProperty("listingID")]
    public string ListingId { get; set; }

    [JsonProperty("materia")]
    public List<object> Materia { get; }

    [JsonProperty("onMannequin")]
    public bool OnMannequin { get; set; }

    [JsonProperty("pricePerUnit")]
    public long PricePerUnit { get; set; }

    [JsonProperty("quantity")]
    public long Quantity { get; set; }

    [JsonProperty("retainerCity")]
    public long RetainerCity { get; set; }

    [JsonProperty("retainerID")]
    public string RetainerId { get; set; }

    [JsonProperty("retainerName")]
    public string RetainerName { get; set; }

    [JsonProperty("sellerID")]
    public string SellerId { get; set; }

    [JsonProperty("stainID")]
    public long StainId { get; set; }

    [JsonProperty("total")]
    public long Total { get; set; }

    [JsonProperty("worldName")]
    public string WorldName { get; set; }
  }
}
