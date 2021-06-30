using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MonsterTrainNumberIndicators
{
    static class RoomIndicatorModCommon
    {
        static Dictionary<object, TMP_Text> extraTextHolder = new Dictionary<object, TMP_Text>();

        public static TMP_Text GetTextObject<T>(T roomIndicator) where T : Component
        {
            TMP_Text text;
            if (!extraTextHolder.TryGetValue(roomIndicator, out text))
            {
                // This text entry is usually shown when 7 unit limit has been reached.
                // Weirdly, RoomCorruptionIndicator has this dummy text object as well (but never shown).
                // This is convenient as we can use it as a text prefab for either cases.
                text = Object.Instantiate(
                    Traverse.Create(roomIndicator).Field("maxRoomTextElement").GetValue<TMP_Text>(),
                    roomIndicator.transform.parent
                );
                text.gameObject.SetActive(true);
                text.enabled = true;

                // Adjust y coordinate
                // Note: For RoomCorruptionIndicator it doesn't seem to work exactly,
                //       so there should be a quick and dirty <br> at the beginning of text.
                Canvas.ForceUpdateCanvases();
                var pos = text.rectTransform.position;
                pos.y = roomIndicator.GetComponent<RectTransform>().position.y;
                text.rectTransform.position = pos;

                // Override sprite asset
                text.spriteAsset = MonsterTrainNumberIndicators.Instance.modSpriteAsset;

                extraTextHolder.Add(roomIndicator, text);
            }

            return text;
        }

        public static void DestroyTextObject(object holder)
        {
            TMP_Text text;
            if (extraTextHolder.TryGetValue(holder, out text))
            {
                extraTextHolder.Remove(holder);
                text.gameObject.transform.SetParent(null);
                Object.Destroy(text);
            }
        }
    }
}
