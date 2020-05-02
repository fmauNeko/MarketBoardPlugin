// <copyright file="MBPlugin.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Net.Http;
  using System.Threading.Tasks;
  using System.Web;

  using Dalamud.Game.Command;
  using Dalamud.Plugin;

  using Lumina.Excel.GeneratedSheets;

  using MarketBoardPlugin.GUI;
  using MarketBoardPlugin.Models.Universalis;

  using Newtonsoft.Json;

  /// <summary>
  /// The entry point of the plugin.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Plugin entry point")]
  public class MBPlugin : IDalamudPlugin
  {
    private bool isDisposed;

    private bool isImguiMarketBoardWindowOpen = true;

    private MarketBoardWindow marketBoardWindow;

    private DalamudPluginInterface pluginInterface;

    /// <inheritdoc/>
    public string Name => "Market Board plugin";

    /// <inheritdoc/>
    public void Initialize(DalamudPluginInterface pluginInterface)
    {
      this.pluginInterface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface));

      this.marketBoardWindow = new MarketBoardWindow(this.pluginInterface);

      // Set up command handlers
      pluginInterface.CommandManager.AddHandler("/mb", new CommandInfo(this.OnOpenMarketBoardCommand)
      {
        HelpMessage = "Open the market board window.",
      });

      pluginInterface.UiBuilder.OnBuildUi += this.BuildMarketBoardUi;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
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
        // Remove command handlers
        this.pluginInterface.UiBuilder.OnBuildUi -= this.BuildMarketBoardUi;
        this.pluginInterface.CommandManager.RemoveHandler("/mb");
        this.pluginInterface.Dispose();
        this.marketBoardWindow.Dispose();
      }

      this.isDisposed = true;
    }

    private void OnOpenMarketBoardCommand(string command, string arguments)
    {
      this.isImguiMarketBoardWindowOpen = true;
    }

    private void BuildMarketBoardUi()
    {
      if (this.isImguiMarketBoardWindowOpen)
      {
        this.isImguiMarketBoardWindowOpen = this.marketBoardWindow != null && this.marketBoardWindow.Draw();
      }
    }
  }
}
