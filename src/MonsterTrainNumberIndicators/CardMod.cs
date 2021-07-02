using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MonsterTrainNumberIndicators
{
    static class CardMod
    {
        public static void Patch(Harmony harmony)
        {
            harmony.ProcessorForAnnotatedClass(typeof(CardMod_CardUI_ApplyStateToUI)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(CardMod_CardUI_DoUpgradeSequence)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(CardMod_CardFrameUI_SetUpFrame)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(CardMod_CapacityChangeHighlighter_SetUp)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(CardMod_HandUI_UpdateCardUI)).Patch();
        }

        public static CardFrameUI GetCardFrameUI(this CardUI cardUI)
            => Traverse.Create(cardUI).Field("_cardFrame").GetValue<CardFrameUI>();

        public static CardStatUpgradeVFX GetCardStatUpgradeVFX(this CardUI cardUI)
            => Traverse.Create(cardUI).Field("cardStatUpgradeVFX").GetValue<CardStatUpgradeVFX>();

        public static CardUI GetCardUI(this CardFrameUI cardFrameUI)
        {
            // [CardUI/CardCanvas/CardUIContainer/Card front/Card Frame UI]
            return cardFrameUI?.transform?.parent?.parent?.parent?.parent?.GetComponent<CardUI>();
        }
        public static List<AbstractSpriteSelector> GetSpriteSelectors(this CardFrameUI cardFrameUI)
            => Traverse.Create(cardFrameUI).Field("spriteSelectors").GetValue<List<AbstractSpriteSelector>>();

        static bool IsThisCardModded(CardUI cardUI)
        {
            // Added at SetupCardFrameUI()
            return cardUI.GetCardFrameUI().transform.Find("Capacity Pieces") != null;
        }
        static void SetupCardFrameUI(CardFrameUI cardFrameUI)
        {
            var spriteSelectors = cardFrameUI.GetSpriteSelectors();

            // Disable pips [Card Frame UI/Capacity indicators]
            var pip = cardFrameUI.transform.Find("Capacity indicators");
            if (pip?.gameObject.activeSelf == true)
            {
                spriteSelectors.Remove(pip.GetComponent<CapacityMasterySpriteSelector>());
                pip.gameObject.SetActive(false);
            }

            // Copy [Card Frame UI/Ember Pieces] to [Card Frame UI/Capacity Pieces]
            Transform emberPieces = cardFrameUI.transform.Find("Ember Pieces");
            var capacityPieces = Object.Instantiate(emberPieces, emberPieces.parent, true);
            capacityPieces.name = "Capacity Pieces";

            // Align to right-upper corner
            Vector3 pos = capacityPieces.localPosition;
            // -138.10 -> 73.0
            pos.x += (138.10f + 73.0f);
            capacityPieces.localPosition = pos;

            // Inject CapacityIconBackgroundRingSpriteSelector to the copied MasterySpriteSelector object
            // [Card Frame UI/Ember Pieces/Ember backing]
            var clonedEmberBackingObject = capacityPieces.Find("Ember backing")?.gameObject
                .AddComponent<CapacityIconBackgroundRingSpriteSelector>();

            // Inject CapacityIconSpriteSelector to the copied CardTypeSpriteSelector object
            // [Card Frame UI/Ember Pieces/Ember]
            var clonedEmberObject = capacityPieces.Find("Ember")?.gameObject
                .AddComponent<CapacityIconSpriteSelector>();

            // Add them to spriteSelectors
            if (clonedEmberBackingObject != null)
                spriteSelectors.Add(clonedEmberBackingObject);
            if (clonedEmberObject != null)
                spriteSelectors.Add(clonedEmberObject);
        }

        static void SetupCardStatUpgradeVFX(CardUI cardUI)
        {
            var cardStatUpgradeVFX = cardUI.GetCardStatUpgradeVFX();

            // Copy [Card Frame UI/UpgradeFX/FX_Upgrade_StatsHealer] to [Card Frame UI/FX_UpgradeCapacity Pieces]
            // This object seems unused as there are no passive healers that can be upgraded,
            // and it has a circular FX that matches nicely with capacity icon.
            Transform healerUpgradeVFX = cardStatUpgradeVFX.transform.Find("FX_Upgrade_StatsHealer");
            var capacityUpgradeVFXObject = Object.Instantiate(healerUpgradeVFX, cardStatUpgradeVFX.transform, true);
            capacityUpgradeVFXObject.name = "FX_Upgrade_Capacity";

            // Align to right-upper corner
            Vector3 pos = capacityUpgradeVFXObject.localPosition;
            // -106.40 -> 109.50
            pos.x += (106.40f + 109.50f);
            // -213.20 -> 192.00
            pos.y += (213.20f + 192.00f);
            capacityUpgradeVFXObject.localPosition = pos;

            // Override capacity upgrade VFX to this object
            var capacityUpgradeVFXList = (
                Traverse.Create(cardStatUpgradeVFX.GetComponent<CardStatUpgradeVFX>())
                .Field("capacityUpgradeVFX").GetValue<List<GameObject>>()
            );
            foreach (var oldCapacityUpgradeVFX in capacityUpgradeVFXList)
            {
                // Seems that they initially start as active
                oldCapacityUpgradeVFX.SetActive(false);
            }
            capacityUpgradeVFXList.Clear();
            
            capacityUpgradeVFXList.Add(capacityUpgradeVFXObject.gameObject);
        }

        public static void Setup(CardUI cardUI)
        {
            if (!IsThisCardModded(cardUI))
            {
                SetupCardFrameUI(cardUI.GetCardFrameUI());
                SetupCardStatUpgradeVFX(cardUI);
                cardUI.gameObject.AddComponent(typeof(CapacityLabelContainer));
            }
        }

        public static void UpdateCapacityLabel(this CardUI cardUI, CardState cardState, bool affordable)
        {
            // Capacity version of CardUI.UpdateCostLabel()

            int originalSize = cardState.GetSize(ignoreTempUpgrade: true);
            int currentSize = cardState.GetSize();

            Color color;
            if (!affordable)
                color = new Color(1.0f, 0.302f, 0.302f); // same as CardUI.disabledCostColor
            else if (currentSize > originalSize)
            {
                // Custom color, orange-ish (CapacityChangeHighlighter.sizeIncreaseColor is exactly same as CardUI.disabledCostColor)
                color = new Color(1.0f, 0.549f, 0.0f);
            }
            else if (currentSize < originalSize)
                color = Color.green; // same as CapacityChangeHighlighter.sizeDecreaseColor
            else
                color = Color.white; // same as CardUI.originalCostColor


            TextMeshProUGUI capacityLabel = cardUI.GetComponent<CapacityLabelContainer>().capacityLabel;
            capacityLabel.color = color;
            capacityLabel.SetTextSafe(currentSize.ToString(), localize: false);
            capacityLabel.gameObject.SetActive(value: true);
        }
    }

    public class CapacityLabelContainer : MonoBehaviour
    {
        private TextMeshProUGUI cachedCapacityLabel = null;
        public TextMeshProUGUI capacityLabel
        {
            get
            {
                if (cachedCapacityLabel is null)
                {
                    cachedCapacityLabel = GetComponent<Transform>().Find(
                        "CardCanvas/CardUIContainer/Card front/Card Frame UI/Capacity Pieces/Ember/Card cost label (1)"
                    ).GetComponent<TextMeshProUGUI>();
                }
                return cachedCapacityLabel;
            }
        }
    }

    [HarmonyPatch(typeof(CardUI))]
    [HarmonyPatch(
        "ApplyStateToUI",
        new Type[] {
            typeof(CardState), typeof(CardStatistics), typeof(MonsterManager), typeof(HeroManager), typeof(RelicManager),
            typeof(SaveManager), typeof(CardUI.MasteryType), typeof(bool), typeof(CardUpgradeState), typeof(CardArtPool)
        }
    )]
    class CardMod_CardUI_ApplyStateToUI
    {
        static void Postfix(CardUI __instance, CardState cardState)
        {
            CardUI This = __instance;

            This.UpdateCapacityLabel(cardState, affordable: true);
        }
    }

    // A capacity version of [Card Frame UI/Ember Pieces/Ember backing] (a small ring object that changes color based on mastery)
    public sealed class CapacityIconBackgroundRingSpriteSelector : AbstractSpriteSelector
    {
        private MasterySpriteSelector myMasterySpriteSelectorComponent = null;

        public void SetMastery(CardType cardType, bool isChampion, MasteryFrameType masteryType)
        {
            myMasterySpriteSelectorComponent = myMasterySpriteSelectorComponent ?? GetComponent<MasterySpriteSelector>();

            if (cardType == CardType.Monster)
                myMasterySpriteSelectorComponent.SetMastery(cardType, isChampion, masteryType);
            else
                SetSprite(null);
        }
    }

    // A capacity version of [Card Frame UI/Ember Pieces/Ember] (the main icon)
    public sealed class CapacityIconSpriteSelector : AbstractSpriteSelector
    {
        public void SetIcon(CardType cardType)
        {
            SetSprite((cardType == CardType.Monster) ? MonsterTrainNumberIndicators.Instance.modBigCapacityIcon : null);
        }
    }

    [HarmonyPatch(typeof(CardUI))]
    [HarmonyPatch("DoUpgradeSequence")]
    class CardMod_CardUI_DoUpgradeSequence
    {
        [ThreadStatic]
        public static int callsOnStackFrame = 0;
        
        static void Prefix()
        {
            callsOnStackFrame += 1;
        }

        static void Postfix()
        {
            callsOnStackFrame -= 1;
        }
    }

    [HarmonyPatch(typeof(CardFrameUI))]
    [HarmonyPatch("SetUpFrame")]
    class CardMod_CardFrameUI_SetUpFrame
    {
        static bool Prefix(CardFrameUI __instance, CardState cardState, bool isChampion, MasteryFrameType masteryFrameType)
        {
            CardFrameUI This = __instance;

            if (CardMod_CardUI_DoUpgradeSequence.callsOnStackFrame > 0)
            {
                // We're called from CardUI.DoUpgradeSequence() -- replace the call to CardUI.UpdateCapacityLabel() instead.
                This.GetCardUI().UpdateCapacityLabel(cardState, affordable: true);
                return false;
            }

            CapacityIconBackgroundRingSpriteSelector clonedEmberBackingObject = null;
            CapacityIconSpriteSelector clonedEmberObject = null;

            while (true)
            {
                foreach (AbstractSpriteSelector spriteSelector in This.GetSpriteSelectors())
                {
                    clonedEmberBackingObject = clonedEmberBackingObject ?? spriteSelector as CapacityIconBackgroundRingSpriteSelector;
                    clonedEmberObject = clonedEmberObject ?? spriteSelector as CapacityIconSpriteSelector;
                }

                if ((clonedEmberBackingObject is null) && (clonedEmberObject is null))
                {
                    CardUI cardUI = This.GetCardUI();
                    if (cardUI)
                        CardMod.Setup(cardUI);
                    else
                    {
                        // A card frame from non-card -- journal frame selection page for example? not sure.
                        return true;
                    }
                }
                else
                    break;
            }

            CardType cardType = cardState.GetCardType();
            if (clonedEmberBackingObject)
                clonedEmberBackingObject.SetMastery(cardType, isChampion, masteryFrameType);
            if (clonedEmberObject)
                clonedEmberObject.SetIcon(cardType);

            // Propagate into the original function
            return true;
        }
    }

    [HarmonyPatch(typeof(CapacityChangeHighlighter))]
    [HarmonyPatch("SetUp")]
    class CardMod_CapacityChangeHighlighter_SetUp
    {
        static bool Prefix(CapacityChangeHighlighter __instance)
        {
            CapacityChangeHighlighter This = __instance;

            // Disable the glow object used in upgrade preview [Card Frame UI/Capacity Glow]
            This.Hide();

            // Disable propagating into the original function
            return false;
        }
    }

    [HarmonyPatch(typeof(HandUI))]
    [HarmonyPatch("UpdateCardUI", new Type[] { typeof(CardUI), typeof(CardState), typeof(bool), typeof(bool) })]
    class CardMod_HandUI_UpdateCardUI
    {
        static void Postfix(HandUI __instance, CardUI cardUI, CardState cardState, bool updateText)
        {
            HandUI This = __instance;

            RoomManager roomManager = Traverse.Create(This).Field("roomManager").GetValue<RoomManager>();
            if ((cardUI is null) || (cardState is null) || (roomManager is null))
            {
                MonsterTrainNumberIndicators.Instance.LogInfo(
                    "CardMod_HandUI_UpdateCardUI: failed as one of following necessary component is unavailable: "
                    + $"{cardUI}, {cardState}, {updateText}, {roomManager}"
                );
                return;
            }

            RoomState roomState = roomManager.GetRoom(roomManager.GetSelectedRoom());
            CapacityInfo capacityInfo = roomState.GetCapacityInfo(Team.Type.Monsters);

            if (updateText)
                cardUI.UpdateCapacityLabel(cardState, cardState.GetSize() <= capacityInfo.max - capacityInfo.count);
        }
    }
}
