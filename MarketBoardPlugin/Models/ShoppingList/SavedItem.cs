// <copyright file="SavedItem.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Models.ShoppingList
{
  using Lumina.Excel.Sheets;

  /// <summary>
  /// A model representing an Item saved into the shopping list.
  /// </summary>
  public class SavedItem
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SavedItem"/> class.
    /// </summary>
    /// <param name="sourceItem"> Item class to save.</param>
    /// <param name="price"> Current cheapest price.</param>
    /// <param name="world"> Current world. </param>
    public SavedItem(Item sourceItem, double price, string world)
    {
      this.SourceItem = sourceItem;
      this.Price = price;
      this.World = world;
    }

    /// <summary>
    ///  Gets or sets original Item Class.
    /// </summary>
    public Item SourceItem { get; set; }

    /// <summary>
    ///  Gets or sets Cheapest price of the item saved.
    /// </summary>
    public double Price { get; set; }

    /// <summary>
    ///  Gets or sets world from where the price attribute was fetched.
    /// </summary>
    public string World { get; set; }
  }
}
