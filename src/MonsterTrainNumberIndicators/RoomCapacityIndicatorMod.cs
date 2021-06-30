using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine.UI;

namespace MonsterTrainNumberIndicators
{
    static class RoomCapacityIndicatorMod
    {
        public static bool addSlotCap { get; private set; }

        public static void Patch(Harmony harmony, bool addSlotCap)
        {
            RoomCapacityIndicatorMod.addSlotCap = addSlotCap;

            harmony.ProcessorForAnnotatedClass(typeof(RoomCapacityIndicatorMod_Start)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(RoomCapacityIndicatorMod_OnDestroy)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(RoomCapacityIndicatorMod_RoomCapacityUI_GetAndCompareCapacityInfo)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(RoomCapacityIndicatorMod_SetCapacity)).Patch();
        }
    }

    static class AdditionalFloorDataCache
    {
        public static int characterCount { get; set; }
        public static int numSpawnPoints { get; set; }
    };

    [HarmonyPatch(typeof(RoomCapacityIndicator))]
    [HarmonyPatch("Start")]
    class RoomCapacityIndicatorMod_Start
    {
        static bool Prefix(RoomCapacityIndicator __instance)
        {
            RoomCapacityIndicator This = __instance;

            // Disable overflow box
            Traverse.Create(This).Field("overflowContainer").GetValue<HorizontalLayoutGroup>().gameObject.SetActive(false);

            // Make the background box transparent -- completely disabling this would make tooltip not appear which is undesirable.
            Image image = This.GetComponent<Image>();
            var color = image.color;
            color.a = 0;
            image.color = color;

            // Propagate into the original function
            return true;
        }
    }

    [HarmonyPatch(typeof(RoomCapacityIndicator))]
    [HarmonyPatch("OnDestroy")]
    class RoomCapacityIndicatorMod_OnDestroy
    {
        static bool Prefix(RoomCapacityIndicator __instance)
        {
            RoomCapacityIndicator This = __instance;

            RoomIndicatorModCommon.DestroyTextObject(This);

            // Propagate into the original function
            return true;
        }
    }

    [HarmonyPatch(typeof(RoomCapacityUI))]
    [HarmonyPatch("GetAndCompareCapacityInfo")]
    class RoomCapacityIndicatorMod_RoomCapacityUI_GetAndCompareCapacityInfo
    {
        static void Postfix(bool __result, RoomState room)
        {
            // Cache room data only if it's about to update
            if (__result)
            {
                var spg = Traverse.Create(room).Field("monsterSpawnPointGroup").GetValue<SpawnPointGroup>();

                AdditionalFloorDataCache.characterCount = spg.GetCharacters(includeOuterTrain: false).Count;
                AdditionalFloorDataCache.numSpawnPoints = Traverse.Create(spg).Field("NumSpawnPoints").GetValue<int>();
            }
        }
    }

    [HarmonyPatch(typeof(RoomCapacityIndicator))]
    [HarmonyPatch("SetCapacity")]
    class RoomCapacityIndicatorMod_SetCapacity
    {
        static bool Prefix(RoomCapacityIndicator __instance, CapacityInfo capacityInfo)
        {
            RoomCapacityIndicator This = __instance;

            bool capacityOverloaded = (capacityInfo.count > capacityInfo.max);
            
            StringBuilder textBuilder = new StringBuilder();

            // Capacity text

            // TODO: For performance add this Capacity sprite into our custom sprite atlas.
            //       According to the doc it will reduce one draw call.
            textBuilder.Append("<sprite name=\"Capacity\"><space=0.5em>");
            if (capacityOverloaded)
                textBuilder.Append("<color=#e6902e>");
            textBuilder.Append(capacityInfo.count);
            if (capacityInfo.nextSpawn > 0)
            {
                if (!capacityOverloaded)
                    textBuilder.Append("<color=#15bd3c>");
                textBuilder.Append($" (+{capacityInfo.nextSpawn})");
                if (!capacityOverloaded)
                    textBuilder.Append("</color>");
            }
            if (capacityOverloaded)
                textBuilder.Append("</color>");
            textBuilder.Append($" / {capacityInfo.max}<space=1em>");

            // Spawn point text: Show only in non-preview mode as we have no way to reliably predict how much will be added.
            //                   (e.g., Morselmaster)
            if (RoomCapacityIndicatorMod.addSlotCap && (AdditionalFloorDataCache.characterCount > 0) && (capacityInfo.nextSpawn == 0))
            {
                textBuilder.Append("<sprite name=\"CPerson\"><space=0.5em>");
                if (capacityInfo.isAtMaxSpawnPoints)
                    textBuilder.Append($"<color=#c9370e>{AdditionalFloorDataCache.characterCount}</color>");
                else
                    textBuilder.Append(AdditionalFloorDataCache.characterCount);
                textBuilder.Append($" / {AdditionalFloorDataCache.numSpawnPoints}");
            }
            else if (!RoomCapacityIndicatorMod.addSlotCap)
                Traverse.Create(This).Field("maxRoomTextElement").GetValue<TMP_Text>().enabled = capacityInfo.isAtMaxSpawnPoints;

            RoomIndicatorModCommon.GetTextObject(This).text = textBuilder.ToString();

            // Disable propagating into the original function
            return false;
        }
    }
}
