using System;
using Nosebleed.Pancake.GameConfig;
using Nosebleed.Pancake.GameLogic;
using Nosebleed.Pancake.Models;

namespace VampireCrawlersMod;

internal static class CardRules
{
    public static bool IsWildCard(CardModel card)
    {
        try
        {
            CardCostType costType = card?.CardCostType;
            if (costType == null)
            {
                return false;
            }

            return costType is WildCostType || costType.TryCast<WildCostType>() != null;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Unable to read card cost type: {ex.Message}");
            return false;
        }
    }

    public static int GetRemainingPlaysBeforeBreak(CardModel card)
    {
        return GetRemainingPlaysBeforeBreak(card?.BreakableCard);
    }

    public static int GetRemainingPlaysBeforeBreak(BreakableCard breakableCard)
    {
        if (breakableCard == null || breakableCard.CrackState != CardCrackState.Cracked)
        {
            return 0;
        }

        CardModel cardModel = breakableCard.GetComponent<CardModel>();
        if (cardModel != null && cardModel.IsBroken)
        {
            return 0;
        }

        int crackingStages = 0;
        try
        {
            crackingStages = GlobalConfig.Instance.cardCrackConfig.crackingStages;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Unable to read card crack config: {ex.Message}");
        }

        int remainingByStage = crackingStages - breakableCard.CardCrackStage;
        return remainingByStage > 0 ? remainingByStage : 0;
    }

    public static bool HasOnlyOnePlayBeforeBreak(CardModel card)
    {
        return GetRemainingPlaysBeforeBreak(card) == 1;
    }
}
