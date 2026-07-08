namespace Jx3.Common.Config;

/// <summary>游戏全局配置</summary>
public static class GameConfig
{
    public static int GatewayPort { get; set; } = 9000;
    public static string MySQLConn { get; set; } = "server=127.0.0.1;port=3306;database=jx3;user=root;password=123456;";
    public static string RedisConn { get; set; } = "127.0.0.1:6379";
    public static int TradeFeePercent { get; set; } = 5;      // 交易手续费5%
    public static int RecruitPityMax { get; set; } = 90;       // 招募保底90抽
    public static int TeamMaxMembers { get; set; } = 8;        // 最大组队人数
}