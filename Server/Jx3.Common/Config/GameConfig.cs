namespace Jx3.Common.Config;

/// <summary>游戏全局配置</summary>
public static class GameConfig
{
    public static string GameVersion { get; set; } = "0.2.0";
    public static int AdminPort { get; set; } = 9100;
    public static int BattlePort { get; set; } = 9004;
    public static int ChatPort { get; set; } = 9005;
    public static int ShopPort { get; set; } = 9006;
    public static int PvpPort { get; set; } = 9007;
    public static int GatewayPort { get; set; } = 9000;
    public static string MySQLConn { get; set; } = "server=127.0.0.1;port=3306;database=jx3;user=root;password=123456;";
    public static string RedisConn { get; set; } = "127.0.0.1:6379";
    public static int TradeFeePercent { get; set; } = 5;      // 交易手续费5%
    public static int RecruitPityMax { get; set; } = 90;       // 招募保底90抽
    public static int TeamMaxMembers { get; set; } = 8;        // 最大组队人数
}