using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Utils;

GameConfigLoader.Load();

Logger.Info("Launcher", "=== 指尖江湖2 服务端启动 ===");
Logger.Info("Launcher", "All TCP microservices starting...");

// 启动所有TCP微服务
_ = ServiceHost.RunAsync(
    new Jx3.Login.LoginServer(),
    new Jx3.Hero.HeroServer(),
    new Jx3.Trade.TradeServer(),
    new Jx3.Battle.BattleServer(),
    new Jx3.Dungeon.DungeonServer(),
    new Jx3.Chat.ChatServer(),
    new Jx3.Social.SocialServer(),
    new Jx3.PVP.PvpServer(),
    new Jx3.Shop.ShopServer(),
    new Jx3.Quest.QuestServer(),
    new Jx3.Gateway.GatewayServer()
);

Logger.Info("Launcher", "=== All services started! ===");
Logger.Info("Launcher", $"Admin panel: http://localhost:{GameConfig.AdminPort}  (run separately)");
Logger.Info("Launcher", $"Gateway TCP: port {GameConfig.GatewayPort}");
Logger.Info("Launcher", "Press Ctrl+C to stop");

await Task.Delay(-1);