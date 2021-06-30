using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace MonsterTrainNumberIndicators
{
    [BepInEx.BepInPlugin("com.nedsociety.MonsterTrainNumberIndicators", "MonsterTrainNumberIndicators", "1.0.0.0")]
    public class MonsterTrainNumberIndicators : BepInEx.BaseUnityPlugin
    {
        public static MonsterTrainNumberIndicators Instance { get; private set; }
        public TMP_SpriteAsset modSpriteAsset { get; private set; }
        public Sprite modBigCapacityIcon { get; private set; }

        bool LoadAsset()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(
                Path.Combine(Path.GetDirectoryName(Info.Location), "modassetbundle")
            );
            if (assetBundle is null)
                return false;
            modSpriteAsset = assetBundle.LoadAsset<TMP_SpriteAsset>("SpriteAsset");
            modBigCapacityIcon = assetBundle.LoadAsset<Sprite>("BigCapacityIcon");
            return !(modSpriteAsset is null) && !(modBigCapacityIcon is null);
        }

        void Awake()
        {
            Instance = this;

            string reqRestart = "The game must be restarted to apply changes.";
            var enableRoomCapacityIndicatorMod = Config.Bind(
                "General", "Capacity Indicators", true,
                new BepInEx.Configuration.ConfigDescription(
                    $"Modify Capacity indicators for each floors to show numbers instead of pips. {reqRestart}"
                )
            );
            var addSlotCap = Config.Bind(
                "General", "Capacity Indicators: add unit number indicator", true,
                new BepInEx.Configuration.ConfigDescription(
                    $"Capacity indicators will show space indicators as well. Requires Capacity Indicators to be active. {reqRestart}"
                )
            );
            var enableRoomCorruptionIndicatorMod = Config.Bind(
                "General", "Echoes Indicators", true,
                new BepInEx.Configuration.ConfigDescription(
                    $"Modify Echoes indicators for each floors to show numbers instead of pips. {reqRestart}"
                )
            );
            var enableCardMod = Config.Bind(
                "General", "Card Capacity Indicators", true,
                new BepInEx.Configuration.ConfigDescription(
                    $"Modify Cards to show numbers instead of pips. {reqRestart}"
                )
            );


            if (!LoadAsset())
            {
                Logger.LogError("Asset loading failed -- this plugin is disabled.");
                return;
            }

            Harmony harmony = new Harmony("com.nedsociety.MonsterTrainNumberIndicators");
            if (enableRoomCapacityIndicatorMod.Value)
                RoomCapacityIndicatorMod.Patch(harmony, addSlotCap.Value);
            if (enableRoomCorruptionIndicatorMod.Value)
                RoomCorruptionIndicatorMod.Patch(harmony);
            if (enableCardMod.Value)
                CardMod.Patch(harmony);
        }

        public void LogInfo(string text)
        {
            Logger.LogInfo(text);
        }
    }
}
