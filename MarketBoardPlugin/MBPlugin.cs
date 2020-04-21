// <copyright file="MBPlugin.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Net.Http;
  using System.Threading.Tasks;
  using System.Web;

  using Dalamud.Game.Command;
  using Dalamud.Plugin;

  using Lumina.Excel.GeneratedSheets;

  using MarketBoardPlugin.GUI;
  using MarketBoardPlugin.Models.Universalis;
  using MarketBoardPlugin.Models.XIVAPI;

  using Newtonsoft.Json;

  /// <summary>
  /// The entry point of the plugin.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Plugin entry point")]
  public class MBPlugin : IDalamudPlugin
  {
    private bool isDisposed;

    private bool isImguiMarketBoardWindowOpen = true;

    private MarketBoardWindow marketBoardWindow;

    private DalamudPluginInterface pluginInterface;

    /// <inheritdoc/>
    public string Name => "Market Board plugin";

    /// <inheritdoc/>
    public void Initialize(DalamudPluginInterface pluginInterface)
    {
      this.pluginInterface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface));

      this.marketBoardWindow = new MarketBoardWindow(this.pluginInterface);

      // Set up command handlers
      pluginInterface.CommandManager.AddHandler("/mb", new CommandInfo(this.OnOpenMarketBoardCommand)
      {
        HelpMessage = "Open the market board window.",
      });

      pluginInterface.CommandManager.AddHandler("/mbsearch", new CommandInfo(this.OnMarketBoardSearch)
      {
        HelpMessage =
          "Query market board information for an item by name or link. Usage: /mb <name of item> [hq] +[world] [- for cheapest on DC] OR /mb <item link> [world] [- for cheapest on DC]",
      });

      pluginInterface.UiBuilder.OnBuildUi += this.BuildMarketBoardUi;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">A value indicating whether we are disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (this.isDisposed)
      {
        return;
      }

      if (disposing)
      {
        // Remove command handlers
        this.pluginInterface.CommandManager.RemoveHandler("/mb");
        this.pluginInterface.CommandManager.RemoveHandler("/mbsearch");
        this.pluginInterface.Dispose();
        this.marketBoardWindow.Dispose();
      }

      this.isDisposed = true;
    }

    private static async Task<List<ItemSearchResult>> SearchItem(string query)
    {
      var uriBuilder = new UriBuilder("https://xivapi.com/search/")
      {
        Query = HttpUtility.ParseQueryString("indexes=Item&string=" + query).ToString(),
      };

      using var client = new HttpClient();
      var res = await client.GetStringAsync(uriBuilder.Uri).ConfigureAwait(false);
      var parsedRes = JsonConvert.DeserializeObject<ItemSearchResponse>(res);

      return parsedRes.Results;
    }

    private static async Task<MarketDataResponse> GetMarketInfo(int itemId, string worldName)
    {
      var uriBuilder = new UriBuilder($"https://universalis.app/api/{worldName}/{itemId}");

      using var client = new HttpClient();
      var res = await client.GetStringAsync(uriBuilder.Uri).ConfigureAwait(false);
      var parsedRes = JsonConvert.DeserializeObject<MarketDataResponse>(res);

      return parsedRes;
    }

    private void OnOpenMarketBoardCommand(string command, string arguments)
    {
      this.isImguiMarketBoardWindowOpen = true;
    }

    private void OnMarketBoardSearch(string command, string arguments)
    {
      if (string.IsNullOrEmpty(arguments))
      {
        this.pluginInterface.Framework.Gui.Chat.PrintError("No item specified.");
        return;
      }

      this.pluginInterface.Framework.Gui.Chat.Print("Searching for market board data...");
      Task.Run(() =>
      {
        var world = this.pluginInterface.ClientState.LocalPlayer.CurrentWorld.GameData.Name;
        var cheapest = false;

        if (this.pluginInterface.Framework.Gui.Chat.LastLinkedItemId != 0 && arguments.Contains("<item>"))
        {
          if (arguments != "<item>")
          {
            world = arguments.Replace("<item>", string.Empty).Replace(" ", string.Empty);
          }

          if (arguments.EndsWith("-", StringComparison.InvariantCulture))
          {
            cheapest = true;
          }

          Task.Run(() => this.SendItemInfo(this.pluginInterface.Framework.Gui.Chat.LastLinkedItemId, (this.pluginInterface.Framework.Gui.Chat.LastLinkedItemFlags & 1) == 1, world, cheapest));

          return;
        }

        var isHq = false;
        var parts = arguments.Split();

        if (parts.Contains("hq"))
        {
          isHq = true;
          parts = parts.Where(x => x != "hq").ToArray();
        }

        if (parts[parts.Length - 1].Contains("+"))
        {
          world = parts[parts.Length - 1].Replace("+", string.Empty);
          parts = parts.Take(parts.Length - 1).ToArray();
        }

        if (parts[parts.Length - 1] == "-")
        {
          cheapest = true;
          parts = parts.Take(parts.Length - 1).ToArray();
        }

        var searchTerm = string.Join(" ", parts);

        var candidates = SearchItem(searchTerm).GetAwaiter().GetResult();

        if (candidates.Count == 0)
        {
          this.pluginInterface.Framework.Gui.Chat.Print("No items found using that name.");
          return;
        }

        this.SendItemInfo((int)candidates[0].Id, isHq, world, cheapest, candidates[0].Name);
      });
    }

    private void BuildMarketBoardUi()
    {
      if (this.isImguiMarketBoardWindowOpen)
      {
        this.isImguiMarketBoardWindowOpen = this.marketBoardWindow != null && this.marketBoardWindow.Draw();
      }
    }

    private void SendItemInfo(int itemId, bool hq, string worldName, bool cheapest = false, string fancyItemName = "")
    {
      try
      {
        MarketDataResponse mbInfo;
        var displayedWorldName = worldName;

        if (cheapest)
        {
          var dc = this.GetDcByWorldName(worldName);

          mbInfo = GetMarketInfo(itemId, dc.Name).GetAwaiter().GetResult();

          displayedWorldName = mbInfo.Listings.Select(listing => (listing.PricePerUnit, listing)).Min().listing
            .WorldName;
        }
        else
        {
          mbInfo = GetMarketInfo(itemId, worldName).GetAwaiter().GetResult();
        }

        var history = mbInfo.RecentHistory.OrderByDescending(h => h.Timestamp).ToList();
        var listings = mbInfo.Listings.OrderBy(l => l.PricePerUnit).ToList();

        if (hq)
        {
          history = history.Where(h => h.Hq).ToList();
          listings = listings.Where(l => l.Hq).ToList();
        }

        this.pluginInterface.Framework.Gui.Chat.Print(
          $"{(cheapest ? "Cheapest result" : "Result")} {(!string.IsNullOrEmpty(fancyItemName) ? $" for \"{fancyItemName}\"{(hq ? "(HQ)" : string.Empty)}" : string.Empty)} on {displayedWorldName}:");
        this.pluginInterface.Framework.Gui.Chat.Print(history.Count == 0
          ? "No recent sales for this item."
          : $"Last sale:\n    {DateTimeOffset.FromUnixTimeSeconds(history.First().Timestamp):R}\n    {history.First().PricePerUnit:N0} /u, {history.First().Quantity} units");

        this.pluginInterface.Framework.Gui.Chat.Print(listings.Count == 0
          ? "No current offerings for this item."
          : $"Current lowest offering:\n    {DateTimeOffset.FromUnixTimeSeconds(listings.First().LastReviewTime):R}\n    {listings.First().PricePerUnit:N0} /u, {listings.First().Quantity} units");
      }
      catch (Exception e)
      {
        this.pluginInterface.Framework.Gui.Chat.PrintError("An error occured when getting market board data.");
        PluginLog.LogError(e, "Could not get market board data.");

        throw e.GetBaseException();
      }
    }

    private WorldDCGroupType GetDcByWorldName(string worldName)
    {
      var worldSheet = this.pluginInterface.Data.GetExcelSheet<World>();
      var worldDcGroupTypeSheet = this.pluginInterface.Data.GetExcelSheet<WorldDCGroupType>();

      var world = worldSheet.GetRows().First(w => w.Name == worldName);
      return worldDcGroupTypeSheet.GetRow(world.DataCenter);
    }
  }
}
