// <copyright file="UniversalisClient.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;

  using MarketBoardPlugin.Models.Universalis;

  using Newtonsoft.Json;

  /// <summary>
  /// Universalis API Client.
  /// </summary>
  public static class UniversalisClient
  {
    /// <summary>
    /// Gets market data of an item for a specific world.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="worldName">The world's name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The market data.</returns>
    public static async Task<MarketDataResponse> GetMarketData(int itemId, string worldName, CancellationToken cancellationToken)
    {
      var uriBuilder = new UriBuilder($"https://universalis.app/api/{worldName}/{itemId}");

      cancellationToken.ThrowIfCancellationRequested();

      using var client = new HttpClient();
      var res = await client
        .GetStringAsync(uriBuilder.Uri)
        .ConfigureAwait(false);

      cancellationToken.ThrowIfCancellationRequested();

      var parsedRes = JsonConvert.DeserializeObject<MarketDataResponse>(res);

      return parsedRes;
    }
  }
}
