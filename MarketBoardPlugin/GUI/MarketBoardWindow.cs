// <copyright file="MarketBoardWindow.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.GUI
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Numerics;
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

  using Item = Dalamud.Data.TransientSheet.Item;

  /// <summary>
  /// The market board window.
  /// </summary>
  public class MarketBoardWindow : IDisposable
  {
    private readonly List<Item> items;

    private readonly DalamudPluginInterface pluginInterface;

    private readonly Dictionary<ItemSearchCategory, List<Item>> sortedCategoriesAndItems;

    private CancellationTokenSource hoveredItemChangeTokenSource;
    private bool isDisposed;

    private bool itemIsBeingHovered;

    private float progressPosition;

    private string searchString = string.Empty;

    private Item selectedItem;

    private TextureWrap selectedItemIcon;

    private bool watchingForHoveredItem = true;

    private ulong playerId = 0;

    private List<(string, string)> worldList = new List<(string, string)>();

    private int selectedWorld = -1;

    private MarketDataResponse marketData;

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

      this.items = pluginInterface.Data.GetExcelSheet<Item>().GetRows();
      this.pluginInterface = pluginInterface;
      this.sortedCategoriesAndItems = this.SortCategoriesAndItems();

      pluginInterface.Framework.OnUpdateEvent += this.HandleFrameworkUpdateEvent;
      pluginInterface.Framework.Gui.HoveredItemChanged += this.HandleHoveredItemChange;
    }

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
      var enumerableCategoriesAndItems = this.sortedCategoriesAndItems.ToList();

      if (!string.IsNullOrWhiteSpace(this.searchString))
      {
        enumerableCategoriesAndItems = enumerableCategoriesAndItems
          .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(
            kv.Key,
            kv.Value
              .Where(i =>
                i.Name.ToUpperInvariant().Contains(this.searchString.ToUpperInvariant()))
              .ToList()))
          .Where(kv => kv.Value.Count > 0)
          .ToList();
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

      ImGui.BeginChild("itemTree", new Vector2(0, -2.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar);

      foreach (var category in enumerableCategoriesAndItems)
      {
        if (ImGui.TreeNode(category.Key.Name + "##cat" + category.Key.RowId))
        {
          ImGui.Unindent(ImGui.GetTreeNodeToLabelSpacing());

          foreach (var item in category.Value)
          {
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

      ImGui.Checkbox("Watch for hovered item?", ref this.watchingForHoveredItem);

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
        ImGui.Image(this.selectedItemIcon.ImGuiHandle, new Vector2(40, 40));
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetFontSize() / 2.0f) + 19);
        ImGui.Text(this.selectedItem?.Name);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 250);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetFontSize() / 2.0f) - 19);
        ImGui.SetNextItemWidth(250);
        ImGui.Combo(
          "##worldCombo",
          ref this.selectedWorld,
          this.worldList.Select(w => w.Item2).ToArray(),
          this.worldList.Count);

        if (ImGui.BeginTabBar("tabBar"))
        {
          if (ImGui.BeginTabItem("Market Data##marketDataTab"))
          {
            ImGui.Text("This is the Avocado tab!\nblah blah blah blah blah");
            ImGui.EndTabItem();
          }

          if (ImGui.BeginTabItem("Price History##priceHistoryTab"))
          {
            ImGui.Text("This is the Broccoli tab!\nblah blah blah blah blah");
            ImGui.EndTabItem();
          }

          ImGui.EndTabBar();
        }
      }

      ImGui.EndChild();
      ImGui.End();

      return windowOpen;
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
        this.hoveredItemChangeTokenSource?.Dispose();
        this.selectedItemIcon?.Dispose();
      }

      this.isDisposed = true;
    }

    private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
    {
      var itemSearchCategories = this.pluginInterface.Data.GetExcelSheet<ItemSearchCategory>().GetRows();

      var sortedCategories = itemSearchCategories
        .Where(c => c.Category > 0)
        .OrderBy(c => c.Category)
        .ThenBy(c => c.Order)
        .ToDictionary(c => c, c => this.items.Where(i => i.ItemSearchCategory == c.RowId).OrderBy(i => i.Name).ToList());

      return sortedCategories;
    }

    private void ChangeSelectedItem(int itemId)
    {
      this.selectedItem = this.items.Single(i => i.RowId == itemId);

      var iconId = this.selectedItem.Icon;
      var iconTexFile = this.pluginInterface.Data.GetIcon(iconId);
      this.selectedItemIcon?.Dispose();
      this.selectedItemIcon = this.pluginInterface.UiBuilder.LoadImageRaw(iconTexFile.GetRgbaImageData(), iconTexFile.Header.Width, iconTexFile.Header.Height, 4);

      this.RefreshMarketData();
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

        var currentDc = this.pluginInterface.Data.GetExcelSheet<WorldDCGroupType>()
          .GetRow(localPlayer.CurrentWorld.GameData.DataCenter);
        var dcWorlds = this.pluginInterface.Data.GetExcelSheet<World>().GetRows()
          .Where(w => w.DataCenter == currentDc.RowId)
          .OrderBy(w => w.Name)
          .Select(w =>
          {
            var displayName = w.Name;

            if (localPlayer.CurrentWorld.Id == w.RowId)
            {
              displayName += $" {SeIconChar.Hyadelyn.ToChar()}";
            }

            return (w.Name, displayName);
          });

        this.worldList.Clear();
        this.worldList.Add((currentDc.Name, $"Cross-World {SeIconChar.CrossWorld.ToChar()}"));
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

      Task.Run(async () =>
      {
        try
        {
          await Task.Delay(1000, this.hoveredItemChangeTokenSource.Token).ConfigureAwait(false);
          this.ChangeSelectedItem(Convert.ToInt32(itemId >= 1000000 ? itemId - 1000000 : itemId));
        }
        catch (TaskCanceledException)
        {
        }
      });
    }

    private void RefreshMarketData()
    {
      Task.Run(async () =>
      {
        this.marketData = await UniversalisClient
          .GetMarketData(this.selectedItem.RowId, this.worldList[this.selectedWorld].Item1, CancellationToken.None)
          .ConfigureAwait(false);
      });
    }
  }
}
