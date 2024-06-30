// <copyright file="UniversalisClient.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.IO;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;

  using MarketBoardPlugin.Models.Universalis;

  /// <summary>
  /// Universalis API Client.
  /// </summary>
  /// <remarks>
  /// Initializes a new instance of the <see cref="UniversalisClient"/> class.
  /// </remarks>
  /// <param name="plugin">The plugin instance.</param>
  public class UniversalisClient(MBPlugin plugin)
  {
    private readonly MBPlugin plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

    /// <summary>
    /// Retrieves market data for a specific item from the Universalis API.
    /// </summary>
    /// <param name="itemId">The ID of the item to retrieve market data for.</param>
    /// <param name="worldName">The name of the world to retrieve market data from.</param>
    /// <param name="historyCount">The number of historical entries to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="MarketDataResponse"/> object containing the retrieved market data, or null if the operation fails.</returns>
    public async Task<MarketDataResponse> GetMarketData(uint itemId, string worldName, int historyCount, CancellationToken cancellationToken)
    {
      var uriBuilder = new UriBuilder($"https://universalis.app/api/{worldName}/{itemId}?entries={historyCount}");

      cancellationToken.ThrowIfCancellationRequested();

      using var client = new HttpClient();

      Stream res;

      try
      {
        res = await client
          .GetStreamAsync(uriBuilder.Uri, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (HttpRequestException)
      {
        this.plugin.Log.Warning($"Fai    /// to fetch market data for item {itemId} on world {worldName}.");
        return null;
      }

      cancellationToken.ThrowIfCancellationRequested();

      MarketDataResponse parsedRes;

      try
      {
        parsedRes = await JsonSerializer
          .DeserializeAsync<MarketDataResponse>(res, cancellationToken: cancellationToken)
          .ConfigureAwait(false);
      }
      catch (JsonException)
      {
        this.plugin.Log.Warning($"Failed to parse market data for item {itemId} on world {worldName}.");
        return null;
      }

      if (parsedRes != null)
      {
        parsedRes.FetchTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      }

      return parsedRes;
    }
  }
}
