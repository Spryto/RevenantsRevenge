using BepInEx.Configuration;

namespace RevenantsRevenge
{
    static class Settings
    {
        public static ConfigEntry<int> minimumDungeonSize;
        public static ConfigEntry<int> maximumDungeonSize;
        public static ConfigEntry<int> minimumCampSize;
        public static ConfigEntry<int> maximumCampSize;

        public static bool isDefaultDifficulty;

        public static ConfigEntry<int> minMobLevel;
        public static ConfigEntry<int> maxMobLevel;
        public static ConfigEntry<int> mobLevelChance;
        public static ConfigEntry<int> mobScale;

        public static ConfigEntry<bool> isNewWorld;

        public static void SetConfig(string name, ConfigFile config)
        {
            var minConfigDescription = new ConfigDescription(
                "Dungeons will generate with at least this many rooms placed. \n" +
                "In vanilla Valheim, this value is 3.",
                new AcceptableValueRange<int>(0, 200));
            minimumDungeonSize = config.Bind(name + ".Global", "min_rooms", 15, minConfigDescription);

            var maxConfigDescription = new ConfigDescription(
                "Dungeons will attempt to place this many rooms. \n" +
                "In vanilla Valheim, this value varies based on dungeon type but tends to be 30 or 40." +
                "Expect fps drops in dungeons if this is over 100.",
                new AcceptableValueRange<int>(0, 200));
            maximumDungeonSize = config.Bind(name + ".Global", "max_rooms", 60, maxConfigDescription);

            var minCampDescription = new ConfigDescription(
                "Camps will generate with at least this many components placed. \n",
                new AcceptableValueRange<int>(0, 80));
            minimumCampSize = config.Bind(name + ".Global", "camp_min", 25, minCampDescription);

            var maxCampDescription = new ConfigDescription(
                "Camps will attempt to place this many components.",
                new AcceptableValueRange<int>(0, 80));
            maximumCampSize = config.Bind(name + ".Global", "camp_max", 40, maxCampDescription);

            var mobMinLvlDescription = new ConfigDescription(
                "Minimum level of dungeon/point of interest mobs. Level 1 (default) means Zero stars.",
                new AcceptableValueRange<int>(0, 10));
            minMobLevel = config.Bind(name + ".Global", "mob_min_lvl", 1, mobMinLvlDescription);

            var mobMaxLvlDescription = new ConfigDescription(
                "Maximum level of dungeon/point of interest mobs. Level 3 (default) means 2 Stars.",
                new AcceptableValueRange<int>(0, 10));
            maxMobLevel = config.Bind(name + ".Global", "mob_max_lvl", 3, mobMaxLvlDescription);

            var mobLevelChanceDescription = new ConfigDescription(
                "Chance for dungeon/point of interest mobs to spawn higher than the minimum level.",
                new AcceptableValueRange<int>(0, 100));
            mobLevelChance = config.Bind(name + ".Global", "mob_lvl_chance", 15, mobLevelChanceDescription);

            var mobScaleDescription = new ConfigDescription(
                "How much bigger are higher level mobs?" +
                "Percentage, per level",
                new AcceptableValueRange<int>(0, 500));
            mobScale = config.Bind(name + ".Global", "mob_scale", 10, mobScaleDescription);

            var newWorldDescription = new ConfigDescription(
                "If you're running this on a world that existed before the mod was installed, change the value to false");
            isNewWorld = config.Bind(name + ".Global", "new_world", true, newWorldDescription);

            isDefaultDifficulty = minMobLevel.Value == 1 && maxMobLevel.Value == 3 && mobLevelChance.Value == 15;
        }
    }
}
