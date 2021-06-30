using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainNumberIndicators
{
    static class RoomCorruptionIndicatorMod
    {
        public static void Patch(Harmony harmony)
        {
            harmony.ProcessorForAnnotatedClass(typeof(RoomCorruptionIndicatorMod_Start)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(RoomCorruptionIndicatorMod_OnDestroy)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(RoomCorruptionIndicatorMod_SetCorruption)).Patch();
        }
    }

    [HarmonyPatch(typeof(RoomCorruptionIndicator))]
    [HarmonyPatch("Start")]
    class RoomCorruptionIndicatorMod_Start
    {
        static bool Prefix(RoomCorruptionIndicator __instance)
        {
            RoomCorruptionIndicator This = __instance;

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

    [HarmonyPatch(typeof(RoomCorruptionIndicator))]
    [HarmonyPatch("OnDestroy")]
    class RoomCorruptionIndicatorMod_OnDestroy
    {
        static bool Prefix(RoomCorruptionIndicator __instance)
        {
            RoomCorruptionIndicator This = __instance;

            RoomIndicatorModCommon.DestroyTextObject(This);

            // Propagate into the original function
            return true;
        }
    }

    [HarmonyPatch(typeof(RoomCorruptionIndicator))]
    [HarmonyPatch("SetCorruption")]
    class RoomCorruptionIndicatorMod_SetCorruption
    {
        static bool Prefix(RoomCorruptionIndicator __instance, CorruptionInfo corruptionInfo)
        {
            RoomCorruptionIndicator This = __instance;

            StringBuilder textBuilder = new StringBuilder();
            textBuilder.Append("<br><sprite name=\"CRemovable\"><space=0.5em>");
            if (corruptionInfo.currentCorruption > corruptionInfo.maxCorruption)
                textBuilder.Append($"<color=#be2ee6>{corruptionInfo.currentCorruption}</color>");
            else
                textBuilder.Append(corruptionInfo.currentCorruption);
            if (corruptionInfo.currentPreviewCorruption != corruptionInfo.currentCorruption)
            {
                int absdiff = System.Math.Abs(corruptionInfo.currentPreviewCorruption - corruptionInfo.currentCorruption);
                if (corruptionInfo.currentPreviewCorruption > corruptionInfo.currentCorruption)
                    textBuilder.Append($" <color=#15bd3c>(+{absdiff})</color>");
                else
                    textBuilder.Append($" <color=#c9370e>(-{absdiff})</color>");
            }
            textBuilder.Append($" / {corruptionInfo.maxCorruption}<space=0.5em><sprite name=\"CSlot\">");
            if (corruptionInfo.currentPermanentCorruption > 0)
                textBuilder.Append($"<space=0.5em>(<sprite name=\"CUnremovable\"><space=0.5em>{corruptionInfo.currentPermanentCorruption})");

            RoomIndicatorModCommon.GetTextObject(This).text = textBuilder.ToString();

            // Disable propagating into the original function
            return false;
        }
    }
}
