// <copyright file="MBPlugin.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin
{
  using System;
  using System.Diagnostics.CodeAnalysis;

  using Dalamud.Game.Command;
  using Dalamud.Plugin;
  using Dalamud.Data;
  using Dalamud.Game;
  using Dalamud.Game.ClientState;
  using Dalamud.Game.Gui;
  using Dalamud.IoC;
  using MarketBoardPlugin.GUI;

  /// <summary>
  /// The entry point of the plugin.
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Plugin entry point")]
  public class MBPlugin : IDalamudPlugin
  {
    private bool isDisposed;

    private MarketBoardWindow marketBoardWindow;

    [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static DataManager Data { get; private set; } = null!;
    [PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static Framework Framework { get; private set; } = null!;
    [PluginService] internal static ClientState ClientState { get; private set; } = null!;
    [PluginService] internal static GameGui GameGui { get; private set; } = null!;

    private MBPluginConfig config;

    /// <inheritdoc/>
    public string Name => "Market Board plugin";

    /// <inheritdoc/>
    public MBPlugin()
    {
      this.config = (MBPluginConfig)PluginInterface.GetPluginConfig() ?? new MBPluginConfig();

      this.marketBoardWindow = new MarketBoardWindow(this.config);

      // Set up command handlers
      CommandManager.AddHandler("/pmb", new CommandInfo(this.OnOpenMarketBoardCommand)
      {
        HelpMessage = "Open the market board window.",
      });

      PluginInterface.UiBuilder.Draw += this.BuildMarketBoardUi;

      #if DEBUG
      this.marketBoardWindow.IsOpen = true;
      #endif
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
        // Save config
        PluginInterface.SavePluginConfig(this.config);

        // Remove command handlers
        PluginInterface.UiBuilder.Draw -= this.BuildMarketBoardUi;
        CommandManager.RemoveHandler("/pmb");
        PluginInterface.Dispose();
        this.marketBoardWindow.Dispose();
      }

      this.isDisposed = true;
    }

    private void OnOpenMarketBoardCommand(string command, string arguments)
    {
      if (!string.IsNullOrEmpty(arguments))
      {
        if (uint.TryParse(arguments, out var itemId)) {
          this.marketBoardWindow.ChangeSelectedItem(itemId);
        } else {
          this.marketBoardWindow.SearchString = arguments;
          this.marketBoardWindow.IsOpen = true;
        }
      }
      else
      {
        this.marketBoardWindow.IsOpen = !this.marketBoardWindow.IsOpen;
      }
    }

    private void BuildMarketBoardUi()
    {
      if (this.marketBoardWindow != null && this.marketBoardWindow.IsOpen)
      {
        this.marketBoardWindow.IsOpen = this.marketBoardWindow.Draw();
      }
    }
  }
}
