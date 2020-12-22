namespace MarketBoardPlugin {
  using Dalamud.Configuration;

  public class MBPluginConfig : IPluginConfiguration {
    public int Version { get; set; } = 1;

    public bool CrossWorld { get; set; } = false;
  }
}
