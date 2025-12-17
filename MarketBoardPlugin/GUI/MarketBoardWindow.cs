// <copyright file="MarketBoardWindow.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.GUI
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Numerics;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Threading;
  using System.Threading.Tasks;
  using Dalamud.Bindings.ImGui;
  using Dalamud.Bindings.ImPlot;
  using Dalamud.Game.Text;
  using Dalamud.Interface;
  using Dalamud.Interface.ManagedFontAtlas;
  using Dalamud.Interface.Textures;
  using Dalamud.Interface.Windowing;
  using Dalamud.Plugin.Services;
  using Lumina.Excel.Sheets;
  using Lumina.Extensions;
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

    private readonly List<ClassJob> classJobs = new();

    private readonly IFontHandle defaultFontHandle;

    private readonly IFontHandle titleFontHandle;

    private readonly Dictionary<uint, MarketDataResponse> marketDataCache;

    private readonly string[] categoryLabels = new[] { "All", "Weapons", "Equipments", "Others", "Furniture" };

    private readonly CancellationTokenSource statusCheckCancellationTokenSource = new();

    private Dictionary<ItemSearchCategory, List<Item>> sortedCategoriesAndItems;

    private bool isDisposed;

    private ulong itemBeingHovered;

    private bool searchHistoryOpen;

    private bool advancedSearchMenuOpen;

    private bool favoritesOpen;

    private float progressPosition;

    private string searchString = string.Empty;
    private string lastSearchString = string.Empty;

    private ClassJob? lastSelectedClassJob;

    private int lvlmin;
    private int lastlvlmin;
    private int lvlmax = 100;
    private int lastlvlmax = 100;
    private int itemCategory;
    private int lastItemCategory;
    private Item? selectedItem;

    private ClassJob? selectedClassJob;

    private bool hQOnly;

    private ulong playerId;

    private int minQuantityFilter;

    private int selectedWorld = -1;

    private MarketDataResponse? marketData;

    private int selectedListing = -1;

    private int selectedHistory = -1;

    private bool hasListingsHQColumnWidthBeenSet;

    private bool hasHistoryHQColumnWidthBeenSet;

    private List<KeyValuePair<ItemSearchCategory, List<Item>>> enumerableCategoriesAndItems = new();

    private Task? currentRefreshTask;

    private CancellationTokenSource? currentRefreshCancellationTokenSource;

    private bool isUniversalisUp;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardWindow"/> class.
    /// </summary>
    /// <param name="plugin">The <see cref="MBPlugin"/>.</param>
    public MarketBoardWindow(MBPlugin plugin)
      : base("Market Board")
    {
      this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
      this.Flags = ImGuiWindowFlags.NoScrollbar;
      this.Size = new Vector2(800, 600);
      this.SizeCondition = ImGuiCond.FirstUseEver;
      this.SizeConstraints = new WindowSizeConstraints
      {
        MinimumSize = new Vector2(350, 225),
        MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
      };

      this.marketDataCache = [];
      this.items = plugin.DataManager.GetExcelSheet<Item>();
      this.classJobs = plugin.DataManager.GetExcelSheet<ClassJob>()?
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
        }).ToList() ?? new List<ClassJob>();
      this.sortedCategoriesAndItems = this.SortCategoriesAndItems();
      this.enumerableCategoriesAndItems = this.sortedCategoriesAndItems
        .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(kv.Key, kv.Value))
        .ToList();

      this.plugin.Framework.Update += this.HandleFrameworkUpdateEvent;
      this.plugin.GameGui.HoveredItemChanged += this.HandleHoveredItemChange;

      this.defaultFontHandle = this.plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        e.OnPreBuild(toolkit =>
        {
          var fontStream = this.GetType().Assembly.GetManifestResourceStream("MarketBoardPlugin.Resources.NotoSans-Medium-NNBSP.otf");

          if (fontStream == null)
          {
            this.plugin.Log.Warning("Failed to load embedded font MarketBoardPlugin.Resources.NotoSans-Medium-NNBSP.otf");
            return;
          }

          toolkit.AddFontFromStream(
            fontStream,
            new SafeFontConfig()
            {
              SizePx = UiBuilder.DefaultFontSizePx,
              GlyphRanges = FontAtlasBuildToolkitUtilities.ToGlyphRange(char.ConvertFromUtf32(0x202F)),
              MergeFont = toolkit.AddDalamudDefaultFont(-1),
            },
            false,
            "NNBSP");
        }));

      this.titleFontHandle = this.plugin.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        e.OnPreBuild(toolkit =>
          toolkit.AddDalamudDefaultFont(this.plugin.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * 1.5f)));

      var imPlotStylePtr = ImPlot.GetStyle();

      imPlotStylePtr.Use24HourClock = DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains('H', StringComparison.InvariantCulture);
      imPlotStylePtr.UseISO8601 = DateTimeFormatInfo.CurrentInfo.ShortDatePattern != "M/d/yyyy";
      imPlotStylePtr.UseLocalTime = true;

#if DEBUG
      this.worldList.Add(("Chaos", "Chaos"));
      this.worldList.Add(("Moogle", "Moogle"));
#endif

      this.StartUniversalisStatusCheckTask(this.statusCheckCancellationTokenSource.Token);
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
      this.marketDataCache.Clear();
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
                                                     || this.lastSelectedClassJob?.RowId != this.selectedClassJob?.RowId)
      {
        this.UpdateCategoriesAndItems();
      }

      var defaultButtonColor = ImGui.GetColorU32(ImGuiCol.Button);
      var defaultButtonHoveredColor = ImGui.GetColorU32(ImGuiCol.ButtonHovered);

      var scale = ImGui.GetIO().FontGlobalScale;

      using var fontDispose = this.defaultFontHandle.Push();

      // Item List Column Setup
      ImGui.BeginChild("itemListColumn", new Vector2(267, 0) * scale, true);

      ImGui.SetNextItemWidth((-64 * ImGui.GetIO().FontGlobalScale) - (ImGui.GetStyle().ItemSpacing.X * 2));
      ImGui.InputTextWithHint("##searchString", "Search for item", ref this.searchString, 256);

      ImGui.PushFont(UiBuilder.IconFont);
      ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

      ImGui.SameLine();
      ImGui.PushStyleColor(ImGuiCol.Button, this.searchHistoryOpen ? 0xFF5CB85C : defaultButtonColor);
      ImGui.PushStyleColor(ImGuiCol.ButtonHovered, this.searchHistoryOpen ? 0x885CB85C : defaultButtonHoveredColor);
      if (ImGui.Button($"{(char)FontAwesomeIcon.History}", new Vector2(32 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
      {
        this.searchHistoryOpen = !this.searchHistoryOpen;
      }

      ImGui.PopStyleColor();
      ImGui.PopStyleColor();

      ImGui.SameLine();
      ImGui.PushStyleColor(ImGuiCol.Button, this.favoritesOpen ? 0xFF5CB85C : defaultButtonColor);
      ImGui.PushStyleColor(ImGuiCol.ButtonHovered, this.favoritesOpen ? 0x885CB85C : defaultButtonHoveredColor);
      if (ImGui.Button($"{(char)FontAwesomeIcon.Star}", new Vector2(32 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
      {
        this.favoritesOpen = !this.favoritesOpen;
      }

      ImGui.PopStyleColor();
      ImGui.PopStyleColor();

      ImGui.PopStyleVar();
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
          this.selectedClassJob == null ? "All Classes" : this.selectedClassJob.Value.Abbreviation.ExtractText()))
        {
          void SelectClassJob(ClassJob? classJob)
          {
            var selected = this.selectedClassJob?.RowId == classJob?.RowId;
            if (ImGui.Selectable(classJob == null ? "All Classes" : classJob?.Abbreviation.ExtractText(), selected))
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
          this.lvlmax = 100;
        }
      }

      ImGui.Separator();
      ImGui.BeginChild("itemTree", new Vector2(0, -2.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
      var itemTextSize = ImGui.CalcTextSize(string.Empty);

      if (this.searchHistoryOpen)
      {
        ImGui.Text("History");
        ImGui.Separator();
        var sheet = this.plugin.DataManager.Excel.GetSheet<Item>();
        foreach (var id in this.plugin.Config.History.ToArray())
        {
          var item = sheet.GetRowOrDefault(id);
          if (!item.HasValue)
          {
            continue;
          }

          if (ImGui.Selectable($"{item.Value.Name.ExtractText()}", this.selectedItem?.RowId == id))
          {
            this.ChangeSelectedItem(id, true);
          }
        }
      }
      else if (this.favoritesOpen)
      {
        ImGui.Text("Favorites");
        ImGui.Separator();
        var sheet = this.plugin.DataManager.Excel.GetSheet<Item>();
        foreach (var id in this.plugin.Config.Favorites.ToArray())
        {
          var item = sheet.GetRowOrDefault(id);
          if (!item.HasValue)
          {
            continue;
          }

          var itemName = item.Value.Name.ExtractText();

          if (ImGui.Selectable($"{itemName}", this.selectedItem?.RowId == id))
          {
            this.ChangeSelectedItem(id, true);
          }

          if (ImGui.BeginPopupContextItem($"itemContextMenu{itemName}"))
          {
            if (ImGui.Selectable("Remove from the favorites"))
            {
              this.plugin.Config.Favorites.Remove(item.Value.RowId);
            }

            ImGui.EndPopup();
          }

          ImGui.OpenPopupOnItemClick($"itemContextMenu{itemName}", ImGuiPopupFlags.MouseButtonRight);
        }
      }
      else
      {
        foreach (var category in this.enumerableCategoriesAndItems)
        {
          if (ImGui.TreeNode(category.Key.Name.ExtractText() + "##cat" + category.Key.RowId))
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

              ImGui.TreeNodeEx(item.Name.ExtractText() + "##item" + item.RowId, nodeFlags);

              if (ImGui.IsItemClicked())
              {
                this.ChangeSelectedItem(item.RowId);
              }

              if (ImGui.BeginPopupContextItem("itemContextMenu" + category.Key.Name.ExtractText() + i))
              {
                if (this.selectedItem != null && this.selectedItem.Value.RowId != item.RowId)
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

                if (ImGui.Selectable("Add to the favorites"))
                {
                  this.plugin.Config.Favorites.Add(item.RowId);
                }

                ImGui.EndPopup();
              }

              ImGui.OpenPopupOnItemClick("itemContextMenu" + category.Key.Name.ExtractText() + i, ImGuiPopupFlags.MouseButtonRight);
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
          var itemId = this.plugin.GameGui.HoveredItem;
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
        using var selectedItemIcon = this.plugin.TextureProvider.GetFromGameIcon(new GameIconLookup
        {
          IconId = this.selectedItem.Value.Icon,
        }).GetWrapOrDefault();

        if (selectedItemIcon != null)
        {
          if (ImGui.ImageButton(selectedItemIcon.Handle, new Vector2(40, 40)))
          {
            ImGui.LogToClipboard();
            ImGui.LogText(this.selectedItem?.Name.ExtractText());
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
        ImGui.Text(this.selectedItem?.Name.ExtractText());
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
              this.selectedWorld = this.worldList.IndexOf(world);
              this.plugin.Config.CrossDataCenter = this.selectedWorld == 0;
              this.plugin.Config.CrossWorld = this.selectedWorld == 1;
              this.ResetMarketData();
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
              ImGui.SetColumnWidth(0, ImGui.GetTextLineHeightWithSpacing() * 1.5f);
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

                var retainerSB = new StringBuilder($"{listing.RetainerName} {SeIconChar.CrossWorld.ToChar()}");

                if (this.selectedWorld <= 1)
                {
                  retainerSB.Append(CultureInfo.CurrentCulture, $" {listing.WorldName}");

                  if (this.selectedWorld == 0)
                  {
                    retainerSB.Append(CultureInfo.CurrentCulture, $" @ {this.GetDcNameFromWorldId(listing.WorldID)}");
                  }
                }
                else
                {
                  retainerSB.Append(CultureInfo.CurrentCulture, $" {this.worldList[this.selectedWorld].Item1}");
                }

                ImGui.Text(retainerSB.ToString());
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
                ImGui.SetColumnWidth(0, ImGui.GetTextLineHeightWithSpacing() * 1.5f);
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

            if (this.marketData?.RecentHistory != null && this.marketData?.RecentHistory.Count > 0)
            {
              this.titleFontHandle.Push();
              ImGui.Text("Price variations (per unit)");
              this.titleFontHandle.Pop();

              if (ImPlot.BeginPlot("##pricePlot", new Vector2(-1, tableHeight)))
              {
                var now = DateTimeOffset.Now;
                var x = new List<float>();
                var y = new List<float>();

                foreach (var historyEntry in this.marketData.RecentHistory)
                {
                  x.Add(historyEntry.Timestamp);
                  y.Add(historyEntry.PricePerUnit);
                }

                ImPlot.SetupAxesLimits(now.AddDays(-7).ToUnixTimeSeconds(), now.ToUnixTimeSeconds(), 0, y.Max(), ImPlotCond.Always);
                ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
                ImPlot.SetNextMarkerStyle(ImPlotMarker.Circle);
                ImPlot.PlotLine("Price", ref x.ToArray()[0], ref y.ToArray()[0], x.Count);
                ImPlot.EndPlot();
              }

              ImGui.Separator();

              this.titleFontHandle.Push();
              ImGui.Text("Traded volumes");
              this.titleFontHandle.Pop();

              if (ImPlot.BeginPlot("##qtyPlot", new Vector2(-1, tableHeight)))
              {
                var now = DateTimeOffset.Now;
                var x = new List<float>();
                var y = new List<float>();

                foreach (var historyEntry in this.marketData.RecentHistory)
                {
                  x.Add(historyEntry.Timestamp);
                  y.Add(historyEntry.Quantity);
                }

                ImPlot.SetupAxesLimits(now.AddDays(-7).ToUnixTimeSeconds(), now.ToUnixTimeSeconds(), 0, y.Max(), ImPlotCond.Always);
                ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
                ImPlot.PlotBars("Quantities", ref x.ToArray()[0], ref y.ToArray()[0], x.Count, 3600);
                ImPlot.EndPlot();
              }
            }

            ImGui.EndTabItem();
          }

          ImGui.EndTabBar();
        }
      }

      ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight());

      if (this.isUniversalisUp)
      {
        var buttonColor = 0x002ba040u;

        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | buttonColor);

        if (ImGui.Button("Data provided by Universalis"))
        {
          var universalisUrl = "https://universalis.app";
          if (this.selectedItem != null)
          {
            universalisUrl += $"/market/{this.selectedItem.Value.RowId}";
          }

          Utilities.OpenBrowser(universalisUrl);
        }

        ImGui.PopStyleColor(3);
      }
      else
      {
        var buttonColor = 0x005345e6u;

        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | buttonColor);

        if (ImGui.Button("Universalis API seems down"))
        {
          Utilities.OpenBrowser("https://status.universalis.app");
        }

        ImGui.PopStyleColor(3);
      }

      ImGui.SameLine(ImGui.GetContentRegionAvail().X - (120 * scale));

      if (!this.plugin.Config.KofiHidden)
      {
        var buttonText = "Support on Ko-fi";
        var buttonColor = 0x005E5BFFu;
        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | buttonColor);

        if (ImGui.Button(buttonText, new Vector2(120, 0)))
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

      var iconId = this.selectedItem.Value.Icon;

      this.RefreshMarketData();
      if (!noHistory)
      {
        this.plugin.Config.History.RemoveAll(i => i == itemId);
        this.plugin.Config.History.Insert(0, itemId);
        if (this.plugin.Config.History.Count > 100)
        {
          this.plugin.Config.History.RemoveRange(100, this.plugin.Config.History.Count - 100);
        }

        this.plugin.PluginInterface.SavePluginConfig(this.plugin.Config);
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
        this.plugin.Framework.Update -= this.HandleFrameworkUpdateEvent;
        this.plugin.GameGui.HoveredItemChanged -= this.HandleHoveredItemChange;
        this.defaultFontHandle?.Dispose();
        this.titleFontHandle?.Dispose();
        this.currentRefreshCancellationTokenSource?.Cancel();
        this.currentRefreshCancellationTokenSource?.Dispose();
        this.statusCheckCancellationTokenSource.Cancel();
        this.statusCheckCancellationTokenSource.Dispose();
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
                i.Name.ExtractText().ToUpperInvariant().Contains(this.searchString.ToUpperInvariant(), StringComparison.InvariantCulture))
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
      var itemSearchCategories = this.plugin.DataManager.GetExcelSheet<ItemSearchCategory>();

      if (itemSearchCategories == null)
      {
        this.plugin.Log.Warning("Failed to load item search categories.");
        return new Dictionary<ItemSearchCategory, List<Item>>();
      }

      var sortedCategories = itemSearchCategories.Where(c => c.Category > 0).OrderBy(c => c.Category).ThenBy(c => c.Order);

      var sortedCategoriesDict = new Dictionary<ItemSearchCategory, List<Item>>();

      foreach (var c in sortedCategories)
      {
        if (sortedCategoriesDict.ContainsKey(c))
        {
          continue;
        }

        sortedCategoriesDict.Add(c, this.items.Where(i => i.ItemSearchCategory.RowId == c.RowId).OrderBy(i => ConvertItemNameToSortableFormat(i.Name.ExtractText())).ToList());
      }

      return sortedCategoriesDict;
    }

    private void HandleFrameworkUpdateEvent(IFramework framework)
    {
      if (!this.plugin.PlayerState.IsLoaded)
      {
        this.playerId = 0;
        return;
      }

      if (this.playerId != this.plugin.PlayerState.ContentId)
      {
        var currentDc = this.plugin.PlayerState.CurrentWorld.Value.DataCenter;
        var dcWorlds = this.plugin.DataManager.GetExcelSheet<World>()
          .Where(w => w.DataCenter.RowId == currentDc.RowId && w.IsPublic)
          .OrderBy(w => w.Name.ExtractText())
          .Select(w =>
          {
            string displayName = w.Name.ExtractText();

            if (this.plugin.PlayerState.CurrentWorld.Value.RowId == w.RowId)
            {
              displayName += $" {SeIconChar.Hyadelyn.ToChar()}";
            }

            return (w.Name.ExtractText(), displayName);
          });

        var regionName = this.plugin.PlayerState.HomeWorld.Value.DataCenter.Value.Region switch
        {
          1 => "Japan",
          2 => "North-America",
          3 => "Europe",
          4 => "Oceania",
          5 => "中国",
          _ => string.Empty,
        };

        this.worldList.Clear();
        this.worldList.Add((regionName, $"Cross-DC {SeIconChar.CrossWorld.ToChar()}"));
        this.worldList.Add((currentDc.Value.Name.ExtractText(), $"Cross-World {SeIconChar.CrossWorld.ToChar()}"));
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
          this.selectedWorld = this.worldList.FindIndex(w => w.Item1 == this.plugin.PlayerState.CurrentWorld.Value.Name);
        }

        if (this.worldList.Count > 1)
        {
          this.playerId = this.plugin.PlayerState.ContentId;
        }
      }
    }

    private void HandleHoveredItemChange(object? sender, ulong itemId)
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

      var item = this.plugin.DataManager.Excel.GetSheet<Item>().GetRowOrDefault((uint)itemId % 500000);

      if (item != null && this.enumerableCategoriesAndItems != null && this.sortedCategoriesAndItems.Any(i => i.Value != null && i.Value.Any(k => k.RowId == item.Value.RowId)))
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
      if (!this.selectedItem.HasValue)
      {
        return;
      }

      this.marketData = null;

      if (this.currentRefreshTask?.Status != TaskStatus.RanToCompletion)
      {
        this.plugin.Log.Debug("Cancelling previous refresh task.");
        this.currentRefreshCancellationTokenSource?.Cancel();
      }

      this.currentRefreshCancellationTokenSource?.Dispose();
      this.currentRefreshCancellationTokenSource = new CancellationTokenSource();

      this.currentRefreshTask = Task.Run(
        async () =>
        {
          var cachedItem = this.marketDataCache.GetValueOrDefault(this.selectedItem.Value.RowId);
          if (
            cachedItem != default(MarketDataResponse)
            && DateTimeOffset.Now.ToUnixTimeMilliseconds() - cachedItem.FetchTimestamp < this.plugin.Config.ItemRefreshTimeout)
          {
            this.marketData = cachedItem;
            return;
          }

          this.marketDataCache.Remove(this.selectedItem.Value.RowId);

          try
          {
            this.marketData = await this.plugin.UniversalisClient
              .GetMarketData(
                this.selectedItem.Value.RowId,
                this.worldList[this.selectedWorld].Item1,
                this.plugin.Config.ListingCount,
                this.plugin.Config.HistoryCount,
                this.currentRefreshCancellationTokenSource.Token)
              .ConfigureAwait(false);

            if (this.selectedWorld == 0 && this.plugin.Config.IncludeOceaniaDC && this.worldList[this.selectedWorld].Item1 != "Oceania")
            {
              var oceaniaMarketData = await this.plugin.UniversalisClient
                .GetMarketData(
                  this.selectedItem.Value.RowId,
                  "Oceania",
                  this.plugin.Config.ListingCount,
                  this.plugin.Config.HistoryCount,
                  this.currentRefreshCancellationTokenSource.Token)
                .ConfigureAwait(false);

              if (oceaniaMarketData != null)
              {
                if (this.marketData == null)
                {
                  this.marketData = oceaniaMarketData;
                }
                else
                {
                  foreach (var listing in oceaniaMarketData.Listings)
                  {
                    this.marketData.Listings.Add(listing);
                  }

                  foreach (var history in oceaniaMarketData.RecentHistory)
                  {
                    this.marketData.RecentHistory.Add(history);
                  }

                  this.marketData.Listings = this.marketData.Listings
                    .OrderBy(l => l.PricePerUnit)
                    .Take(this.plugin.Config.ListingCount)
                    .ToList();
                  this.marketData.RecentHistory = this.marketData.RecentHistory
                    .OrderByDescending(h => h.Timestamp)
                    .Take(this.plugin.Config.HistoryCount)
                    .ToList();
                }
              }
            }
          }
          catch (AggregateException ae)
          {
            this.plugin.Log.Warning(ae, $"Failed to fetch market data for item {this.selectedItem.Value.RowId} from Universalis.");

            foreach (var ex in ae.InnerExceptions)
            {
              this.plugin.Log.Warning(ex, "Inner exception");
            }

            this.marketData = null;
          }

          if (this.marketData != null)
          {
            this.marketDataCache.Add(this.selectedItem.Value.RowId, this.marketData);
          }
        },
        this.currentRefreshCancellationTokenSource.Token);
    }

    private void StartUniversalisStatusCheckTask(CancellationToken cancellationToken)
    {
      Task.Run(
        async () =>
        {
          while (!cancellationToken.IsCancellationRequested)
          {
            this.isUniversalisUp = await this.plugin.UniversalisClient.CheckStatus(cancellationToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
          }
        },
        cancellationToken);
    }

    private string GetDcNameFromWorldId(int worldId)
    {
      var world = this.plugin.DataManager.GetExcelSheet<World>().FirstOrNull(w => w.RowId == worldId);

      if (world != null)
      {
        return world.Value.DataCenter.Value.Name.ExtractText();
      }

      return string.Empty;
    }
  }
}
