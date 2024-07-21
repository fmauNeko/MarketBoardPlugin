// <copyright file="UniversalisClient.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using MarketBoardPlugin.Models.Universalis;
  using Polly;

  /// <summary>
  /// Universalis API Client.
  /// </summary>
  /// <remarks>
  /// Initializes a new instance of the <see cref="UniversalisClient"/> class.
  /// </remarks>
  public class UniversalisClient : IDisposable
  {
    private readonly MBPlugin plugin;

    private readonly HttpClient client;

    private readonly ResiliencePipeline resiliencePipeline;

    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniversalisClient"/> class.
    /// </summary>
    /// <param name="plugin">The <see cref="MBPlugin"/> instance.</param>
    /// <exception cref="ArgumentNullException">One of the required arguments is null.</exception>
    public UniversalisClient(MBPlugin plugin)
    {
      ArgumentNullException.ThrowIfNull(plugin);

      this.plugin = plugin;

      this.client = new HttpClient
      {
        BaseAddress = new Uri("https://universalis.app/api/"),
      };
      this.client.DefaultRequestHeaders.UserAgent.ParseAdd($"MarketBoardPlugin/{this.plugin.PluginInterface.Manifest.AssemblyVersion}");

      this.resiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new()
        {
          BackoffType = DelayBackoffType.Exponential,
          MaxRetryAttempts = 3,
          ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
          UseJitter = true,
        })
        .Build();
    }

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
      try
      {
        using var content = await this.resiliencePipeline.ExecuteAsync(
            async (ct) =>
              await this.client.GetStreamAsync(new Uri($"{worldName}/{itemId}?entries={historyCount}", UriKind.Relative), ct).ConfigureAwait(false),
            cancellationToken)
          .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var parsedRes = await JsonSerializer
          .DeserializeAsync<MarketDataResponse>(content, cancellationToken: cancellationToken)
          .ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to parse market data for item {itemId} on world {worldName}.");

        parsedRes.FetchTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        return parsedRes;
      }
      catch (HttpRequestException ex)
      {
        this.plugin.Log.Warning(ex, $"Failed to fetch market data for item {itemId} on world {worldName}.");
        throw;
      }
      catch (JsonException ex)
      {
        this.plugin.Log.Warning(ex, $"Failed to parse market data for item {itemId} on world {worldName}.");
        throw;
      }
    }

    /// <summary>
    /// Retrieves the collection of data centers.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="DataCenter"/> objects containing the data centers.</returns>
    public async Task<ICollection<DataCenter>> GetDataCenters(CancellationToken cancellationToken)
    {
      try
      {
        using var content = await this.resiliencePipeline.ExecuteAsync(
            async (ct) =>
              await this.client.GetStreamAsync(new Uri("data-centers", UriKind.Relative), ct).ConfigureAwait(false),
            cancellationToken)
          .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return await JsonSerializer
          .DeserializeAsync<ICollection<DataCenter>>(content, cancellationToken: cancellationToken)
          .ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to parse data centers.");
      }
      catch (HttpRequestException ex)
      {
        this.plugin.Log.Warning(ex, "Failed to fetch data centers.");
        throw;
      }
      catch (JsonException ex)
      {
        this.plugin.Log.Warning(ex, "Failed to parse data centers.");
        throw;
      }
    }

    /// <summary>
    /// Checks if the Universalis API is up and running.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if the Universalis API is up; otherwise, <c>false</c>.</returns>
    public async Task<bool> CheckStatus(CancellationToken cancellationToken)
    {
      try
      {
        await this.GetDataCenters(cancellationToken).ConfigureAwait(false);
      }
      catch (HttpRequestException ex)
      {
        this.plugin.Log.Warning(ex, "Universalis seems down.");
        return false;
      }
      catch (JsonException ex)
      {
        this.plugin.Log.Warning(ex, "Universalis seems down.");
        return false;
      }

      this.plugin.Log.Verbose("Universalis seems up.");
      return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
      if (!this.disposedValue)
      {
        if (disposing)
        {
          this.client.Dispose();
        }

        this.disposedValue = true;
      }
    }
  }
}
