using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace RevenantsRevenge
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [HarmonyPatch]
    public class RevenantsRevenge : BaseUnityPlugin
    {
        private const string GUID = "spryto.revenants-revenge";
        private const string NAME = "Revenants Revenge";
        private const string VERSION = "1.0.0";

        private static ManualLogSource logger;

        private static ConfigFile config;

        private static ConfigEntry<int> minimumDungeonSize;
        private static ConfigEntry<int> maximumDungeonSize;
        private static ConfigEntry<int> minimumCampSize;
        private static ConfigEntry<int> maximumCampSize;
        private static ConfigEntry<int> minMobLevel;
        private static ConfigEntry<int> maxMobLevel;
        private static ConfigEntry<int> mobLevelChance;
        // TODO spawn chance config
        
        void Awake()
        {
            logger = Logger;
            config = Config;

            var minConfigDescription = new ConfigDescription(
                "Dungeons will generate with at least this many rooms placed. \n" +
                "In vanilla Valheim, this value is 3.",
                new AcceptableValueRange<int>(0, 200));
            minimumDungeonSize = config.Bind(NAME + ".Global", "min_rooms", 15, minConfigDescription);

            var maxConfigDescription = new ConfigDescription(
                "Dungeons will attempt to place this many rooms. \n" +
                "In vanilla Valheim, this value varies based on dungeon type but tends to be 30 or 40." +
                "Expect fps drops in dungeons if this is over 100.",
                new AcceptableValueRange<int>(0, 200));
            maximumDungeonSize = config.Bind(NAME + ".Global", "max_rooms", 60, maxConfigDescription);

            var minCampDescription = new ConfigDescription(
                "Camps will generate with at least this many components placed. \n",
                new AcceptableValueRange<int>(0, 80));
            minimumCampSize = config.Bind(NAME + ".Global", "camp_min", 25, minCampDescription);

            var maxCampDescription = new ConfigDescription(
                "Camps will attempt to place this many components.",
                new AcceptableValueRange<int>(0, 80));
            maximumCampSize = config.Bind(NAME + ".Global", "camp_max", 40, maxCampDescription);

            var mobMinLvlDescription = new ConfigDescription(
                "Minimum level of dungeon/point of interest mobs. Level 1 (default) means Zero stars.",
                new AcceptableValueRange<int>(0, 10));
            minMobLevel = config.Bind(NAME + ".Global", "mob_min_lvl", 1, mobMinLvlDescription);

            var mobMaxLvlDescription = new ConfigDescription(
                "Maximum level of dungeon/point of interest mobs. Level 3 (default) means 2 Stars.",
                new AcceptableValueRange<int>(0, 10));
            maxMobLevel = config.Bind(NAME + ".Global", "mob_max_lvl", 3, mobMaxLvlDescription);

            var mobLevelChanceDescription = new ConfigDescription(
                "Chance for dungeon/point of interest mobs to spawn higher than the minimum level.",
                new AcceptableValueRange<int>(0, 100));
            mobLevelChance = config.Bind(NAME + ".Global", "mob_lvl_chance", 15, mobLevelChanceDescription);


            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DungeonGenerator), "Generate", new Type[] { typeof(int), typeof(ZoneSystem.SpawnMode) })]
        static void ApplyGeneratorSettings(ref DungeonGenerator __instance)
        {
            __instance.m_minRooms = minimumDungeonSize.Value;
            __instance.m_maxRooms = maximumDungeonSize.Value;

            __instance.m_campRadiusMin = minimumCampSize.Value;
            __instance.m_campRadiusMax = maximumCampSize.Value;

            __instance.m_zoneSize = new Vector3(192f, 192f, 192f);
            logger.LogInfo($"Attempting to make a bigger {GetThemeName(__instance.m_themes)}");
        }

        private static string GetThemeName(Room.Theme theme)
        {
            var themes = new List<string>();
            if ((theme & Room.Theme.Crypt) != 0)
            {
                themes.Add("Crypt");
            }
            if ((theme & Room.Theme.SunkenCrypt) != 0)
            {
                themes.Add("SunkenCrypt");
            }
            if ((theme & Room.Theme.Cave) != 0)
            {
                themes.Add("Cave");
            }
            if ((theme & Room.Theme.ForestCrypt) != 0)
            {
                themes.Add("ForestCrypt");
            }
            if ((theme & Room.Theme.GoblinCamp) != 0)
            {
                themes.Add("GoblinCamp");
            }
            if ((theme & Room.Theme.MeadowsVillage) != 0)
            {
                themes.Add("MeadowsVillage");
            }
            if ((theme & Room.Theme.MeadowsFarm) != 0)
            {
                themes.Add("MeadowsFarm");
            }
            return themes.Join();
        }


        [HarmonyPatch(typeof(Location), "Awake")]
        class ResizeDungeonEnvironmentPatch
        {
            static void Prefix(ref Location __instance, out bool __state)
            {
                __state = __instance.m_hasInterior;
                __instance.m_hasInterior = false;
            }

            static void Postfix(ref Location __instance, bool __state)
            {
                __instance.m_hasInterior = __state;
                if (__instance.m_hasInterior)
                {
                    Vector2i zone = ZoneSystem.instance.GetZone(__instance.transform.position);
                    Vector3 zoneCenter = ZoneSystem.instance.GetZonePos(zone);

                    var zoneSize = ZoneSystem.instance.m_zoneSize;
                    if (__instance.name.ToUpperInvariant().Contains("TROLL"))
                    {
                        Vector3 position = new Vector3(zoneCenter.x, __instance.transform.position.y + 5000f, zoneCenter.z);
                        GameObject gameObject = Instantiate(__instance.m_interiorPrefab, position, Quaternion.identity, __instance.transform);
                        gameObject.transform.localScale = new Vector3(zoneSize, 250f, zoneSize);
                        gameObject.GetComponent<EnvZone>().m_environment = __instance.m_interiorEnvironment;
                    }
                    else
                    {
                        Vector3 position = new Vector3(zoneCenter.x, __instance.transform.position.y + 5000f, zoneCenter.z);
                        GameObject gameObject = Instantiate(__instance.m_interiorPrefab, position, Quaternion.identity, __instance.transform);
                        gameObject.transform.localScale = new Vector3(192f, 500f, 192f);
                        gameObject.GetComponent<EnvZone>().m_environment = __instance.m_interiorEnvironment;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), "HaveLocationInRange")]
        class OverlapPreventionPatch
        {
            class PrefixState
            {
                public PrefixState(Vector3 point, float radius)
                {
                    this.point = point;
                    this.radius = Math.Max(radius, 192);
                }

                public bool isDungeon { get; set; }
                public float radius { get; }
                public Vector3 point { get; }
            }

            static void Prefix(string __0, ref string __1, Vector3 __2, ref float __3, out PrefixState __state)
            {
                __state = new PrefixState(__2, __3);
                __state.isDungeon = IsDungeon(__0);
            }

            static void Postfix(ref ZoneSystem __instance, ref bool __result, PrefixState __state)
            {
                if (__result)
                    return;
                if (__state.isDungeon)
                {
                    var locationInstances = AccessTools.FieldRefAccess<ZoneSystem, Dictionary<Vector2i, ZoneSystem.LocationInstance>> (__instance, "m_locationInstances");

                    foreach (ZoneSystem.LocationInstance locationInstance in locationInstances.Values)
                    {
                        var prefabName = locationInstance.m_location.m_prefabName;
                        if ((IsDungeon(prefabName) || IsTrollCave(prefabName)) && 
                            Vector3.Distance(locationInstance.m_position, __state.point) < __state.radius)
                        {
                            __result = true;
                            return;
                        }
                    }
                }
                return;
            }

            private static bool IsTrollCave(string prefabName)
            {
                return prefabName.Contains("Troll");
            }

            private static bool IsDungeon(string prefabName)
            {
                return prefabName.Contains("Crypt") || prefabName.Contains("Cave");
            }
        }

        [HarmonyPatch(typeof(DungeonGenerator), "PlaceOneRoom")]
        class RetryRoomPlacementPatch
        {
            static int retry_limit = 5;
            static int retries = 0;

            static void Prefix(ZoneSystem.SpawnMode __0, out ZoneSystem.SpawnMode __state)
            {
                __state = __0;
            }

            static bool Postfix(bool __result, ZoneSystem.SpawnMode __state, DungeonGenerator __instance)
            {
                if (__result)
                {
                    retries = 0;
                    return true;
                }
                retries++;
                
                if (retries > retry_limit) return false;

                var methodHandler = MethodInvoker.GetHandler(AccessTools.Method(typeof(DungeonGenerator), "PlaceOneRoom"));
                return (bool) methodHandler.Invoke(__instance, __state);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch (typeof(SpawnSystem), "Spawn")]
        static void RemoveMinLevelUpDistance(ref SpawnSystem.SpawnData __0)
        {
            __0.m_levelUpMinCenterDistance = 0f;
        }

        [HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
        class CreatureSpawnerPatch
        {
            static void Prefix(ref CreatureSpawner __instance)
            {
                __instance.m_minLevel = minMobLevel.Value;
                __instance.m_maxLevel = maxMobLevel.Value;
                __instance.m_levelupChance = mobLevelChance.Value;
                if (__instance.m_creaturePrefab.GetComponent(typeof(LevelEffects)) == null)
                {
                    __instance.m_creaturePrefab.AddComponent(typeof(LevelEffects));
                }
            }
        }

        [HarmonyPatch(typeof(LevelEffects), "Start")]
        class ApplyLevelVisualisationPatch
        {
            static void Prefix(ref LevelEffects __instance)
            {
                var count = __instance.m_levelSetups.Count;
                if (count == 0)
                {
                    var levelSetup = new LevelEffects.LevelSetup();
                    levelSetup.m_scale = 1.1f;
                    levelSetup.m_hue = 0.3f;
                    levelSetup.m_saturation = 0.3f;
                    levelSetup.m_value = 0.1f;
                    __instance.m_levelSetups.Add(levelSetup);

                    var levelSetup2 = new LevelEffects.LevelSetup();
                    levelSetup.m_scale = 1.2f;
                    levelSetup.m_hue = 0.4f;
                    levelSetup.m_saturation = 0.35f;
                    levelSetup.m_value = 0.15f;
                    __instance.m_levelSetups.Add(levelSetup2);
                    count = __instance.m_levelSetups.Count;
                }
                if (count < maxMobLevel.Value)
                {
                    for (int i = count; i <= maxMobLevel.Value; i++)
                    {
                        var levelSetup = new LevelEffects.LevelSetup();
                        var prev = __instance.m_levelSetups[i - 1];
                        var prev2 = __instance.m_levelSetups[i - 2];

                        var scaleFactor = 0.1f;
                        levelSetup.m_scale = 1.0f + scaleFactor*(i+1);
                        levelSetup.m_hue = prev.m_hue * 2 - prev2.m_hue;
                        levelSetup.m_saturation = prev.m_saturation * 2 - prev2.m_saturation;
                        levelSetup.m_value = prev.m_value * 2 - prev2.m_value;
                        levelSetup.m_enableObject = prev.m_enableObject;
                        __instance.m_levelSetups.Add(levelSetup);
                    }
                }
            }
        }

        // reduces loot from higher level mobs for better balancing and to reduce risk of lag or crashing
        [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
        class ApplyLootFixPatch
        {
            // just matching the math.pow type signature for now
            // capping drops at 100 until loot entity count is reduced
            private static double DropFormula(double x, double level) => Math.Min(level * (level + 1) / 2 + 1, 100);

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var found = false;
                foreach (var instruction in instructions)
                {
                    if (!found && instruction.Calls(AccessTools.Method(typeof(Mathf), "Pow")))
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ApplyLootFixPatch), "DropFormula"));
                        found = true;
                        continue;
                    }
                    yield return instruction;
                }
                if (found is false)
                    logger.LogError("Cannot find Math.Pow in CharacterDrop.GenerateDropList");
            }
        }

    }
}