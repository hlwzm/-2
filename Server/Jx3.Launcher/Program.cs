using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Utils;
using Jx3.Common.Service;

// 加载配置
GameConfigLoader.Load();

Logger.Info("Launcher", "=== 指尖江湖2 服务端启动 ===");
Logger.Info("Launcher", $"Version: {GameConfig.GameVersion}");
Logger.Info("Launcher", $"MySQL: {GameConfig.MySQLConn}");
Logger.Info("Launcher", $"Redis: {GameConfig.RedisConn}");
Logger.Info("Launcher", "");

// 启动所有微服务
await ServiceHost.RunAsync(
    new Jx3.Login.LoginServer(),
    new Jx3.Hero.HeroServer(),
    new Jx3.Trade.TradeServer(),
    new Jx3.Battle.BattleServer(),
    new Jx3.Dungeon.DungeonServer(),
    new Jx3.Chat.ChatServer(),
    new Jx3.Social.SocialServer(),
    new Jx3.PVP.PvpServer(),
    new Jx3.Shop.ShopServer(),
    new Jx3.Quest.QuestServer()
);

// Gateway需要最后启动（依赖其他服务先注册）
_ = Task.Run(async () => await new Jx3.Gateway.GatewayServer().StartAsync());

// Admin Web API单独进程
Logger.Info("Launcher", "");
Logger.Info("Launcher", "=== 所有微服务已启动 ===");
Logger.Info("Launcher", $"Admin Panel: http://localhost:{GameConfig.AdminPort}");
Logger.Info("Launcher", $"Gateway TCP: port {GameConfig.GatewayPort}");
Logger.Info("Launcher", "按 Ctrl+C 停止所有服务");

await Task.Delay(-1);