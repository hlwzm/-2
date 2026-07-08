using System.Collections.Generic;

namespace Jx3.Core
{
    public static class HeroManager
    {
        public static List<HeroInfo> OwnedHeroes { get; } = new();
        public static void AddHero(HeroInfo hero) { OwnedHeroes.Add(hero); }
    }

    public class HeroInfo
    {
        public uint HeroUid; public uint TemplateId; public string Name = "";
        public int Level = 1; public int Star = 1; public int Quality = 4;
    }
}
