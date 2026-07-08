using System.Text.Json;
using Jx3.Common.Utils;

namespace Jx3.Common.Config;

public class GameConfigLoader
{
    public ServerConfig? Server { get; set; }
    public DatabaseConfig? Database { get; set; }
    public GameSettings? Game { get; set; }

    public static GameConfigLoader Load(string path = "appsettings.json")
    {
        if (!File.Exists(path))
        {
            Logger.Warn("Config", $"Config file not found: {path}, using defaults");
            return new GameConfigLoader
            {
                Server = new ServerConfig(),
                Database = new DatabaseConfig(),
                Game = new GameSettings()
            };
        }
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<GameConfigLoader>(json) ?? new();
        config.Server ??= new();
        config.Database ??= new();
        config.Game ??= new();

        // 同步到 GameConfig 静态字段
        GameConfig.GatewayPort = config.Server.GatewayPort;
        GameConfig.MySQLConn = config.Database.MySQL;
        GameConfig.RedisConn = config.Database.Redis;
        GameConfig.TradeFeePercent = config.Game.TradeFeePercent;
        GameConfig.RecruitPityMax = config.Game.RecruitPityMax;
        GameConfig.TeamMaxMembers = config.Game.TeamMaxMembers;

        Logger.Info("Config", $"Loaded config: Gateway={config.Server.GatewayPort}");
        return config;
    }
}

public class ServerConfig
{
    public int GatewayPort { get; set; } = 9000;
    public int LoginPort { get; set; } = 9001;
    public int HeroPort { get; set; } = 9002;
    public int TradePort { get; set; } = 9003;
}

public class DatabaseConfig
{
    public string MySQL { get; set; } = "server=127.0.0.1;port=3306;database=jx3;user=root;password=123456;";
    public string Redis { get; set; } = "127.0.0.1:6379";
}

public class GameSettings
{
    public int TradeFeePercent { get; set; } = 5;
    public int RecruitPityMax { get; set; } = 90;
    public int TeamMaxMembers { get; set; } = 8;
    public int MaxHeroLevel { get; set; } = 100;
    public int MaxHeroStar { get; set; } = 6;
}