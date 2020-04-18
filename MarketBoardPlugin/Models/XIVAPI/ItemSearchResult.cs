// <copyright file="ItemSearchResult.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.XIVAPI
{
  using System.Diagnostics.CodeAnalysis;

  using Newtonsoft.Json;

  public class ItemSearchResult
  {
    [JsonProperty("ID")]
    public long Id { get; set; }

    [JsonProperty("Icon")]
    public string Icon { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Url")]
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Not an URI")]
    public string Url { get; set; }

    [JsonProperty("UrlType")]
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Not an URI")]
    public string UrlType { get; set; }

    [JsonProperty("_")]
    public string Type { get; set; }

    [JsonProperty("_Score")]
    public long Score { get; set; }
  }
}
