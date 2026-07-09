using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

namespace Jx3.UI
{
    [ExecuteInEditMode]
    public class IconManager : MonoBehaviour
    {
        public static IconManager Instance { get; private set; }

        [Header("Currency Icons")]
        public Sprite goldIcon;
        public Sprite tongbaoIcon;
        public Sprite staminaIcon;
        public Sprite gemIcon;

        [Header("Skill Icons")]
        public Sprite fireSkillIcon;
        public Sprite iceSkillIcon;
        public Sprite lightningSkillIcon;
        public Sprite healSkillIcon;
        public Sprite buffSkillIcon;

        [Header("Item Icons")]
        public Sprite potionIcon;
        public Sprite swordIcon;
        public Sprite shieldIcon;
        public Sprite scrollIcon;
        public Sprite armorIcon;

        [Header("UI Icons")]
        public Sprite closeIcon;
        public Sprite backIcon;
        public Sprite coinIcon;

        private Dictionary<string, Sprite> _iconMap = new();

        void Awake()
        {
            if (Instance == null) Instance = this;
            BuildMap();
        }

        void BuildMap()
        {
            _iconMap = new Dictionary<string, Sprite>
            {
                ["gold"] = goldIcon,
                ["tongbao"] = tongbaoIcon,
                ["stamina"] = staminaIcon,
                ["gem"] = gemIcon,
                ["skill_fire"] = fireSkillIcon,
                ["skill_ice"] = iceSkillIcon,
                ["skill_lightning"] = lightningSkillIcon,
                ["skill_heal"] = healSkillIcon,
                ["skill_buff"] = buffSkillIcon,
                ["item_potion"] = potionIcon,
                ["item_sword"] = swordIcon,
                ["item_shield"] = shieldIcon,
                ["item_scroll"] = scrollIcon,
                ["item_armor"] = armorIcon,
                ["close"] = closeIcon,
                ["back"] = backIcon,
                ["coin"] = coinIcon,
            };
        }

        public Sprite GetIcon(string key)
        {
            if (_iconMap.TryGetValue(key, out var icon)) return icon;
            return null;
        }

        public void SetImage(Image img, string key)
        {
            var icon = GetIcon(key);
            if (icon != null)
            {
                img.sprite = icon;
                img.color = Color.white;
            }
        }

        [MenuItem("GameObject/UI/Create Icon Manager")]
        static void CreateIconManager()
        {
            var go = new GameObject("IconManager", typeof(IconManager));
            Undo.RegisterCreatedObjectUndo(go, "Create Icon Manager");
            Selection.activeObject = go;
            Debug.Log("[IconManager] Created. Drag sprites into the fields from the Asset packs.");
        }
    }
}