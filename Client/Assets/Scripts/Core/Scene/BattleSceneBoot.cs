using UnityEngine;
using UnityEngine.UI;
using Jx3.Core.Battle;

namespace Jx3.Core.Scene
{
    public class BattleSceneBoot : SceneBoot
    {
        public int heroId = 1001;
        public int enemyBossId = 3001;  // 默认董龙
        public string dungeonName = "训练场";
        public int dungeonId = 1;        // 当前副本ID

        protected override void SetupScene()
        {
            // 从GameManager读取副本数据（由DungeonSelectPanel设置）
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentDungeonIndex >= 0)
            {
                var dungeons = DungeonConfig.GetAllDungeons();
                if (gm.CurrentDungeonIndex < dungeons.Count)
                {
                    var dungeonData = dungeons[gm.CurrentDungeonIndex];
                    dungeonId = dungeonData.id;
                    dungeonName = dungeonData.name;

                    // 选第一个Boss作为初始Boss
                    if (dungeonData.bossIds != null && dungeonData.bossIds.Count > 0)
                        enemyBossId = dungeonData.bossIds[0];

                    Debug.Log($"[BattleBoot] 从GameManager读取副本: {dungeonName} | 副本ID={dungeonId} | 初始BossID={enemyBossId}");
                }
            }

            Debug.Log($"[BattleBoot] Initializing battle: {dungeonName} (dungeonId={dungeonId})");

            // 初始化副本管理器
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.InitDungeon(dungeonId);
                DungeonManager.Instance.OnDungeonFailed += (reason) =>
                {
                    Debug.Log($"[BattleBoot] 副本失败回调: {reason}");
                };
                DungeonManager.Instance.OnDungeonComplete += () =>
                {
                    Debug.Log("[BattleBoot] 副本通关回调！");
                };
            }

            // 生成玩家英雄
            SpawnPlayerHero();

            // 生成敌人
            SpawnEnemy();

            // 创建HUD
            CreateBattleHUD();

            // 设置摄像机
            SetupCamera();
        }

        void SpawnPlayerHero()
        {
            var systemGo = new GameObject("HeroSwitchSystem", typeof(HeroSwitchSystem));
            var system = systemGo.GetComponent<HeroSwitchSystem>();
            system.teamIds = new System.Collections.Generic.List<int> { 1001, 1002, 1004, 1006 };
            system.transform.position = Vector3.zero;
            system.OnTeamWipe += () =>
            {
                Debug.Log("[BattleBoot] Team wiped - battle failed");
                var hud = FindObjectOfType<Jx3.UI.Battle.BattleHUD>();
                if (hud != null) hud.ShowDefeat();
            };
            Debug.Log("[BattleBoot] HeroSwitchSystem created (4-hero team)");
        }

        void SpawnEnemy()
        {
            var enemyGo = new GameObject("Enemy", typeof(EnemyUnit));
            enemyGo.transform.position = new Vector3(4, 0, 0);
            var enemy = enemyGo.GetComponent<EnemyUnit>();

            // 从配置加载Boss数据
            var bossData = DungeonConfig.GetBoss(enemyBossId);
            if (bossData != null)
            {
                enemy.bossId = bossData.id;
                enemy.bossName = bossData.name;
                enemy.maxHp = bossData.hp;
                enemy.currentHp = bossData.hp;
                enemy.attackPower = bossData.attack;
                enemy.defense = bossData.defense;
                enemy.attackInterval = bossData.attackInterval;
                enemy.skillNames = bossData.skillNames;
            }
            else
            {
                enemy.bossName = "训练木桩";
                enemy.maxHp = 5000;
                enemy.currentHp = 5000;
            }
            Debug.Log($"[BattleBoot] Spawned enemy {enemy.bossName} (HP:{enemy.maxHp})");
        }

        void CreateBattleHUD()
        {
            var hudGo = new GameObject("BattleHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(hudGo);
            var canvas = hudGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hudGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            hudGo.AddComponent<Jx3.UI.Battle.BattleHUD>();
            Debug.Log("[BattleBoot] BattleHUD created");
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 6, -10);
                cam.transform.rotation = Quaternion.Euler(25, 0, 0);
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0.08f, 0.06f, 0.12f);
            }
        }
    }
}