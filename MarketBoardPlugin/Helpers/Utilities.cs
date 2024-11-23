// <copyright file="Utilities.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System.Diagnostics;

  /// <summary>
  /// Utilities.
  /// </summary>
  internal static class Utilities
  {
    internal static void OpenBrowser(string url)
    {
      _ = Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
  }
}
