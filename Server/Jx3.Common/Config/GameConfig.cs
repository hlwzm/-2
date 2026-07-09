namespace Jx3.Common.Config;

/// <summary>游戏全局配置</summary>
public static class GameConfig
{
    public static string GameVersion { get; set; } = "0.2.0";

    // Ports
    public static int GatewayPort { get; set; } = 9000;
    public static int LoginPort { get; set; } = 9001;
    public static int HeroPort { get; set; } = 9002;
    public static int TradePort { get; set; } = 9003;
    public static int BattlePort { get; set; } = 9004;
    public static int DungeonPort { get; set; } = 9005;
    public static int ChatPort { get; set; } = 9006;
    public static int SocialPort { get; set; } = 9007;
    public static int PvpPort { get; set; } = 9008;
    public static int ShopPort { get; set; } = 9009;
    public static int QuestPort { get; set; } = 9010;
    public static int AdminPort { get; set; } = 9100;

    // Database
    public static string MySQLConn { get; set; } = "";
    public static string RedisConn { get; set; } = "";

    // Game settings
    public static int TradeFeePercent { get; set; } = 5;
    public static int RecruitPityMax { get; set; } = 90;
    public static int TeamMaxMembers { get; set; } = 8;
    public static int MaxHeroLevel { get; set; } = 100;
    public static int MaxHeroStar { get; set; } = 6;
}