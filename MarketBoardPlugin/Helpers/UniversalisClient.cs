// <copyright file="UniversalisClient.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;

  using MarketBoardPlugin.Models.Universalis;

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
    /// <param name="historyCount">Number of entries to fetch from the history.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The market data.</returns>
    public static async Task<MarketDataResponse> GetMarketData(uint itemId, string worldName, int historyCount, CancellationToken cancellationToken)
    {
      var uriBuilder = new UriBuilder($"https://universalis.app/api/{worldName}/{itemId}?entries={historyCount}");

      cancellationToken.ThrowIfCancellationRequested();

      using var client = new HttpClient();
      var res = await client
        .GetStreamAsync(uriBuilder.Uri, cancellationToken)
        .ConfigureAwait(false);

      cancellationToken.ThrowIfCancellationRequested();

      var parsedRes = await JsonSerializer
        .DeserializeAsync<MarketDataResponse>(res, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
      if (parsedRes != null)
      {
        parsedRes.FetchTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      }

      return parsedRes;
    }
  }
}
