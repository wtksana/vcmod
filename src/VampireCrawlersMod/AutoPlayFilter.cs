using BepInEx.Configuration;
using Il2CppInterop.Runtime.Attributes;
using Nosebleed.Pancake.Models;

namespace VampireCrawlersMod;

internal static class AutoPlayFilter
{
    private static ConfigEntry<bool> _onlyPlayWildSafeCards;

    public static void Configure(ConfigFile config)
    {
        _onlyPlayWildSafeCards = config.Bind(
            "AutoPlay",
            "OnlyPlayWildSafeCards",
            true,
            "自动打出只允许打出万能牌，并跳过碎裂剩余次数为 1 的牌。万能牌通过 WildCostType 判断，不按费用是否为 0 判断。");
    }

    public static bool TryReplaceAutoPlayCard(PlayerModel player, ref CardModel card)
    {
        if (_onlyPlayWildSafeCards?.Value != true)
        {
            return true;
        }

        if (CanAutoPlayCard(card))
        {
            return true;
        }

        CardModel replacement = FindFirstPlayableWildSafeCard(player);
        if (replacement == null)
        {
            return false;
        }

        card = replacement;
        return true;
    }

    [HideFromIl2Cpp]
    private static CardModel FindFirstPlayableWildSafeCard(PlayerModel player)
    {
        CardPileModel cardPile = player?.HandPile?.CardPile;
        if (cardPile == null)
        {
            return null;
        }

        for (int i = 0; i < cardPile.Count; i++)
        {
            if (cardPile.TryPeekIndex(i, out CardModel card) && CanAutoPlayCard(card))
            {
                return card;
            }
        }

        return null;
    }

    [HideFromIl2Cpp]
    private static bool CanAutoPlayCard(CardModel card)
    {
        if (card == null || card.IsBroken)
        {
            return false;
        }

        return CardRules.IsWildCard(card) && !CardRules.HasOnlyOnePlayBeforeBreak(card);
    }
}
