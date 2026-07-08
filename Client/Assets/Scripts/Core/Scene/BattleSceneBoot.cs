using UnityEngine;
using Jx3.Core.Battle;

namespace Jx3.Core.Scene
{
    public class BattleSceneBoot : SceneBoot
    {
        public int heroId = 1001;
        public int enemyBossId = 3001;  // 默认董龙
        public string dungeonName = "训练场";

        public override void OnSceneLoaded()
        {
            base.OnSceneLoaded();
            SetupEnvironment();

            Debug.Log($"[BattleBoot] Initializing battle: {dungeonName}");

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
            var heroGo = new GameObject("PlayerHero", typeof(HeroUnit));
            heroGo.transform.position = new Vector3(-4, 0, 0);
            var hero = heroGo.GetComponent<HeroUnit>();
            hero.heroId = heroId;
            var template = HeroConfig.Get(heroId);
            if (template != null)
            {
                hero.heroName = template.name;
                hero.maxHp = template.baseHp;
                hero.currentHp = template.baseHp;
                hero.level = 20;
            }
            Debug.Log($"[BattleBoot] Spawned hero {hero.heroName} (HP:{hero.maxHp})");
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