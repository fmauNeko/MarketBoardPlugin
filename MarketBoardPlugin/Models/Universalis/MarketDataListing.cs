// <copyright file="MarketDataListing.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  /// <summary>
  /// A model representing a market data listing from Universalis.
  /// </summary>
  public class MarketDataListing
  {
    /// <summary>
    /// Gets or sets the ID of the creator.
    /// </summary>
    [JsonProperty("creatorID")]
    public string CreatorId { get; set; }

    /// <summary>
    /// Gets or sets the name of the creator.
    /// </summary>
    [JsonProperty("creatorName")]
    public string CreatorName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items are HQ.
    /// </summary>
    [JsonProperty("hq")]
    public bool Hq { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items are crafted.
    /// </summary>
    [JsonProperty("isCrafted")]
    public bool IsCrafted { get; set; }

    /// <summary>
    /// Gets or sets the last review time.
    /// </summary>
    [JsonProperty("lastReviewTime")]
    public long LastReviewTime { get; set; }

    /// <summary>
    /// Gets or sets the ID of the listing.
    /// </summary>
    [JsonProperty("listingID")]
    public string ListingId { get; set; }

    /// <summary>
    /// Gets the list of the materias.
    /// </summary>
    [JsonProperty("materia")]
    public List<object> Materia { get; } = new List<object>();

    /// <summary>
    /// Gets or sets a value indicating whether the item is on a mannequin.
    /// </summary>
    [JsonProperty("onMannequin")]
    public bool OnMannequin { get; set; }

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
    /// Gets or sets the city of the retainer.
    /// </summary>
    [JsonProperty("retainerCity")]
    public long RetainerCity { get; set; }

    /// <summary>
    /// Gets or sets the ID of the retainer.
    /// </summary>
    [JsonProperty("retainerID")]
    public string RetainerId { get; set; }

    /// <summary>
    /// Gets or sets the retainer's name.
    /// </summary>
    [JsonProperty("retainerName")]
    public string RetainerName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the seller.
    /// </summary>
    [JsonProperty("sellerID")]
    public string SellerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the stain.
    /// </summary>
    [JsonProperty("stainID")]
    public long StainId { get; set; }

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
