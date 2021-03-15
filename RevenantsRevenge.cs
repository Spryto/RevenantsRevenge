using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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

        static ManualLogSource logger;

        
        void Awake()
        {
            logger = Logger;
            Settings.SetConfig(NAME, Config);

            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DungeonGenerator), "Generate", new Type[] { typeof(int), typeof(ZoneSystem.SpawnMode) })]
        static void ApplyGeneratorSettings(ref DungeonGenerator __instance)
        {
            __instance.m_minRooms = Settings.minimumDungeonSize.Value;
            __instance.m_maxRooms = Settings.maximumDungeonSize.Value;

            __instance.m_campRadiusMin = Settings.minimumCampSize.Value;
            __instance.m_campRadiusMax = Settings.maximumCampSize.Value;

            __instance.m_zoneSize = new Vector3(192f, 192f, 192f);
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
                    var locationInstances = AccessTools.FieldRefAccess<ZoneSystem, Dictionary<Vector2i, ZoneSystem.LocationInstance>>(__instance, "m_locationInstances");

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
                return (bool)methodHandler.Invoke(__instance, __state);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch (typeof(SpawnSystem), "Spawn")]
        static void RemoveMinLevelUpDistance(ref SpawnSystem.SpawnData __0)
        {
            if (Settings.isDefaultDifficulty) return;
            __0.m_levelUpMinCenterDistance = 0f;
        }

        [HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
        class CreatureSpawnerPatch
        {
            static void Prefix(ref CreatureSpawner __instance)
            {
                if (Settings.isDefaultDifficulty) return;

                __instance.m_minLevel = Settings.minMobLevel.Value;
                __instance.m_maxLevel = Settings.maxMobLevel.Value;
                __instance.m_levelupChance = Settings.mobLevelChance.Value;
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
                if (Settings.isDefaultDifficulty) return;

                var count = __instance.m_levelSetups.Count;
                if (count == 0)
                {
                    var levelSetup = new LevelEffects.LevelSetup();
                    levelSetup.m_scale = 1f + Settings.mobScale.Value / 100f;
                    levelSetup.m_hue = 0.3f;
                    levelSetup.m_saturation = 0.3f;
                    levelSetup.m_value = 0.1f;
                    __instance.m_levelSetups.Add(levelSetup);

                    var levelSetup2 = new LevelEffects.LevelSetup();
                    levelSetup.m_scale = 1f + 2 * Settings.mobScale.Value / 100f; ;
                    levelSetup.m_hue = 0.4f;
                    levelSetup.m_saturation = 0.35f;
                    levelSetup.m_value = 0.15f;
                    __instance.m_levelSetups.Add(levelSetup2);
                    count = __instance.m_levelSetups.Count;
                }
                if (count < Settings.maxMobLevel.Value)
                {
                    for (int i = count; i <= Settings.maxMobLevel.Value; i++)
                    {
                        var levelSetup = new LevelEffects.LevelSetup();
                        var prev = __instance.m_levelSetups[i - 1];
                        var prev2 = __instance.m_levelSetups[i - 2];

                        var scaleFactor = Settings.mobScale.Value / 100f;
                        levelSetup.m_scale = prev.m_scale + scaleFactor;
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
                        if (Settings.isDefaultDifficulty)
                        {
                            yield return instruction;
                            continue;
                        }
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ApplyLootFixPatch), "DropFormula"));
                        found = true;
                        continue;
                    }
                    yield return instruction;
                }
                if (found is false && !Settings.isDefaultDifficulty)
                    logger.LogError("Cannot find Math.Pow in CharacterDrop.GenerateDropList");
            }
        }

        [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
        private class ShowStar
        {
            private static int starOffset = 16;
            private static bool flag = false;
            private static void Prefix(ref EnemyHud __instance)
            {
                if (Settings.isDefaultDifficulty) return;
                if (Settings.maxMobLevel.Value > 7 && !flag)
                {
                    starOffset = 10;
                    MoveOriginalLevel3(__instance.m_baseHud);
                    flag = true;
                }
            }

            private static void Postfix(Character c, Dictionary<Character, object> ___m_huds)
            {
                if (Settings.isDefaultDifficulty) return;

                var hud = ___m_huds[c];
                var guiObject = (GameObject)typeof(EnemyHud).GetNestedType("HudData", BindingFlags.NonPublic).GetField("m_gui").GetValue(hud);

                if (HasLevelObject(guiObject, 2))
                {
                    SetupStarPositions(guiObject, c.GetLevel());
                }
            }

            private static void SetupStarPositions(GameObject guiObject, int level)
            {
                for (int i = 2; i <= Settings.maxMobLevel.Value; i++)
                {
                    var name = $"level_{i}";
                    if (!HasLevelObject(guiObject, i))
                    {
                        var gameObject = Instantiate(guiObject.transform.Find($"level_{i-1}").gameObject, guiObject.transform);
                        gameObject.name = name;
                        var left = Settings.maxMobLevel.Value > 7 ? 28 : 40;
                        CreateStar(gameObject, starOffset * i - left, i);
                    }
                    guiObject.transform.Find(name).gameObject.SetActive(i <= level);
                }
            }

            private static bool HasLevelObject(GameObject go, int level)
            {
                return go.transform.Find($"level_{level}");
            }

            private static void MoveOriginalLevel3(GameObject go)
            {
                var lgo = go.transform.Find("level_3").gameObject;
                var firstStar = lgo.transform.Find("star");
                var existingStar = lgo.transform.Find("star (1)");
                existingStar.localPosition = firstStar.localPosition + new Vector3(starOffset, 0);
            }

            private static void CreateStar(GameObject parent, int offset, int i)
            {
                var star = parent.transform.Find("star");
                var go = Instantiate(star.gameObject, parent.transform);
                go.transform.localPosition = new Vector3(offset, 0f);
            }
        }
    }
}