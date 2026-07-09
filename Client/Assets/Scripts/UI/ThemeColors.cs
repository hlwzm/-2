using UnityEngine;

namespace Jx3.UI
{
    /// <summary>
    /// Unified UI theme — 金墨武侠风格 (Gold Ink Wuxia)
    /// Deep warm backgrounds · Gold accents · Ink-green / dark-red highlights
    /// </summary>
    public static class ThemeColors
    {
        // ── Backgrounds ──
        public static readonly Color BgMain = new(0.047f, 0.039f, 0.031f);        // #0c0a08 墨底
        public static readonly Color BgCard = new(0.102f, 0.086f, 0.070f, 0.92f); // #1a1612 卡片底
        public static readonly Color BgListItem = new(0.133f, 0.118f, 0.094f);    // #221e18 列表项
        public static readonly Color BgInput = new(0.133f, 0.118f, 0.094f, 0.85f);// 输入框

        // ── Borders & Decorations ──
        public static readonly Color Border = new(0.227f, 0.196f, 0.165f);        // #3a322a 暖棕边框
        public static readonly Color Accent = new(0.83f, 0.66f, 0.26f, 0.9f);     // #d4a843 金色主强调
        public static readonly Color AccentDim = new(0.83f, 0.66f, 0.26f, 0.35f);
        public static readonly Color DecorLine = new(0.54f, 0.42f, 0.16f, 0.5f);  // #8a6b2a 金铜装饰线

        // ── Currencies ──
        public static readonly Color Gold = new(0.91f, 0.77f, 0.31f);    // #e8c550 明金
        public static readonly Color Tongbao = new(0.35f, 0.69f, 0.77f); // #5ab0c4 玉青
        public static readonly Color Stamina = new(0.35f, 0.54f, 0.29f); // #5a8a4a 墨绿

        // ── Text ──
        public static readonly Color TextBright = new(0.94f, 0.91f, 0.85f); // #f0e8d8 暖白
        public static readonly Color TextNormal = new(0.77f, 0.72f, 0.63f);  // #c4b8a0 暖灰金
        public static readonly Color TextDim = new(0.48f, 0.43f, 0.38f);     // #7a6e60 暗灰
        public static readonly Color TextWhite = TextBright;

        // ── Buttons ──
        public static readonly Color BtnPrimary = new(0.54f, 0.42f, 0.16f, 0.85f);   // #8a6b2a 铜金
        public static readonly Color BtnSecondary = new(0.16f, 0.15f, 0.13f, 0.85f); // #2a2622 暗暖
        public static readonly Color BtnDanger = new(0.42f, 0.16f, 0.16f, 0.85f);     // #6b2a2a 暗红
        public static readonly Color BtnHover = new(0.22f, 0.20f, 0.17f, 0.9f);     // #38332b

        // ── Tab ──
        public static readonly Color TabActive = new(0.54f, 0.42f, 0.16f, 0.75f);    // #8a6b2a 铜金底
        public static readonly Color TabInactive = new(0.07f, 0.06f, 0.05f, 0.6f);

        // ── Quality Colors ──
        public static readonly Color QualityNormal = new(0.69f, 0.69f, 0.69f);     // ★ 白灰
        public static readonly Color QualityGood = new(0.35f, 0.54f, 0.29f);       // ★★ 墨绿
        public static readonly Color QualityRare = new(0.29f, 0.48f, 0.69f);       // ★★★ 靛蓝
        public static readonly Color QualityEpic = new(0.54f, 0.35f, 0.69f);       // ★★★★ 紫
        public static readonly Color QualityLegend = new(0.91f, 0.77f, 0.31f);     // ★★★★★ 金

        // ── Chat Channels ──
        public static readonly Color ChatWorld = new(0.48f, 0.69f, 0.77f);    // 玉青
        public static readonly Color ChatLocal = new(0.35f, 0.69f, 0.35f);   // 草绿
        public static readonly Color ChatTeam = new(0.91f, 0.77f, 0.31f);    // 金
        public static readonly Color ChatGuild = new(0.77f, 0.35f, 0.69f);   // 樱粉
        public static readonly Color ChatSystem = new(0.91f, 0.48f, 0.29f);  // 橙
        public static readonly Color ChatPrivate = new(0.77f, 0.35f, 0.35f); // 玫瑰

        // ── Spacing ──
        public const float SpacingLarge = 30f;
        public const float SpacingMedium = 15f;
        public const float SpacingSmall = 10f;

        // ── Font Sizes ──
        public const int FontTitle = 36;
        public const int FontPanelTitle = 28;
        public const int FontEntry = 20;
        public const int FontBody = 18;
        public const int FontSmall = 16;
        public const int FontTiny = 14;

        public static Color GetQualityColor(int stars) => stars switch
        {
            1 => QualityNormal,
            2 => QualityGood,
            3 => QualityRare,
            4 => QualityEpic,
            _ => QualityLegend
        };

        public static string GetQualityStars(int stars)
        {
            var s = "";
            for (int i = 0; i < 5; i++) s += i < stars ? "★" : "☆";
            return s;
        }
    }
}