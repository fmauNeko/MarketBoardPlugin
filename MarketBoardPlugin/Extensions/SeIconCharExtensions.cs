// <copyright file="SeIconCharExtensions.cs" company="MTVirux">
// Copyright (c) MTVirux. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Extensions
{
  using System;
  using System.Globalization;

  using Dalamud.Game.Text;

  /// <summary>
  /// <see cref="SeIconChar"/> extensions.
  /// </summary>
  public static class SeIconCharExtensions
  {
    /// <summary>
    /// Converts a <see cref="SeIconChar"/> member to a string.
    /// </summary>
    /// <param name="iconChar">A <see cref="SeIconChar"/>.</param>
    /// <returns>A string containing the icon.</returns>
    public static string AsString(this SeIconChar iconChar)
    {
      return Convert.ToChar(iconChar, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

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
