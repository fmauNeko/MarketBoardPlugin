// <copyright file="SeIconCharExtensions.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Globalization;

  using Dalamud.Game.Chat;

  /// <summary>
  /// <see cref="SeIconChar"/> extensions.
  /// </summary>
  public static class SeIconCharExtensions
  {
    /// <summary>
    /// Converts a <see cref="SeIconChar"/> member to its char value.
    /// </summary>
    /// <param name="iconChar">A <see cref="SeIconChar"/>.</param>
    /// <returns>A char value.</returns>
    public static char ToChar(this SeIconChar iconChar)
    {
      return Convert.ToChar(iconChar, CultureInfo.InvariantCulture);
    }
  }
}
