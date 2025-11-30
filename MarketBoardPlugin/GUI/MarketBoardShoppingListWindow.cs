// <copyright file="MarketBoardShoppingListWindow.cs" company="MTVirux">
// Copyright (c) MTVirux. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Numerics;
  using Dalamud.Bindings.ImGui;
  using Dalamud.Interface;
  using Dalamud.Interface.Windowing;
  using MarketBoardPlugin.Helpers;
  using MarketBoardPlugin.Models.ShoppingList;

  /// <summary>
  /// The market board config window.
  /// </summary>
  public class MarketBoardShoppingListWindow : Window
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardShoppingListWindow"/> class.
    /// </summary>
    /// <param name="plugin">The <see cref="MBPlugin"/>.</param>
    public MarketBoardShoppingListWindow(MBPlugin plugin)
      : base("Market Board Shopping List")
    {
      this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

      this.Flags = ImGuiWindowFlags.NoScrollbar;
      this.IsOpen = true;
      this.RespectCloseHotkey = false;
      this.ShowCloseButton = false;
      this.Size = new Vector2(400, 150);
      this.SizeCondition = ImGuiCond.FirstUseEver;
      this.SizeConstraints = new WindowSizeConstraints
      {
        MinimumSize = new Vector2(400, 150),
        MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
      };
    }

    private MBPlugin Plugin { get; init; }

    /// <inheritdoc/>
    public override bool DrawConditions() => this.Plugin.ShoppingList.Count > 0;

    /// <inheritdoc/>
    public override void Draw()
    {
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
      foreach (var item in this.Plugin.ShoppingList)
      {
        ImGui.Text(item.SourceItem.Name.ExtractText());
        ImGui.NextColumn();
        if (this.Plugin.Config.PriceIconShown)
        {
          ImGui.Text(item.Price.ToString("C", this.Plugin.NumberFormatInfo));
        }
        else
        {
          ImGui.Text(item.Price.ToString("N0", CultureInfo.CurrentCulture));
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
        this.Plugin.ShoppingList.Remove(item);
      }
    }
  }
}
