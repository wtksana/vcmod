using HarmonyLib;
using Nosebleed.Pancake.Models;
using Nosebleed.Pancake.View;

namespace VampireCrawlersMod;

[HarmonyPatch]
internal static class CardBreakCountdownPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerModel), "Awake")]
    private static void PlayerModel_Awake_Postfix(PlayerModel __instance)
    {
        HandSortButtonController.SetPlayer(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerModel), "OnDestroy")]
    private static void PlayerModel_OnDestroy_Postfix(PlayerModel __instance)
    {
        HandSortButtonController.ClearPlayer(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerModel), nameof(PlayerModel.TryPlayCard))]
    private static void PlayerModel_TryPlayCard_Postfix(bool __result)
    {
        HandSortButtonController.NotifyCardPlayed(__result);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerModel), nameof(PlayerModel.TryPlayCard))]
    private static bool PlayerModel_TryPlayCard_Prefix(PlayerModel __instance, ref CardModel cardModel, bool isAutoPlay, ref bool __result)
    {
        if (!isAutoPlay)
        {
            return true;
        }

        if (AutoPlayFilter.TryReplaceAutoPlayCard(__instance, ref cardModel))
        {
            return true;
        }

        __result = false;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardView), nameof(CardView.GetCardDescription))]
    private static void CardView_GetCardDescription_Postfix(CardView __instance, ref string __result)
    {
        __result = CardBreakCountdownDisplay.AddCountdownLine(__instance, __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnDrawCard))]
    private static void CardModel_OnDrawCard_Postfix(PlayerModel playerModel)
    {
        HandSortButtonController.RequestAutoSortAfterDraw(playerModel);
    }
}
