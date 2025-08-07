// <copyright file="Utilities.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Diagnostics;
  using System.Runtime.CompilerServices;
  using Dalamud.Bindings.ImGui;
  using Dalamud.Interface.Utility.Raii;

  /// <summary>
  /// Utilities.
  /// </summary>
  internal static class Utilities
  {
    public static bool Checkbox(string label, string description, bool current, Action<bool> setter, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
      var tmp = current;
      var result = ImGui.Checkbox(label, ref tmp);
      HoverTooltip(description, flags);
      if (!result || tmp == current)
      {
        return false;
      }

      setter(tmp);
      return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HoverTooltip(string tooltip, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
      if (tooltip.Length > 0 && ImGui.IsItemHovered(flags))
      {
        using var tt = ImRaii.Tooltip();
        ImGui.TextUnformatted(tooltip);
      }
    }

    internal static void OpenBrowser(string url)
    {
      Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
  }
}
