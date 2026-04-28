using System;
using Il2CppInterop.Runtime.Attributes;
using Nosebleed.Pancake.GameConfig;
using Nosebleed.Pancake.GameLogic;
using Nosebleed.Pancake.Models;
using Nosebleed.Pancake.View;
using UnityEngine;

namespace VampireCrawlersMod;

public sealed class CardBreakCountdownDisplay : MonoBehaviour
{
    public CardBreakCountdownDisplay(IntPtr ptr) : base(ptr)
    {
    }

    [HideFromIl2Cpp]
    public static string AddCountdownLine(CardView cardView, string description)
    {
        int remainingPlays = GetRemainingPlaysBeforeBreak(cardView?.CardModel?.BreakableCard);
        if (remainingPlays <= 0)
        {
            return description;
        }

        string countdownLine = $"<color=#ff140a>碎裂剩余 {remainingPlays}</color>";
        if (string.IsNullOrEmpty(description))
        {
            return countdownLine;
        }

        return $"{countdownLine}\n{description}";
    }

    [HideFromIl2Cpp]
    private static int GetRemainingPlaysBeforeBreak(BreakableCard breakableCard)
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
}
