// <copyright file="MarketBoardWindow.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.GUI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Globalization;
  using System.Linq;
  using System.Numerics;
  using System.Text.RegularExpressions;
  using System.Threading;
  using System.Threading.Tasks;
  using Dalamud.Game.Text;
  using Dalamud.Interface;
  using Dalamud.Interface.Internal;
  using Dalamud.Interface.ManagedFontAtlas;
  using Dalamud.Interface.Windowing;
  using Dalamud.Plugin.Services;
  using ImGuiNET;
  using Lumina.Excel.GeneratedSheets;
  using MarketBoardPlugin.Extensions;
  using MarketBoardPlugin.Helpers;
  using MarketBoardPlugin.Models.ShoppingList;
  using MarketBoardPlugin.Models.Universalis;

  /// <summary>
  /// The market board window.
  /// </summary>
  public class MarketBoardWindow : Window, IDisposable
  {
    private static Dictionary<char, int> romanNumberMap = new Dictionary<char, int>
    {
      { 'I', 1 },
      { 'V', 5 },
      { 'X', 10 },
    };

    private readonly IEnumerable<Item> items;

    private readonly MBPlugin plugin;

    private readonly List<(string, string)> worldList = new List<(string, string)>();

    private readonly List<ClassJob> classJobs;

    private readonly IFontHandle titleFontHandle;

    private Dictionary<ItemSearchCategory, List<Item>> sortedCategoriesAndItems;

    private bool isDisposed;

    private ulong itemBeingHovered;

    private bool searchHistoryOpen;

    private bool advancedSearchMenuOpen;

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
    private Item selectedItem;

    private ClassJob selectedClassJob;

    private IDalamudTextureWrap selectedItemIcon;

    private bool hQOnly;

    private ulong playerId;

    private int minQuantityFilter;

    private int selectedWorld = -1;

    private int previousSelectedWorld = -1;

    private MarketDataResponse marketData;

    private List<MarketDataResponse> marketBuffer;

    private int selectedListing = -1;

    private int selectedHistory = -1;

    private bool hasListingsHQColumnWidthBeenSet;

    private bool hasHistoryHQColumnWidthBeenSet;

    private List<KeyValuePair<ItemSearchCategory, List<Item>>> enumerableCategoriesAndItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardWindow"/> class.
    /// </summary>
    /// <param name="plugin">The <see cref="MBPlugin"/>.</param>
    public MarketBoardWindow(MBPlugin plugin)
      : base("Market Board")
    {
      this.Flags = ImGuiWindowFlags.NoScrollbar;
      this.Size = new Vector2(800, 600);
      this.SizeCondition = ImGuiCond.FirstUseEver;
      this.SizeConstraints = new WindowSizeConstraints
      {
        MinimumSize = new Vector2(350, 225),
        MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
      };

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
      this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
      this.sortedCategoriesAndItems = this.SortCategoriesAndItems();

      MBPlugin.Framework.Update += this.HandleFrameworkUpdateEvent;
      MBPlugin.GameGui.HoveredItemChanged += this.HandleHoveredItemChange;

      this.titleFontHandle = MBPlugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        e.OnPreBuild(toolkit =>
          toolkit.AddDalamudDefaultFont(MBPlugin.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * 1.5f)));

      MBPlugin.PluginInterface.UiBuilder.RebuildFonts();

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

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reset the market data.
    /// </summary>
    public void ResetMarketData()
    {
      this.marketBuffer.Clear();
      this.RefreshMarketData();
    }

    /// <summary>
    /// Draws the window.
    /// </summary>
    public override void Draw()
    {
      if (this.sortedCategoriesAndItems == null)
      {
        this.sortedCategoriesAndItems = this.SortCategoriesAndItems();
        return;
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
        foreach (var id in this.plugin.Config.History.ToArray())
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
                  double price = this.plugin.Config.NoGilSalesTax
                    ? itm.PricePerUnit
                    : itm.PricePerUnit + (itm.Tax / itm.Quantity);
                  this.plugin.ShoppingList.Add(new SavedItem(item, price, itm.WorldName));
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
        this.plugin.OpenConfigUi();
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
          if (ImGui.ImageButton(this.selectedItemIcon.ImGuiHandle, new Vector2(40, 40)))
          {
            ImGui.LogToClipboard();
            ImGui.LogText(this.selectedItem.Name);
            ImGui.LogFinish();
          }
        }
        else
        {
          ImGui.SetCursorPos(new Vector2(40, 40));
        }

        this.titleFontHandle.Push();
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetFontSize() / 2.0f) + (20 * scale));
        ImGui.Text(this.selectedItem?.Name);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - (250 * scale));
        ImGui.SetCursorPosY(0);
        this.titleFontHandle.Pop();
        ImGui.BeginGroup();
        ImGui.SetNextItemWidth(250 * scale);
        if (ImGui.BeginCombo("##worldCombo", this.selectedWorld > -1 ? this.worldList[this.selectedWorld].Item2 : string.Empty))
        {
          foreach (var world in this.worldList)
          {
            var isSelected = this.selectedWorld == this.worldList.IndexOf(world);
            if (ImGui.Selectable(world.Item2, isSelected))
            {
              this.previousSelectedWorld = this.selectedWorld;
              this.selectedWorld = this.worldList.IndexOf(world);
              this.plugin.Config.CrossDataCenter = this.selectedWorld == 0;
              this.plugin.Config.CrossWorld = this.selectedWorld == 1;
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
                $"\nLast Fetch : {DateTimeOffset.FromUnixTimeMilliseconds(this.marketData.FetchTimestamp).LocalDateTime:G}");

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
            this.titleFontHandle.Push();
            int usedTile = this.plugin.Config.RecentHistoryDisabled ? 1 : 2;
            var tableHeight = (ImGui.GetContentRegionAvail().Y / usedTile) - (ImGui.GetTextLineHeightWithSpacing() * 2);
            ImGui.Text("Current listings (Includes 5%% GST)");
            this.titleFontHandle.Pop();

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
                double pricePerUnit = this.plugin.Config.NoGilSalesTax
                  ? listing.PricePerUnit
                  : listing.PricePerUnit + (listing.Tax / listing.Quantity);
                if (this.plugin.Config.PriceIconShown)
                {
                  ImGui.Text(pricePerUnit.ToString("C", this.plugin.NumberFormatInfo));
                }
                else
                {
                  ImGui.Text(pricePerUnit.ToString("N0", CultureInfo.CurrentCulture));
                }

                ImGui.NextColumn();
                ImGui.Text($"{listing.Quantity:##,###}");
                ImGui.NextColumn();
                double totalPrice = this.plugin.Config.NoGilSalesTax
                  ? listing.Total
                  : listing.Total + listing.Tax;
                if (this.plugin.Config.PriceIconShown)
                {
                  ImGui.Text(totalPrice.ToString("C", this.plugin.NumberFormatInfo));
                }
                else
                {
                  ImGui.Text(totalPrice.ToString("N0", CultureInfo.CurrentCulture));
                }

                ImGui.NextColumn();
                ImGui.Text(
                  $"{listing.RetainerName} {SeIconChar.CrossWorld.ToChar()} {(this.selectedWorld <= 1 ? listing.WorldName : this.worldList[this.selectedWorld].Item1)}");
                ImGui.NextColumn();
                ImGui.Separator();
              }
            }

            ImGui.EndChild();
            if (!this.plugin.Config.RecentHistoryDisabled)
            {
              ImGui.Separator();

              this.titleFontHandle.Push();
              ImGui.Text("Recent history");
              this.titleFontHandle.Pop();

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
                  if (this.plugin.Config.PriceIconShown)
                  {
                    ImGui.Text(history.PricePerUnit.ToString("C", this.plugin.NumberFormatInfo));
                  }
                  else
                  {
                    ImGui.Text(history.PricePerUnit.ToString("N0", CultureInfo.CurrentCulture));
                  }

                  ImGui.NextColumn();
                  ImGui.Text($"{history.Quantity:##,###}");
                  ImGui.NextColumn();
                  if (this.plugin.Config.PriceIconShown)
                  {
                    ImGui.Text(history.Total.ToString("C", this.plugin.NumberFormatInfo));
                  }
                  else
                  {
                    ImGui.Text(history.Total.ToString("N0", CultureInfo.CurrentCulture));
                  }

                  ImGui.NextColumn();
                  ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(history.Timestamp).LocalDateTime:G}");
                  ImGui.NextColumn();
                  ImGui.Text(
                    $"{history.BuyerName} {SeIconChar.CrossWorld.ToChar()} {(this.selectedWorld <= 1 ? history.WorldName : this.worldList[this.selectedWorld].Item1)}");
                  ImGui.NextColumn();
                  ImGui.Separator();
                }
              }

              ImGui.EndChild();
            }

            ImGui.EndTabItem();
          }

          ImGui.Separator();
          if (ImGui.BeginTabItem("Charts##chartsTab"))
          {
            this.titleFontHandle.Push();
            var tableHeight = (ImGui.GetContentRegionAvail().Y / 2) - (ImGui.GetTextLineHeightWithSpacing() * 2);
            this.titleFontHandle.Pop();
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

              this.titleFontHandle.Push();
              ImGui.Text("Price variations (per unit)");
              this.titleFontHandle.Pop();

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

              this.titleFontHandle.Push();
              ImGui.Text("Traded volumes");
              this.titleFontHandle.Pop();

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
        var universalisUrl = "https://universalis.app";
        if (this.selectedItem != null)
        {
          universalisUrl += $"/market/{this.selectedItem.RowId}";
        }

        Utilities.OpenBrowser(universalisUrl);
      }

      ImGui.SameLine(ImGui.GetContentRegionAvail().X - (120 * scale));

      if (!this.plugin.Config.KofiHidden)
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

      return;
    }

    internal void ChangeSelectedItem(uint itemId, bool noHistory = false)
    {
      this.selectedItem = this.items.Single(i => i.RowId == itemId);

      var iconId = this.selectedItem.Icon;
      this.selectedItemIcon?.Dispose();
      this.selectedItemIcon = MBPlugin.TextureProvider.GetIcon(iconId);

      this.RefreshMarketData();
      if (!noHistory)
      {
        this.plugin.Config.History.RemoveAll(i => i == itemId);
        this.plugin.Config.History.Insert(0, itemId);
        if (this.plugin.Config.History.Count > 100)
        {
          this.plugin.Config.History.RemoveRange(100, this.plugin.Config.History.Count - 100);
        }

        MBPlugin.PluginInterface.SavePluginConfig(this.plugin.Config);
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
        this.selectedItemIcon?.Dispose();
        this.titleFontHandle?.Dispose();
      }

      this.isDisposed = true;
    }

    private static string PadNumbers(string input)
    {
      return Regex.Replace(input, "[0-9]+", match => match.Value.PadLeft(10, '0'));
    }

    private static string ConvertItemNameToSortableFormat(string itemName)
    {
      Regex regex = new Regex(@"^[IVX]+$");
      foreach (var word in itemName.Split(' '))
      {
        if (word.Length <= 4 && regex.IsMatch(word))
        {
          int value = 0;
          for (int index = word.Length - 1, lastValue = 0; index >= 0; index--)
          {
            int currentValue = romanNumberMap[word[index]];
            value += currentValue < lastValue ? -currentValue : currentValue;
            lastValue = currentValue;
          }

          return PadNumbers(itemName.Replace(word, value.ToString(CultureInfo.CurrentCulture), StringComparison.CurrentCulture));
        }
      }

      return itemName;
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
        MBPlugin.Log.Information("{0}={1}", name, value);
      }
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

          sortedCategoriesDict.Add(c, this.items.Where(i => i.ItemSearchCategory.Row == c.RowId).OrderBy(i => ConvertItemNameToSortableFormat(i.Name.ToString())).ToList());
        }

        return sortedCategoriesDict;
      }
      catch (Exception ex)
      {
        MBPlugin.Log.Error(ex, $"Error loading category list.");
        return null;
      }
    }

    private void HandleFrameworkUpdateEvent(IFramework framework)
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

        if (this.plugin.Config.CrossDataCenter)
        {
          this.selectedWorld = 0;
        }
        else if (this.plugin.Config.CrossWorld)
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
      if (!this.plugin.Config.WatchForHovered || this.itemBeingHovered == itemId)
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

    private void RefreshMarketData()
    {
      Task.Run(async () =>
      {
        var cachedItem = this.marketBuffer.FirstOrDefault(data => data.ItemId == this.selectedItem.RowId, null);
        if (cachedItem != null)
        {
          this.marketData = cachedItem;
        }

        if (cachedItem == null || this.selectedWorld != this.previousSelectedWorld || DateTimeOffset.Now.ToUnixTimeMilliseconds() - cachedItem.FetchTimestamp > this.plugin.Config.ItemRefreshTimeout)
        {
          this.previousSelectedWorld = this.selectedWorld;
          if (cachedItem == null)
          {
            if (this.marketData != null)
            {
              this.marketBuffer.Add(this.marketData);
            }

            if (this.marketBuffer.Count > this.plugin.Config.MarketBufferSize)
            {
              this.marketBuffer.RemoveAt(0);
            }

            this.marketData = null;
          }

          this.marketData = await UniversalisClient
            .GetMarketData(
              this.selectedItem.RowId,
              this.worldList[this.selectedWorld].Item1,
              50,
              CancellationToken.None)
            .ConfigureAwait(false);

          if (cachedItem != null)
          {
            this.marketBuffer.Remove(cachedItem);
            this.marketBuffer.Add(this.marketData);
          }
        }
      });
    }
  }
}
