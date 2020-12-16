// <copyright file="MarketBoardWindow.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.GUI
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Numerics;
  using System.Reflection;
  using System.Threading;
  using System.Threading.Tasks;

  using Dalamud.Data.LuminaExtensions;
  using Dalamud.Game.Chat;
  using Dalamud.Game.Internal;
  using Dalamud.Plugin;

  using ImGuiNET;
  using ImGuiScene;

  using Lumina.Excel.GeneratedSheets;

  using MarketBoardPlugin.Extensions;
  using MarketBoardPlugin.Helpers;
  using MarketBoardPlugin.Models.Universalis;

  /// <summary>
  /// The market board window.
  /// </summary>
  public class MarketBoardWindow : IDisposable
  {
    private readonly IEnumerable<Item> items;

    private readonly DalamudPluginInterface pluginInterface;

    private readonly Dictionary<ItemSearchCategory, List<Item>> sortedCategoriesAndItems;

    private readonly List<(string, string)> worldList = new List<(string, string)>();

    private CancellationTokenSource hoveredItemChangeTokenSource;

    private bool isDisposed;

    private bool itemIsBeingHovered;

    private float progressPosition;

    private string searchString = string.Empty;
    private string lastSearchString = string.Empty;

    private Item selectedItem;

    private TextureWrap selectedItemIcon;

    private bool watchingForHoveredItem = true;

    private ulong playerId = 0;

    private int selectedWorld = -1;

    private MarketDataResponse marketData;

    private int selectedListing = -1;

    private int selectedHistory = -1;

    private ImFontPtr fontPtr;

    private bool hasListingsHQColumnWidthBeenSet = false;

    private bool hasHistoryHQColumnWidthBeenSet = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardWindow"/> class.
    /// </summary>
    /// <param name="pluginInterface">The <see cref="DalamudPluginInterface"/>.</param>
    public MarketBoardWindow(DalamudPluginInterface pluginInterface)
    {
      if (pluginInterface == null)
      {
        throw new ArgumentNullException(nameof(pluginInterface));
      }

      this.items = pluginInterface.Data.GetExcelSheet<Item>();
      this.pluginInterface = pluginInterface;
      this.sortedCategoriesAndItems = this.SortCategoriesAndItems();

      pluginInterface.Framework.OnUpdateEvent += this.HandleFrameworkUpdateEvent;
      pluginInterface.Framework.Gui.HoveredItemChanged += this.HandleHoveredItemChange;
      pluginInterface.UiBuilder.OnBuildFonts += this.HandleBuildFonts;

      pluginInterface.UiBuilder.RebuildFonts();

      #if DEBUG
      this.worldList.Add(("Chaos", "Chaos"));
      this.worldList.Add(("Moogle", "Moogle"));
      #endif
    }

    /// <summary>
    /// Gets or sets the current search string.
    /// </summary>
    public string SearchString
    {
      get => this.searchString;
      set => this.searchString = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Market Board window is open or not.
    /// </summary>
    public bool IsOpen { get; set; }

    private List<KeyValuePair<ItemSearchCategory, List<Item>>> enumerableCategoriesAndItems;

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Draws the window.
    /// </summary>
    /// <returns>A value indicating whether the window is open.</returns>
    public bool Draw()
    {
      var windowOpen = true;
      this.enumerableCategoriesAndItems ??= this.sortedCategoriesAndItems.ToList();

      if (this.searchString != this.lastSearchString)
      {
        if (!string.IsNullOrEmpty(this.searchString))
        {
          this.enumerableCategoriesAndItems = this.sortedCategoriesAndItems
            .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(
              kv.Key,
              kv.Value
                .Where(i =>
                  i.Name.ToString().ToUpperInvariant().Contains(this.searchString.ToUpperInvariant()))
                .ToList()))
            .Where(kv => kv.Value.Count > 0)
            .ToList();
        }
        else
        {
          this.enumerableCategoriesAndItems = this.sortedCategoriesAndItems.ToList();
        }

        this.lastSearchString = this.searchString;
      }

      ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

      if (!ImGui.Begin("Market Board", ref windowOpen, ImGuiWindowFlags.NoScrollbar))
      {
        ImGui.End();
        return windowOpen;
      }

      ImGui.BeginChild("itemListColumn", new Vector2(267, 0), true);

      ImGui.SetNextItemWidth(-1);
      ImGuiOverrides.InputTextWithHint("##searchString", "Search for item", ref this.searchString, 256);
      ImGui.Separator();

      ImGui.BeginChild("itemTree", new Vector2(0, -2.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);

      var itemTextSize = ImGui.CalcTextSize(string.Empty);
      foreach (var category in this.enumerableCategoriesAndItems)
      {
        if (ImGui.TreeNode(category.Key.Name + "##cat" + category.Key.RowId))
        {
          ImGui.Unindent(ImGui.GetTreeNodeToLabelSpacing());

          for (var i = 0; i < category.Value.Count; i++)
          {
            if (ImGui.GetCursorPosY() < ImGui.GetScrollY() - itemTextSize.Y)
            {
              // Don't draw items above the scroll region.
              var y = ImGui.GetCursorPosY();
              var sy = ImGui.GetScrollY() - itemTextSize.Y;
              var spacing = itemTextSize.Y + ImGui.GetStyle().ItemSpacing.Y;
              var c = category.Value.Count;
              while (i < c && y < sy)
              {
                y += spacing;
                i++;
              }

              ImGui.SetCursorPosY(y);
              continue;
            }

            if (ImGui.GetCursorPosY() > ImGui.GetScrollY() + ImGui.GetWindowHeight())
            {
              // Don't draw item names below the scroll region
              var remainingItems = category.Value.Count - i;
              var remainingItemsHeight = itemTextSize.Y * remainingItems;
              var remainingGapHeight = ImGui.GetStyle().ItemSpacing.Y * (remainingItems - 1);
              ImGui.Dummy(new Vector2(1, remainingItemsHeight + remainingGapHeight));
              break;
            }

            var item = category.Value[i];
            var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

            if (item.RowId == this.selectedItem?.RowId)
            {
              nodeFlags |= ImGuiTreeNodeFlags.Selected;
            }

            ImGui.TreeNodeEx(item.Name + "##item" + item.RowId, nodeFlags);

            if (ImGui.IsItemClicked())
            {
              this.ChangeSelectedItem(item.RowId);
            }
          }

          ImGui.Indent(ImGui.GetTreeNodeToLabelSpacing());
          ImGui.TreePop();
        }
      }

      ImGui.EndChild();

      ImGui.Checkbox("Watch for hovered item", ref this.watchingForHoveredItem);
      ImGui.SameLine();
      ImGui.TextDisabled("(?)");
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
        ImGui.TextUnformatted("Automatically select the item hovered in any of the in-game inventory window after 1 second.");
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
      }

      if (this.itemIsBeingHovered)
      {
        if (this.progressPosition < 1.0f)
        {
          this.progressPosition += ImGui.GetIO().DeltaTime;
        }
        else
        {
          this.progressPosition = 1.0f;
        }
      }
      else
      {
        this.progressPosition = 0.0f;
      }

      ImGui.ProgressBar(this.progressPosition, new Vector2(-1, 0), string.Empty);

      ImGui.EndChild();
      ImGui.SameLine();
      ImGui.BeginChild("tabColumn", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);

      if (this.selectedItem?.RowId > 0)
      {
        if (this.selectedItemIcon != null)
        {
          ImGui.Image(this.selectedItemIcon.ImGuiHandle, new Vector2(40, 40));
        }
        else
        {
          ImGui.SetCursorPos(new Vector2(40, 40));
        }

        ImGui.PushFont(this.fontPtr);
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetFontSize() / 2.0f) + 19);
        ImGui.Text(this.selectedItem?.Name);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 250);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetFontSize() / 2.0f) - 19);
        ImGui.PopFont();
        ImGui.SetNextItemWidth(250);
        if (ImGui.BeginCombo("##worldCombo", this.selectedWorld > -1 ? this.worldList[this.selectedWorld].Item2 : string.Empty))
        {
          foreach (var world in this.worldList)
          {
            var isSelected = this.selectedWorld == this.worldList.IndexOf(world);
            if (ImGui.Selectable(world.Item2, isSelected))
            {
              this.selectedWorld = this.worldList.IndexOf(world);
              this.RefreshMarketData();
            }

            if (isSelected)
            {
              ImGui.SetItemDefaultFocus();
            }
          }

          ImGui.EndCombo();
        }

        if (this.marketData != null)
        {
          ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 250);
          ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetTextLineHeight());
          ImGui.SetNextItemWidth(250);
          ImGui.Text(
            $"Last update: {DateTimeOffset.FromUnixTimeMilliseconds(this.marketData.LastUploadTime).LocalDateTime:G}");
          ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight() - ImGui.GetTextLineHeightWithSpacing());
        }

        if (ImGui.BeginTabBar("tabBar"))
        {
          if (ImGui.BeginTabItem("Market Data##marketDataTab"))
          {
            ImGui.PushFont(this.fontPtr);
            var tableHeight = (ImGui.GetContentRegionAvail().Y / 2) - (ImGui.GetTextLineHeightWithSpacing() * 2);
            ImGui.Text("Current listings (Includes 5%% GST)");
            ImGui.PopFont();

            ImGui.BeginChild("currentListings", new Vector2(0.0f, tableHeight));
            ImGui.Columns(5, "currentListingsColumns");

            if (!this.hasListingsHQColumnWidthBeenSet)
            {
              ImGui.SetColumnWidth(0, 30.0f);
              this.hasListingsHQColumnWidthBeenSet = true;
            }

            ImGui.Separator();
            ImGui.Text("HQ");
            ImGui.NextColumn();
            ImGui.Text("Price");
            ImGui.NextColumn();
            ImGui.Text("Qty");
            ImGui.NextColumn();
            ImGui.Text("Total");
            ImGui.NextColumn();
            ImGui.Text("Retainer");
            ImGui.NextColumn();
            ImGui.Separator();

            var marketDataListings = this.marketData?.Listings.OrderBy(l => l.PricePerUnit).ToList();
            if (marketDataListings != null)
            {
              foreach (var listing in marketDataListings)
              {
                var index = marketDataListings.IndexOf(listing);

                if (ImGui.Selectable(
                  $"{(listing.Hq ? SeIconChar.HighQuality.AsString() : string.Empty)}##listing{index}",
                  this.selectedListing == index,
                  ImGuiSelectableFlags.SpanAllColumns))
                {
                  this.selectedListing = index;
                }

                ImGui.NextColumn();
                ImGui.Text($"{listing.PricePerUnit:##,###}");
                ImGui.NextColumn();
                ImGui.Text($"{listing.Quantity:##,###}");
                ImGui.NextColumn();
                ImGui.Text($"{listing.Total:##,###}");
                ImGui.NextColumn();
                ImGui.Text($"{listing.RetainerName}{(this.selectedWorld == 0 ? $" {SeIconChar.CrossWorld.ToChar()} {listing.WorldName}" : string.Empty)}");
                ImGui.NextColumn();
                ImGui.Separator();
              }
            }

            ImGui.EndChild();

            ImGui.Separator();

            ImGui.PushFont(this.fontPtr);
            ImGui.Text("Recent history");
            ImGui.PopFont();

            ImGui.BeginChild("recentHistory", new Vector2(0.0f, tableHeight));
            ImGui.Columns(6, "recentHistoryColumns");

            if (!this.hasHistoryHQColumnWidthBeenSet)
            {
              ImGui.SetColumnWidth(0, 30.0f);
              this.hasHistoryHQColumnWidthBeenSet = true;
            }

            ImGui.Separator();
            ImGui.Text("HQ");
            ImGui.NextColumn();
            ImGui.Text("Price");
            ImGui.NextColumn();
            ImGui.Text("Qty");
            ImGui.NextColumn();
            ImGui.Text("Total");
            ImGui.NextColumn();
            ImGui.Text("Date");
            ImGui.NextColumn();
            ImGui.Text("Buyer");
            ImGui.NextColumn();
            ImGui.Separator();

            var marketDataRecentHistory = this.marketData?.RecentHistory.OrderByDescending(h => h.Timestamp).ToList();
            if (marketDataRecentHistory != null)
            {
              foreach (var history in marketDataRecentHistory)
              {
                var index = marketDataRecentHistory.IndexOf(history);

                if (ImGui.Selectable(
                  $"{(history.Hq ? SeIconChar.HighQuality.AsString() : string.Empty)}##history{index}",
                  this.selectedHistory == index,
                  ImGuiSelectableFlags.SpanAllColumns))
                {
                  this.selectedHistory = index;
                }

                ImGui.NextColumn();
                ImGui.Text($"{history.PricePerUnit:##,###}");
                ImGui.NextColumn();
                ImGui.Text($"{history.Quantity:##,###}");
                ImGui.NextColumn();
                ImGui.Text($"{history.Total:##,###}");
                ImGui.NextColumn();
                ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(history.Timestamp).LocalDateTime:G}");
                ImGui.NextColumn();
                ImGui.Text($"{history.BuyerName}{(this.selectedWorld == 0 ? $" {SeIconChar.CrossWorld.ToChar()} {history.WorldName}" : string.Empty)}");
                ImGui.NextColumn();
                ImGui.Separator();
              }
            }

            ImGui.EndChild();
            ImGui.Separator();
            ImGui.EndTabItem();
          }

          if (ImGui.BeginTabItem("Charts##chartsTab"))
          {
            var tableHeight = (ImGui.GetContentRegionAvail().Y / 2) - (ImGui.GetTextLineHeightWithSpacing() * 2);
            var marketDataRecentHistory = this.marketData?.RecentHistory
              .GroupBy(h => DateTimeOffset.FromUnixTimeSeconds(h.Timestamp).LocalDateTime.Date)
              .Select(g => (Date: g.Key, PriceAvg: (float)g.Average(h => h.PricePerUnit),
                QtySum: (float)g.Sum(h => h.Quantity)))
              .ToList();

            if (marketDataRecentHistory != null && marketDataRecentHistory.Count > 0)
            {
              for (var day = marketDataRecentHistory.Min(h => h.Date);
                day <= marketDataRecentHistory.Max(h => h.Date);
                day = day.AddDays(1))
              {
                if (!marketDataRecentHistory.Exists(h => h.Date == day))
                {
                  marketDataRecentHistory.Add((day, 0, 0));
                }
              }

              marketDataRecentHistory = marketDataRecentHistory
                .OrderBy(h => h.Date)
                .ToList();

              ImGui.PushFont(this.fontPtr);
              ImGui.Text("Price variations (per unit)");
              ImGui.PopFont();

              var pricePlotValues = marketDataRecentHistory
                .Select(h => h.PriceAvg)
                .ToArray();
              ImGui.SetNextItemWidth(-1);
              ImGui.PlotLines(
                "##pricePlot",
                ref pricePlotValues[0],
                pricePlotValues.Length,
                0,
                null,
                float.MaxValue,
                float.MaxValue,
                new Vector2(0, tableHeight));

              ImGui.Separator();

              ImGui.PushFont(this.fontPtr);
              ImGui.Text("Traded volumes");
              ImGui.PopFont();

              var qtyPlotValues = marketDataRecentHistory
                .Select(h => h.QtySum)
                .ToArray();
              ImGui.SetNextItemWidth(-1);
              ImGui.PlotHistogram(
                "##qtyPlot",
                ref qtyPlotValues[0],
                qtyPlotValues.Length,
                0,
                null,
                float.MaxValue,
                float.MaxValue,
                new Vector2(0, tableHeight));
            }

            ImGui.EndTabItem();
          }

          ImGui.EndTabBar();
        }
      }

      ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetTextLineHeightWithSpacing());
      ImGui.Text("Data provided by Universalis (https://universalis.app/)");

      ImGui.EndChild();
      ImGui.End();

      return windowOpen;
    }

    internal void ChangeSelectedItem(uint itemId)
    {
      this.selectedItem = this.items.Single(i => i.RowId == itemId);

      var iconId = this.selectedItem.Icon;
      var iconTexFile = this.pluginInterface.Data.GetIcon(iconId);
      this.selectedItemIcon?.Dispose();
      this.selectedItemIcon = this.pluginInterface.UiBuilder.LoadImageRaw(iconTexFile.GetRgbaImageData(), iconTexFile.Header.Width, iconTexFile.Header.Height, 4);

      this.RefreshMarketData();
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
        this.pluginInterface.Framework.OnUpdateEvent -= this.HandleFrameworkUpdateEvent;
        this.pluginInterface.Framework.Gui.HoveredItemChanged -= this.HandleHoveredItemChange;
        this.pluginInterface.UiBuilder.OnBuildFonts -= this.HandleBuildFonts;
        this.hoveredItemChangeTokenSource?.Dispose();
        this.selectedItemIcon?.Dispose();
      }

      this.isDisposed = true;
    }

    private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
    {
      var itemSearchCategories = this.pluginInterface.Data.GetExcelSheet<ItemSearchCategory>();

      var sortedCategories = itemSearchCategories
        .Where(c => c.Category > 0)
        .OrderBy(c => c.Category)
        .ThenBy(c => c.Order)
        .ToDictionary(c => c, c => this.items.Where(i => i.ItemSearchCategory.Row == c.RowId).OrderBy(i => i.Name.ToString()).ToList());

      return sortedCategories;
    }

    private void HandleBuildFonts()
    {
      var fontPath = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(DalamudPluginInterface)).Location) ?? string.Empty, "UIRes", "NotoSansCJKjp-Medium.otf");
      this.fontPtr = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPath, 24.0f);
    }

    private void HandleFrameworkUpdateEvent(Framework framework)
    {
      var localPlayer = this.pluginInterface.ClientState.LocalPlayer;

      if (localPlayer == null)
      {
        return;
      }

      if (this.playerId != this.pluginInterface.ClientState.LocalContentId)
      {
        this.playerId = this.pluginInterface.ClientState.LocalContentId;

        var currentDc = localPlayer.CurrentWorld.GameData.DataCenter;
        var dcWorlds = this.pluginInterface.Data.GetExcelSheet<World>()
          .Where(w => w.DataCenter.Row == currentDc.Row)
          .OrderBy(w => w.Name.ToString())
          .Select(w =>
          {
            string displayName = w.Name;

            if (localPlayer.CurrentWorld.Id == w.RowId)
            {
              displayName += $" {SeIconChar.Hyadelyn.ToChar()}";
            }

            return (w.Name.ToString(), displayName);
          });

        this.worldList.Clear();
        this.worldList.Add((currentDc.Value?.Name, $"Cross-World {SeIconChar.CrossWorld.ToChar()}"));
        this.worldList.AddRange(dcWorlds);

        this.selectedWorld = this.worldList.FindIndex(w => w.Item1 == localPlayer.CurrentWorld.GameData.Name);
      }
    }

    private void HandleHoveredItemChange(object sender, ulong itemId)
    {
      if (!this.watchingForHoveredItem)
      {
        return;
      }

      if (this.hoveredItemChangeTokenSource != null)
      {
        if (!this.hoveredItemChangeTokenSource.IsCancellationRequested)
        {
          this.hoveredItemChangeTokenSource.Cancel();
        }

        this.hoveredItemChangeTokenSource.Dispose();
      }

      this.progressPosition = 0.0f;

      if (itemId == 0)
      {
        this.itemIsBeingHovered = false;
        this.hoveredItemChangeTokenSource = null;
        return;
      }

      this.itemIsBeingHovered = true;
      this.hoveredItemChangeTokenSource = new CancellationTokenSource();

      if (this.IsOpen)
      {
        Task.Run(async () =>
        {
          try
          {
            await Task.Delay(1000, this.hoveredItemChangeTokenSource.Token).ConfigureAwait(false);
            this.ChangeSelectedItem(Convert.ToUInt32(itemId >= 1000000 ? itemId - 1000000 : itemId));
          }
          catch (TaskCanceledException)
          {
          }
        });
      }
    }

    private void RefreshMarketData()
    {
      Task.Run(async () =>
      {
        this.marketData = null;
        this.marketData = await UniversalisClient
          .GetMarketData(this.selectedItem.RowId, this.worldList[this.selectedWorld].Item1, CancellationToken.None)
          .ConfigureAwait(false);
      });
    }
  }
}
