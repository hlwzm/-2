# 指尖江湖2 - Unity客户端设置指南

## 项目结构
```
Client/
├── ProjectSettings/          # Unity项目设置
├── Packages/manifest.json    # 包依赖
└── Assets/
    ├── Scripts/
    │   ├── Core/             # 核心框架
    │   │   ├── GameManager.cs      # 全局游戏管理器
    │   │   ├── Protocol.cs         # 完整MsgId枚举(148个)
    │   │   ├── AppConfig.cs        # 配置
    │   │   ├── Network/
    │   │   │   └── NetworkClient.cs # TCP客户端
    │   │   ├── Manager/            # 14个服务管理器
    │   │   │   ├── LoginManager.cs
    │   │   │   ├── BattleManager.cs
    │   │   │   ├── ChatManager.cs
    │   │   │   ├── DungeonManager.cs
    │   │   │   ├── FriendManager.cs
    │   │   │   ├── HeroManager.cs
    │   │   │   ├── HeroScreenManager.cs
    │   │   │   ├── PvpManager.cs
    │   │   │   ├── QuestManager.cs
    │   │   │   ├── ShopManager.cs
    │   │   │   ├── TeamManager.cs
    │   │   │   └── TradeManager.cs
    │   │   └── Scene/              # 场景管理
    │   ├── UI/                     # UI系统
    │   │   ├── UIRoot.cs           # Canvas层级
    │   │   ├── UIManager.cs        # 面板管理器
    │   │   ├── BasePanel.cs        # 面板基类
    │   │   ├── Panels/             # 13个功能面板
    │   │   └── Battle/             # 战斗UI
    │   └── Game/
    │       └── Bootstrapper.cs     # 游戏启动器
    ├── Art/                        # 资源目录(空)
    └── Resources/                  # 资源目录(空)

## 快速开始

### 1. 用Unity打开项目
- 启动 Unity Hub
- Open -> 选择 `Client/` 目录
- Unity 2022.3+ 推荐

### 2. 创建场景
在 Unity 中创建以下场景（File -> New Scene）：

| 场景文件名 | 挂载脚本 | 说明 |
|---|---|---|
| Boot | Bootstrapper.cs | 启动场景，自动加载登录 |
| Login | LoginSceneBoot.cs | 登录界面 |
| MainCity | MainCitySceneBoot.cs | 主城 |
| DungeonSelect | DungeonSceneBoot.cs | 副本选择 |
| Battle | BattleSceneBoot.cs | 战斗场景 |
| PVP | (预留) | PVP场景 |

### 3. 构建设置
File -> Build Settings:
- 将所有场景拖入 Scenes in Build
- Boot 场景放在第一位

### 4. UI自动生成
所有UI面板都是**程序化生成**的（代码自动创建），无需手动搭建UI预制体。
- 运行游戏后自动显示登录面板
- 登录成功后自动切换到主城
- 所有面板通过 `UIManager.Instance.Show<PanelName>()` 打开

### 5. 连接到服务器
默认连接 `127.0.0.1:9000`
- 启动服务端: `cd Server && dotnet run --project Jx3.Launcher`
- 启动Admin面板: `http://localhost:9100/`

### 6. 后续需要美术同学做的
- `Assets/Art/Models/` - 角色/场景3D模型
- `Assets/Art/Textures/UI/` - UI贴图/图标
- `Assets/Art/Animations/` - 动画控制器
- `Assets/Art/Fonts/` - 自定义字体
- `Assets/Art/Materials/` - 材质球

## 关键API

### 面板操作
```csharp
UIManager.Instance.Show<LoginPanel>();    // 显示面板
UIManager.Instance.Hide<LoginPanel>();    // 隐藏面板
UIManager.Instance.HideAll();             // 隐藏所有
```

### 网络通信
```csharp
GameManager.Instance.Network.Send(msgId, body);  // 发送消息
// 通过 Manager 的 HandleMessage 接收消息
```

### 场景切换
```csharp
SceneManager.Instance.LoadScene(GameScene.MainCity);
```

### 战斗UI
```csharp
// 6个技能槽 + CD遮罩 + HP条 + Boss血条 + Buff图标
BattleHUD.Instance.UpdateHP(0.75f);           // 更新血量
BattleHUD.Instance.UpdateBossHP(0.5f, "Boss名"); // Boss血条
BattleHUD.Instance.UpdateCombo(5);            // 连击数
DamageNumber.Spawn(pos, 1234, true, false);   // 暴击飘字
BuffIcon.Create(buffArea, "Buff名", 5.0f);    // Buff图标
```
