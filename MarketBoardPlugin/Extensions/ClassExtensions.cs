// <copyright file="ClassExtensions.cs" company="MTVirux">
// Copyright (c) MTVirux. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Extensions
{
  using Lumina.Excel.Sheets;

  /// <summary>
  /// <see cref="ClassJobCategory"/> and <see cref="ClassJob"/> extensions.
  /// </summary>
  public static class ClassExtensions
  {
    /// <summary>
    /// Checks if <see cref="ClassJobCategory"/> contains <see cref="ClassJob"/>.
    /// </summary>
    /// <param name="classJobCategory">A <see cref="ClassJobCategory"/>.</param>
    /// <param name="classJob">A <see cref="ClassJob"/>.</param>
    /// <returns>
    /// True if contained or classJob is null.
    /// False if not contained.
    /// </returns>
    public static bool HasClass(this ClassJobCategory classJobCategory, ClassJob? classJob)
    {
      if (!classJob.HasValue)
      {
        return true;
      }

      return classJob.Value.RowId switch
      {
        0 => classJobCategory.ADV,
        1 => classJobCategory.GLA,
        2 => classJobCategory.PGL,
        3 => classJobCategory.MRD,
        4 => classJobCategory.LNC,
        5 => classJobCategory.ARC,
        6 => classJobCategory.CNJ,
        7 => classJobCategory.THM,
        8 => classJobCategory.CRP,
        9 => classJobCategory.BSM,
        10 => classJobCategory.ARM,
        11 => classJobCategory.GSM,
        12 => classJobCategory.LTW,
        13 => classJobCategory.WVR,
        14 => classJobCategory.ALC,
        15 => classJobCategory.CUL,
        16 => classJobCategory.MIN,
        17 => classJobCategory.BTN,
        18 => classJobCategory.FSH,
        19 => classJobCategory.PLD,
        20 => classJobCategory.MNK,
        21 => classJobCategory.WAR,
        22 => classJobCategory.DRG,
        23 => classJobCategory.BRD,
        24 => classJobCategory.WHM,
        25 => classJobCategory.BLM,
        26 => classJobCategory.ACN,
        27 => classJobCategory.SMN,
        28 => classJobCategory.SCH,
        29 => classJobCategory.ROG,
        30 => classJobCategory.NIN,
        31 => classJobCategory.MCH,
        32 => classJobCategory.DRK,
        33 => classJobCategory.AST,
        34 => classJobCategory.SAM,
        35 => classJobCategory.RDM,
        36 => classJobCategory.BLU,
        37 => classJobCategory.GNB,
        38 => classJobCategory.DNC,
        39 => classJobCategory.RPR,
        40 => classJobCategory.SGE,
        _ => false,
      };
    }
  }
}
