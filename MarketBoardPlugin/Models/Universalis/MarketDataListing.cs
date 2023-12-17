// <copyright file="MarketDataListing.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.Universalis
{
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Text.Json.Serialization;

  /// <summary>
  /// A model representing a market data listing from Universalis.
  /// </summary>
  public class MarketDataListing
  {
    /// <summary>
    /// Gets or sets the ID of the creator.
    /// </summary>
    [JsonPropertyName("creatorID")]
    public string CreatorId { get; set; }

    /// <summary>
    /// Gets or sets the name of the creator.
    /// </summary>
    [JsonPropertyName("creatorName")]
    public string CreatorName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items are HQ.
    /// </summary>
    [JsonPropertyName("hq")]
    public bool Hq { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items are crafted.
    /// </summary>
    [JsonPropertyName("isCrafted")]
    public bool IsCrafted { get; set; }

    /// <summary>
    /// Gets or sets the last review time.
    /// </summary>
    [JsonPropertyName("lastReviewTime")]
    public long LastReviewTime { get; set; }

    /// <summary>
    /// Gets or sets the ID of the listing.
    /// </summary>
    [JsonPropertyName("listingID")]
    public string ListingId { get; set; }

    /// <summary>
    /// Gets or sets the list of the materias.
    /// </summary>
    [JsonPropertyName("materia")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<object> Materia { get; set; } = new List<object>();

    /// <summary>
    /// Gets or sets a value indicating whether the item is on a mannequin.
    /// </summary>
    [JsonPropertyName("onMannequin")]
    public bool OnMannequin { get; set; }

    /// <summary>
    /// Gets or sets the price per unit.
    /// </summary>
    [JsonPropertyName("pricePerUnit")]
    public long PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [JsonPropertyName("quantity")]
    public long Quantity { get; set; }

    /// <summary>
    /// Gets or sets the city of the retainer.
    /// </summary>
    [JsonPropertyName("retainerCity")]
    public long RetainerCity { get; set; }

    /// <summary>
    /// Gets or sets the ID of the retainer.
    /// </summary>
    [JsonPropertyName("retainerID")]
    public string RetainerId { get; set; }

    /// <summary>
    /// Gets or sets the retainer's name.
    /// </summary>
    [JsonPropertyName("retainerName")]
    public string RetainerName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the seller.
    /// </summary>
    [JsonPropertyName("sellerID")]
    public string SellerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the stain.
    /// </summary>
    [JsonPropertyName("stainID")]
    public long StainId { get; set; }

    /// <summary>
    /// Gets or sets the Gil sales tax (GST) to be added to the total price during purchase.
    /// </summary>
    [JsonPropertyName("tax")]
    public long Tax { get; set; }

    /// <summary>
    /// Gets or sets the total.
    /// </summary>
    [JsonPropertyName("total")]
    public long Total { get; set; }

    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    [JsonPropertyName("worldName")]
    public string WorldName { get; set; }
  }
}
