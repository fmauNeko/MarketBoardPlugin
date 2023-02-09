// <copyright file="MarketBoardWindow.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

using System.Data.SqlTypes;

namespace MarketBoardPlugin.GUI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Numerics;
  using System.Runtime.InteropServices;
  using System.Threading;
  using System.Threading.Tasks;
  using Dalamud.Game;
  using Dalamud.Game.Text;
  using Dalamud.Interface;
  using Dalamud.Logging;
  using Dalamud.Utility;
  using ImGuiNET;
  using ImGuiScene;
  using Lumina.Data.Parsing;
  using Lumina.Excel.GeneratedSheets;
  using MarketBoardPlugin.Extensions;
  using MarketBoardPlugin.Helpers;
  using MarketBoardPlugin.Models.ShoppingList;
  using MarketBoardPlugin.Models.Universalis;

  /// <summary>
  /// The market board window.
  /// </summary>
  public class MarketBoardWindow : IDisposable
  {
    private readonly IEnumerable<Item> items;

    private readonly MBPluginConfig config;

    private readonly List<(string, string)> worldList = new List<(string, string)>();

    private Dictionary<ItemSearchCategory, List<Item>> sortedCategoriesAndItems;

    private bool isDisposed;

    private ulong itemBeingHovered;

    private bool searchHistoryOpen;

    private bool advancedSearchMenuOpen;

    private bool settingMenuOpen;

    private float progressPosition;

    private string searchString = string.Empty;
    private string lastSearchString = string.Empty;

    private ClassJob lastSelectedClassJob;

    private int lvlmin;
    private int lastlvlmin;
    private int lvlmax = 90;
    private int lastlvlmax = 90;

    private int itemCategory;
    private int lastItemCategory;
    private string[] categoryLabels = new[] { "All", "Weapons", "Equipments", "Others", "Furniture" };
    private readonly List<ClassJob> classJobs;

    private Item selectedItem;

    private ClassJob selectedClassJob;

    private TextureWrap selectedItemIcon;

    private bool watchingForHoveredItem = true;

    private bool priceIconShown = true;

    private bool hQOnly;

    private ulong playerId;

    private int minQuantityFilter;

    private int selectedWorld = -1;

    private MarketDataResponse marketData;

    private List<MarketDataResponse> marketBuffer;

    private int marketBufferMaxSize = 10;

    private int oldMarketBufferMaxSize = 10;

    private int bufferRefreshTimeout = 30000; // milliseconds

    private int oldIntRefreshTimeout = 30000;

    private int selectedListing = -1;

    private int selectedHistory = -1;

    private ImFontPtr fontPtr;

    private bool hasListingsHQColumnWidthBeenSet;

    private bool hasHistoryHQColumnWidthBeenSet;

    private List<KeyValuePair<ItemSearchCategory, List<Item>>> enumerableCategoriesAndItems;

    private List<SavedItem> shoppingList = new List<SavedItem>();

    private NumberFormatInfo numberFormatInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardWindow"/> class.
    /// </summary>
    /// <param name="config">The <see cref="MBPluginConfig"/>.</param>
    public MarketBoardWindow(MBPluginConfig config)
    {
      this.marketBuffer = new List<MarketDataResponse>();
      this.items = MBPlugin.Data.GetExcelSheet<Item>();
      this.classJobs = MBPlugin.Data.GetExcelSheet<ClassJob>()?
        .Where(cj => cj.RowId != 0)
        .OrderBy(cj =>
        {
          return cj.Role switch
          {
            0 => 3,
            1 => 0,
            2 => 2,
            3 => 2,
            4 => 1,
            _ => 4,
          };
        }).ToList();
      this.config = config ?? throw new ArgumentNullException(nameof(config));
      this.sortedCategoriesAndItems = this.SortCategoriesAndItems();

      MBPlugin.Framework.Update += this.HandleFrameworkUpdateEvent;
      MBPlugin.GameGui.HoveredItemChanged += this.HandleHoveredItemChange;
      MBPlugin.PluginInterface.UiBuilder.BuildFonts += this.HandleBuildFonts;

      MBPlugin.PluginInterface.UiBuilder.RebuildFonts();

      this.watchingForHoveredItem = this.config.WatchForHovered;
      this.priceIconShown = this.config.PriceIconShown;
      this.bufferRefreshTimeout = this.config.ItemRefreshTimeout;
      this.marketBufferMaxSize = this.config.MarketBufferSize;

      this.numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
      this.numberFormatInfo.CurrencySymbol = SeIconChar.Gil.ToIconString();
      this.numberFormatInfo.CurrencyDecimalDigits = 0;

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

      if (this.sortedCategoriesAndItems == null)
      {
        this.sortedCategoriesAndItems = this.SortCategoriesAndItems();
        return true;
      }

      this.enumerableCategoriesAndItems ??= this.sortedCategoriesAndItems.ToList();

      // Update Categories And Items if needed
      if (this.searchString != this.lastSearchString || this.itemCategory != this.lastItemCategory
                                                     || this.lastlvlmin != this.lvlmin
                                                     || this.lastlvlmax != this.lvlmax
                                                     || this.lastSelectedClassJob != this.selectedClassJob)
      {
        this.UpdateCategoriesAndItems();
      }

      var scale = ImGui.GetIO().FontGlobalScale;

      // Window Setup
      ImGui.SetNextWindowSize(new Vector2(800, 600) * scale, ImGuiCond.FirstUseEver);
      ImGui.SetNextWindowSizeConstraints(new Vector2(350, 225) * scale, new Vector2(10000, 10000) * scale);

      if (!ImGui.Begin($"Market Board", ref windowOpen, ImGuiWindowFlags.NoScrollbar))
      {
        ImGui.End();
        return windowOpen;
      }

      // Item List Column Setup
      ImGui.BeginChild("itemListColumn", new Vector2(267, 0) * scale, true);

      ImGui.SetNextItemWidth((-32 * ImGui.GetIO().FontGlobalScale) - ImGui.GetStyle().ItemSpacing.X);
      ImGuiOverrides.InputTextWithHint("##searchString", "Search for item", ref this.searchString, 256);

      ImGui.SameLine();
      ImGui.PushFont(UiBuilder.IconFont);
      ImGui.PushStyleColor(ImGuiCol.Text, this.searchHistoryOpen ? 0xFF0000FF : 0xFFFFFFFF);
      if (ImGui.Button($"{(char)FontAwesomeIcon.History}", new Vector2(32 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
      {
        this.searchHistoryOpen = !this.searchHistoryOpen;
      }

      ImGui.PopStyleColor();
      ImGui.PopFont();

      var previousYCursor = ImGui.GetCursorPosY();
      ImGui.SetCursorPosY(previousYCursor - (ImGui.GetFontSize() / 2.0f) + (13 * scale));
      ImGui.Text("Advanced Search");
      ImGui.SameLine();
      ImGui.SetCursorPosY(previousYCursor);
      ImGui.PushFont(UiBuilder.IconFont);
      ImGui.PushStyleColor(ImGuiCol.Text, this.advancedSearchMenuOpen ? 0xFF0000FF : 0xFFFFFFFF);
      if (ImGui.Button($"{(char)FontAwesomeIcon.Wrench}", new Vector2(32 * ImGui.GetIO().FontGlobalScale, 1.5f * ImGui.GetItemRectSize().Y)))
      {
        this.advancedSearchMenuOpen = !this.advancedSearchMenuOpen;
      }

      ImGui.PopStyleColor();
      ImGui.PopFont();

      if (this.advancedSearchMenuOpen)
      {
        ImGui.Text("Category: ");
        ImGui.SameLine();
        ImGui.Combo("###ListBox", ref this.itemCategory, this.categoryLabels, this.categoryLabels.Length);
        ImGui.Text("HQ Only : ");
        ImGui.SameLine();
        ImGui.Checkbox("###Checkbox", ref this.hQOnly);
        ImGui.Text("Min Qty : ");
        ImGui.SameLine();
        ImGui.InputInt("###MinQuantity", ref this.minQuantityFilter);
        ImGui.Text("Class: ");
        ImGui.SameLine();
        if (ImGui.BeginCombo(
          "###ClassJobCombo",
          this.selectedClassJob == null ? "All Classes" : this.selectedClassJob.Abbreviation))
        {
          void SelectClassJob(ClassJob classJob)
          {
            var selected = this.selectedClassJob == classJob;
            if (ImGui.Selectable(classJob == null ? "All Classes" : classJob.Abbreviation, selected))
            {
              this.selectedClassJob = classJob;
            }

            if (selected)
            {
              ImGui.SetItemDefaultFocus();
            }
          }

          SelectClassJob(null);

          foreach (var classJob in this.classJobs)
          {
            SelectClassJob(classJob);
          }

          ImGui.EndCombo();
        }

        if (this.itemCategory is 1 or 2)
        {
          ImGui.Text("Min level : ");
          ImGui.SameLine();
          ImGui.InputInt("##lvlmin", ref this.lvlmin);
          ImGui.Text("Max level : ");
          ImGui.SameLine();
          ImGui.InputInt("##lvlmax", ref this.lvlmax);
        }
        else
        {
          // If the category selected doesn't need an equip level -> reset to default
          this.lvlmin = 0;
          this.lvlmax = 90;
        }
      }

      ImGui.Separator();
      ImGui.BeginChild("itemTree", new Vector2(0, -2.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
      var itemTextSize = ImGui.CalcTextSize(string.Empty);

      if (this.searchHistoryOpen)
      {
        ImGui.Text("History");
        ImGui.Separator();
        var sheet = MBPlugin.Data.Excel.GetSheet<Item>();
        foreach (var id in this.config.History.ToArray())
        {
          var item = sheet.GetRow(id);
          if (item == null)
          {
            continue;
          }

          if (ImGui.Selectable($"{item.Name}", this.selectedItem == item))
          {
            this.ChangeSelectedItem(id, true);
          }
        }
      }
      else
      {
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

              if (ImGui.BeginPopupContextItem("shoplist" + category.Key.Name + i))
              {
                if (this.selectedItem != null && item != null && this.selectedItem.RowId != item.RowId)
                {
                  this.ChangeSelectedItem(item.RowId);
                }

                if (ImGui.Selectable("Add to the shopping list") && this.marketData != null && this.selectedWorld >= 0)
                {
                  MarketDataListing itm = this.marketData.Listings.OrderBy(l => l.PricePerUnit).ToList()[0];
                  this.shoppingList.Add(new SavedItem(item, itm.PricePerUnit, itm.WorldName));
                }

                ImGui.EndPopup();
              }

              ImGui.OpenPopupOnItemClick("shoplist" + category.Key.Name + i, ImGuiPopupFlags.MouseButtonRight);
            }

            ImGui.Indent(ImGui.GetTreeNodeToLabelSpacing());
            ImGui.TreePop();
          }
        }
      }

      ImGui.EndChild();

      if (this.itemBeingHovered != 0)
      {
        if (this.progressPosition < 1.0f)
        {
          this.progressPosition += ImGui.GetIO().DeltaTime;
        }
        else
        {
          this.progressPosition = 0;
          var itemId = MBPlugin.GameGui.HoveredItem;
          this.ChangeSelectedItem(Convert.ToUInt32(itemId % 500000));
          this.itemBeingHovered = 0;
        }
      }
      else
      {
        this.progressPosition = 0.0f;
      }

      ImGui.Text("Settings : ");
      ImGui.SameLine();
      ImGui.PushFont(UiBuilder.IconFont);
      if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}"))
      {
        this.settingMenuOpen = !this.settingMenuOpen;
      }

      ImGui.PopFont();

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
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetFontSize() / 2.0f) + (19 * scale));
        ImGui.Text(this.selectedItem?.Name);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - (250 * scale));
        ImGui.SetCursorPosY(0);
        ImGui.PopFont();
        ImGui.BeginGroup();
        ImGui.SetNextItemWidth(250 * scale);
        if (ImGui.BeginCombo("##worldCombo", this.selectedWorld > -1 ? this.worldList[this.selectedWorld].Item2 : string.Empty))
        {
          foreach (var world in this.worldList)
          {
            var isSelected = this.selectedWorld == this.worldList.IndexOf(world);
            if (ImGui.Selectable(world.Item2, isSelected))
            {
              this.selectedWorld = this.worldList.IndexOf(world);
              this.config.CrossDataCenter = this.selectedWorld == 0;
              this.config.CrossWorld = this.selectedWorld == 1;
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
          ImGui.SetNextItemWidth(250 * scale);
          ImGui.Text(
            $"Last update: {DateTimeOffset.FromUnixTimeMilliseconds(this.marketData.LastUploadTime).LocalDateTime:G}" +
                $"\nLast Fetch : {DateTimeOffset.FromUnixTimeMilliseconds(this.marketData.FetchTimestamp)}");

          ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight() - ImGui.GetTextLineHeightWithSpacing());
        }
        else
        {
          ImGui.SetNextItemWidth(250 * scale);
          ImGui.Text(
            $"Fetching data from Universalis...");
          ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight() - ImGui.GetTextLineHeightWithSpacing());
        }

        ImGui.EndGroup();

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

            var marketDataListings = this.marketData?.Listings.Where(i => !this.hQOnly || i.Hq)
              .Where(l => l.Quantity >= this.minQuantityFilter).OrderBy(l => l.PricePerUnit).ToList();
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
                if (this.priceIconShown)
                {
                  ImGui.Text(listing.PricePerUnit.ToString("C", this.numberFormatInfo));
                }
                else
                {
                  ImGui.Text(listing.PricePerUnit.ToString("N0"));
                }

                ImGui.NextColumn();
                ImGui.Text($"{listing.Quantity:##,###}");
                ImGui.NextColumn();
                if (this.priceIconShown)
                {
                  ImGui.Text(listing.Total.ToString("C", this.numberFormatInfo));
                }
                else
                {
                  ImGui.Text(listing.Total.ToString("N0"));
                }

                ImGui.NextColumn();
                ImGui.Text($"{listing.RetainerName} {SeIconChar.CrossWorld.ToChar()} {(this.selectedWorld <= 1 ? listing.WorldName : this.worldList[this.selectedWorld].Item1)}");
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
                if (this.priceIconShown)
                {
                  ImGui.Text(history.PricePerUnit.ToString("C", this.numberFormatInfo));
                }
                else
                {
                  ImGui.Text(history.PricePerUnit.ToString("N0"));
                }

                ImGui.NextColumn();
                ImGui.Text($"{history.Quantity:##,###}");
                ImGui.NextColumn();
                if (this.priceIconShown)
                {
                  ImGui.Text(history.Total.ToString("C", this.numberFormatInfo));
                }
                else
                {
                  ImGui.Text(history.Total.ToString("N0"));
                }

                ImGui.NextColumn();
                ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(history.Timestamp).LocalDateTime:G}");
                ImGui.NextColumn();
                ImGui.Text($"{history.BuyerName} {SeIconChar.CrossWorld.ToChar()} {(this.selectedWorld <= 1 ? history.WorldName : this.worldList[this.selectedWorld].Item1)}");
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
      if (ImGui.Button("Data provided by Universalis"))
      {
        Utilities.OpenBrowser("https://universalis.app/");
      }

      ImGui.SameLine(ImGui.GetContentRegionAvail().X - (120 * scale));

      if (!this.config.KofiHidden)
      {
        var buttonText = "Support on Ko-fi";
        var buttonColor = 0x005E5BFFu;
        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | buttonColor);

        if (ImGui.Button(buttonText, new Vector2(120, 24)))
        {
          Utilities.OpenBrowser("https://ko-fi.com/fmauneko");
        }

        ImGui.PopStyleColor(3);
      }
      else
      {
        ImGui.Dummy(new Vector2(120, 24));
      }

      ImGui.EndChild();
      ImGui.End();

      if (this.settingMenuOpen)
      {
        this.OpenSettingMenu();
      }

      if (this.shoppingList.Count > 0)
      {
        this.ShowShoppingListMenu();
      }

      return windowOpen;
    }

    internal void ChangeSelectedItem(uint itemId, bool noHistory = false)
    {
      this.selectedItem = this.items.Single(i => i.RowId == itemId);

      var iconId = this.selectedItem.Icon;
      var iconTexFile = MBPlugin.Data.GetIcon(iconId);
      this.selectedItemIcon?.Dispose();
      this.selectedItemIcon = MBPlugin.PluginInterface.UiBuilder.LoadImageRaw(iconTexFile.GetRgbaImageData(), iconTexFile.Header.Width, iconTexFile.Header.Height, 4);

      this.RefreshMarketData();
      if (!noHistory)
      {
        this.config.History.RemoveAll(i => i == itemId);
        this.config.History.Insert(0, itemId);
        if (this.config.History.Count > 100)
        {
          this.config.History.RemoveRange(100, this.config.History.Count - 100);
        }

        MBPlugin.PluginInterface.SavePluginConfig(this.config);
      }
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
        MBPlugin.Framework.Update -= this.HandleFrameworkUpdateEvent;
        MBPlugin.GameGui.HoveredItemChanged -= this.HandleHoveredItemChange;
        MBPlugin.PluginInterface.UiBuilder.BuildFonts -= this.HandleBuildFonts;
        this.selectedItemIcon?.Dispose();
      }

      this.isDisposed = true;
    }

    private void OpenSettingMenu()
    {
      var scale = ImGui.GetIO().FontGlobalScale;
      ImGui.SetNextWindowSize(new Vector2(150, 75) * scale, ImGuiCond.FirstUseEver);

      ImGui.Begin("Settings");
      var contextMenuIntegration = this.config.ContextMenuIntegration;
      var kofiHidden = this.config.KofiHidden;
      if (ImGui.Checkbox("Context menu integration", ref contextMenuIntegration))
      {
        this.config.ContextMenuIntegration = contextMenuIntegration;
        MBPlugin.PluginInterface.SavePluginConfig(this.config);
      }

      if (ImGui.Checkbox("Gil Icon Shown", ref this.priceIconShown))
      {
        this.config.PriceIconShown = this.priceIconShown;
        MBPlugin.PluginInterface.SavePluginConfig(this.config);
      }

      if (ImGui.Checkbox("Watch for hovered item", ref this.watchingForHoveredItem))
      {
        this.config.WatchForHovered = this.watchingForHoveredItem;
      }

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

      if (ImGui.Checkbox("Hide Ko-Fi button", ref kofiHidden))
      {
        this.config.KofiHidden = kofiHidden;
        MBPlugin.PluginInterface.SavePluginConfig(this.config);
      }

      ImGui.Text("Number of buffered item : ");
      ImGui.InputInt("###marketBufferSize", ref this.marketBufferMaxSize);
      if (this.oldMarketBufferMaxSize != this.marketBufferMaxSize)
      {
        this.config.MarketBufferSize = this.marketBufferMaxSize;
        MBPlugin.PluginInterface.SavePluginConfig(this.config);
        this.oldMarketBufferMaxSize = this.marketBufferMaxSize;
      }

      ImGui.Text("Item buffer Timeout (ms) :");
      ImGui.InputInt("###refreshTimeout", ref this.bufferRefreshTimeout);
      if (this.oldIntRefreshTimeout != this.bufferRefreshTimeout)
      {
        this.config.ItemRefreshTimeout = this.bufferRefreshTimeout;
        MBPlugin.PluginInterface.SavePluginConfig(this.config);
        this.oldIntRefreshTimeout = this.bufferRefreshTimeout;
      }

      ImGui.End();
    }

    /// <summary>
    /// Create a new separate window showing the shopping list.
    /// </summary>
    private void ShowShoppingListMenu()
    {
      var scale = ImGui.GetIO().FontGlobalScale;

      ImGui.SetNextWindowSize(new Vector2(400, 150) * scale, ImGuiCond.FirstUseEver);
      ImGui.SetNextWindowSizeConstraints(new Vector2(400, 150) * scale, new Vector2(10000, 10000) * scale);

      ImGui.Begin("Shopping List");
      ImGui.Columns(4, "recentHistoryColumns");
      ImGui.Text("Name");
      ImGui.NextColumn();
      ImGui.Text("Price");
      ImGui.NextColumn();
      ImGui.Text("World");
      ImGui.NextColumn();
      ImGui.Text("Action");
      ImGui.NextColumn();
      ImGui.Separator();

      List<SavedItem> todel = new List<SavedItem>();

      int k = 0;
      foreach (var item in this.shoppingList)
      {
        ImGui.Text(item.SourceItem.Name);
        ImGui.NextColumn();
        if (this.priceIconShown)
        {
          ImGui.Text(item.Price.ToString("C", this.numberFormatInfo));
        }
        else
        {
          ImGui.Text(item.Price.ToString("N0"));
        }

        ImGui.NextColumn();
        ImGui.Text(item.World);
        ImGui.NextColumn();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Slash}##shoplist" + k, new Vector2(32 * ImGui.GetIO().FontGlobalScale, 1.5f * ImGui.GetItemRectSize().Y)))
        {
          todel.Add(item);
        }

        ImGui.PopFont();
        ImGui.NextColumn();
        ImGui.Separator();
        k += 1;
      }

      foreach (var item in todel)
      {
        this.shoppingList.Remove(item);
      }

      ImGui.End();
    }

    /// <summary>
    /// Update Categories and Items Dictionary based on current searchString.
    /// </summary>
    private void UpdateCategoriesAndItems()
    {
      if (!string.IsNullOrEmpty(this.searchString))
      {
        this.enumerableCategoriesAndItems = this.sortedCategoriesAndItems.Where(c => (this.itemCategory == 0 || (this.itemCategory > 0 && c.Key.Category == this.itemCategory)))
          .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(
            kv.Key,
            kv.Value
              .Where(i =>
                i.Name.ToString().ToUpperInvariant().Contains(this.searchString.ToUpperInvariant(), StringComparison.InvariantCulture))
              .Where(i => i.LevelEquip >= this.lvlmin && i.LevelEquip <= this.lvlmax)
              .Where(i => i.ClassJobCategory.Value.HasClass(this.selectedClassJob))
              .ToList()))
          .Where(kv => kv.Value.Count > 0)
          .ToList();
      }
      else
      {
        this.enumerableCategoriesAndItems = this.sortedCategoriesAndItems.Where(c => (this.itemCategory == 0 || (this.itemCategory > 0 && c.Key.Category == this.itemCategory)))
          .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(
            kv.Key,
            kv.Value
              .Where(i => i.LevelEquip >= this.lvlmin && i.LevelEquip <= this.lvlmax)
              .Where(i => i.ClassJobCategory.Value.HasClass(this.selectedClassJob))
              .ToList()))
          .Where(kv => kv.Value.Count > 0)
          .ToList();
      }

      this.lastSearchString = this.searchString;
      this.lastItemCategory = this.itemCategory;
      this.lastSelectedClassJob = this.selectedClassJob;
      this.lastlvlmin = this.lvlmin;
      this.lastlvlmax = this.lvlmax;
    }

    private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
    {
      try
      {
        var itemSearchCategories = MBPlugin.Data.GetExcelSheet<ItemSearchCategory>();

        var sortedCategories = itemSearchCategories.Where(c => c.Category > 0).OrderBy(c => c.Category).ThenBy(c => c.Order);

        var sortedCategoriesDict = new Dictionary<ItemSearchCategory, List<Item>>();

        foreach (var c in sortedCategories)
        {
          if (sortedCategoriesDict.ContainsKey(c))
          {
            continue;
          }

          sortedCategoriesDict.Add(c, this.items.Where(i => i.ItemSearchCategory.Row == c.RowId).OrderBy(i => i.Name.ToString()).ToList());
        }

        return sortedCategoriesDict;
      }
      catch (Exception ex)
      {
        PluginLog.Error(ex, $"Error loading category list.");
        return null;
      }
    }

    private unsafe void HandleBuildFonts()
    {
      var fontPath = Path.Combine(MBPlugin.PluginInterface.DalamudAssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Medium.otf");
      this.fontPtr = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPath, 24.0f);

      ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
      fontConfig.MergeMode = true;
      fontConfig.NativePtr->DstFont = UiBuilder.DefaultFont.NativePtr;

      var fontRangeHandle = GCHandle.Alloc(
        new ushort[]
        {
            0x202F,
            0x202F,
            0,
        },
        GCHandleType.Pinned);

      var otherPath = Path.Combine(MBPlugin.PluginInterface.AssemblyLocation.DirectoryName, "Resources", "NotoSans-Medium.otf");
      ImGui.GetIO().Fonts.AddFontFromFileTTF(otherPath, 17.0f, fontConfig, fontRangeHandle.AddrOfPinnedObject());

      fontConfig.Destroy();
      fontRangeHandle.Free();
    }

    private void HandleFrameworkUpdateEvent(Framework framework)
    {
      if (MBPlugin.ClientState.LocalContentId != 0 && this.playerId != MBPlugin.ClientState.LocalContentId)
      {
        var localPlayer = MBPlugin.ClientState.LocalPlayer;
        if (localPlayer == null)
        {
          return;
        }

        var currentDc = localPlayer.CurrentWorld.GameData.DataCenter;
        var dcWorlds = MBPlugin.Data.GetExcelSheet<World>()
          .Where(w => w.DataCenter.Row == currentDc.Row && w.IsPublic)
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

        var regionName = localPlayer.CurrentWorld.GameData.DataCenter.Value.Region switch
        {
          1 => "Japan",
          2 => "North-America",
          3 => "Europe",
          4 => "Oceania",
          _ => string.Empty,
        };

        this.worldList.Clear();
        this.worldList.Add((regionName, $"Cross-DC {SeIconChar.CrossWorld.ToChar()}"));
        this.worldList.Add((currentDc.Value?.Name, $"Cross-World {SeIconChar.CrossWorld.ToChar()}"));
        this.worldList.AddRange(dcWorlds);

        if (this.config.CrossDataCenter)
        {
          this.selectedWorld = 0;
        }
        else if (this.config.CrossWorld)
        {
          this.selectedWorld = 1;
        }
        else
        {
          this.selectedWorld = this.worldList.FindIndex(w => w.Item1 == localPlayer.CurrentWorld.GameData.Name);
        }

        if (this.worldList.Count > 1)
        {
          this.playerId = MBPlugin.ClientState.LocalContentId;
        }
      }

      if (MBPlugin.ClientState.LocalContentId == 0)
      {
        this.playerId = 0;
      }
    }

    private void HandleHoveredItemChange(object sender, ulong itemId)
    {
      if (!this.watchingForHoveredItem || this.itemBeingHovered == itemId)
      {
        return;
      }

      this.progressPosition = 0.0f;

      if (itemId == 0 || itemId >= 2000000)
      {
        this.itemBeingHovered = 0;
        return;
      }

      var item = MBPlugin.Data.Excel.GetSheet<Item>().GetRow((uint)itemId % 500000);

      if (item != null && this.enumerableCategoriesAndItems != null && this.sortedCategoriesAndItems.Any(i => i.Value != null && i.Value.Any(k => k.ToString() == item.ToString())))
      {
        this.itemBeingHovered = itemId;
      }
      else
      {
        this.itemBeingHovered = 0;
      }
    }

    /// <summary>
    ///  Function used for debug purposes : Log every attributes of an Item.
    /// </summary>
    /// <param name="itm"> Item class to show in logs.</param>
    private static void LogItemInfo(Item itm)
    {
      foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(itm))
      {
        string name = descriptor.Name;
        object value = descriptor.GetValue(itm);
        PluginLog.LogInformation("{0}={1}", name, value);
      }
    }

    private void RefreshMarketData()
    {
      Task.Run(async () =>
      {
          var cachedItem = this.marketBuffer.FirstOrDefault(data => data.ItemId == this.selectedItem.RowId, null);
          if (cachedItem != null)
          {
            this.marketData = cachedItem;
          }

          if (cachedItem == null || DateTimeOffset.Now.ToUnixTimeMilliseconds() - cachedItem.FetchTimestamp > this.bufferRefreshTimeout)
          {
            MarketDataResponse response = null;
            response = await UniversalisClient
              .GetMarketData(this.selectedItem.RowId, this.worldList[this.selectedWorld].Item1, 50,
                CancellationToken.None)
              .ConfigureAwait(false);

            if (this.marketData != null)
            {
              this.marketBuffer.Add(this.marketData);
            }

            if (this.marketBuffer.Count > this.marketBufferMaxSize)
            {
              this.marketBuffer.RemoveAt(0);
            }

            this.marketData = response;
          }
        });
    }
  }
}
