// <copyright file="UniversalisClient.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Net.Http;
  using System.Threading.Tasks;

  using MarketBoardPlugin.Models.Universalis;

  using Newtonsoft.Json;

  /// <summary>
  /// Universalis API Client.
  /// </summary>
  public static class UniversalisClient
  {
    /// <summary>
    /// Gets market info of an item for a specific world.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="worldName">The world's name.</param>
    /// <returns>The market data.</returns>
    public static async Task<MarketDataResponse> GetMarketInfo(int itemId, string worldName)
    {
      var uriBuilder = new UriBuilder($"https://universalis.app/api/{worldName}/{itemId}");

      using var client = new HttpClient();
      var res = await client.GetStringAsync(uriBuilder.Uri).ConfigureAwait(false);
      var parsedRes = JsonConvert.DeserializeObject<MarketDataResponse>(res);

      return parsedRes;
    }
  }
}
