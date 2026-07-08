# 指尖江湖2 (JX3: Fingertip Rivers and Lakes 2)

英雄收集+ARPG武侠MMORPG，参照剑网三指尖江湖。

## 技术栈

| 层 | 技术 |
|------|------|
| 客户端 | Unity 2022 LTS+ / URP / FGUI |
| 服务端 | C# .NET 9 (ASP.NET + 自研TCP) |
| 数据库 | MySQL 8.0 (Dapper) + Redis 7.x |
| 部署 | Docker Compose |

## 快速开始

### 1. 启动基础设施 (MySQL + Redis)

```bash
cd deploy
docker-compose up -d
```

### 2. 建表 (自动执行 或 手动)

```bash
mysql -h127.0.0.1 -uroot -p123456 jx3 < Server/SQL/create_tables.sql
mysql -h127.0.0.1 -uroot -p123456 jx3 < Server/SQL/init_data.sql
```

### 3. 启动服务器

```bash
# 一键启动全部微服务 (推荐)
cd Server
dotnet run --project Jx3.Launcher

# 或按需启动单个
dotnet run --project Jx3.Gateway
dotnet run --project Jx3.Login
# ...
```

### 4. 访问后台管理

打开浏览器: http://localhost:9100
用户名/密码: admin / admin123

## 项目结构

```
指尖江湖2/
├── Documents/        # 设计文档 + 协议接口
├── Client/           # Unity客户端代码
├── Server/           # 服务端 (13个微服务)
│   ├── Jx3.Launcher/ # 一键启动器
│   ├── Jx3.Gateway/  # 网关 (TCP 9000)
│   ├── Jx3.Login/    # 登录 (TCP 9001)
│   ├── Jx3.Hero/     # 英雄+招募 (TCP 9002)
│   ├── Jx3.Trade/    # 交易 (TCP 9003)
│   ├── Jx3.Battle/   # 战斗 (TCP 9004)
│   ├── Jx3.Dungeon/  # 副本 (TCP 9005)
│   ├── Jx3.Chat/     # 聊天 (TCP 9006)
│   ├── Jx3.Social/   # 组队/好友/同盟 (TCP 9007)
│   ├── Jx3.PVP/      # 竞技场 (TCP 9008)
│   ├── Jx3.Shop/     # 商城 (TCP 9009)
│   ├── Jx3.Quest/    # 任务 (TCP 9010)
│   └── Jx3.Admin/    # 后台管理 (HTTP 9100)
├── deploy/           # Docker/部署配置
└── build.bat         # 构建脚本
```

## 系统功能

- **英雄收集**: 50+英雄, 抽卡90保底, 升级升星, 编队战斗
- **副本**: 4副本(风雨稻香村/天子峰/日轮山城/荻花宫)
  每个3Boss+终极Boss, 限时解锁, 4-8人组队
- **交易**: 自由市场, 5%手续费, 分类搜索
- **PVP**: ELO匹配, 青铜→传说段位, 赛季制
- **聊天**: 5频道, 私聊, 语音, 敏感词过滤
- **同盟**: 帮会创建, 贡献系统, 帮会战

## 开发指南

详见: Documents/游戏开发指南.md
接口协议: Documents/接口协议/*.proto