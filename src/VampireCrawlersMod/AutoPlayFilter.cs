using BepInEx.Configuration;
using Il2CppInterop.Runtime.Attributes;
using Nosebleed.Pancake.Models;

namespace VampireCrawlersMod;

internal static class AutoPlayFilter
{
    private static ConfigEntry<bool> _onlyPlayWildCards;
    private static ConfigEntry<bool> _skipOneBreakRemainingCards;

    public static void Configure(ConfigFile config)
    {
        _onlyPlayWildCards = config.Bind(
            "AutoPlay",
            "OnlyPlayWildCards",
            true,
            "自动打出只允许打出万能牌。万能牌通过 WildCostType 判断，不按费用是否为 0 判断。");

        _skipOneBreakRemainingCards = config.Bind(
            "AutoPlay",
            "SkipOneBreakRemainingCards",
            true,
            "自动打出跳过碎裂剩余次数为 1 的牌。");
    }

    public static bool TryReplaceAutoPlayCard(PlayerModel player, ref CardModel card)
    {
        bool onlyPlayWildCards = _onlyPlayWildCards?.Value == true;
        bool skipOneBreakRemainingCards = _skipOneBreakRemainingCards?.Value == true;
        if (!onlyPlayWildCards && !skipOneBreakRemainingCards)
        {
            return true;
        }

        if (!onlyPlayWildCards)
        {
            if (CanAutoPlayCard(card, onlyPlayWildCards, skipOneBreakRemainingCards))
            {
                return true;
            }

            CardModel replacement = FindFirstPlayableCard(player, onlyPlayWildCards, skipOneBreakRemainingCards);
            if (replacement == null)
            {
                return false;
            }

            card = replacement;
            return true;
        }

        CardModel preferredCard = FindPreferredPlayableWildCard(player, skipOneBreakRemainingCards);
        if (preferredCard == null)
        {
            return false;
        }

        card = preferredCard;
        return true;
    }

    [HideFromIl2Cpp]
    private static CardModel FindFirstPlayableCard(PlayerModel player, bool onlyPlayWildCards, bool skipOneBreakRemainingCards)
    {
        CardPileModel cardPile = player?.HandPile?.CardPile;
        if (cardPile == null)
        {
            return null;
        }

        for (int i = 0; i < cardPile.Count; i++)
        {
            if (cardPile.TryPeekIndex(i, out CardModel card) && CanAutoPlayCard(card, onlyPlayWildCards, skipOneBreakRemainingCards))
            {
                return card;
            }
        }

        return null;
    }

    [HideFromIl2Cpp]
    private static CardModel FindPreferredPlayableWildCard(PlayerModel player, bool skipOneBreakRemainingCards)
    {
        CardPileModel cardPile = player?.HandPile?.CardPile;
        if (cardPile == null)
        {
            return null;
        }

        CardModel preferredCard = null;
        AutoPlayCardPriority preferredPriority = default;
        for (int i = 0; i < cardPile.Count; i++)
        {
            if (!cardPile.TryPeekIndex(i, out CardModel card) || !CanAutoPlayCard(card, true, skipOneBreakRemainingCards))
            {
                continue;
            }

            AutoPlayCardPriority priority = GetPriority(player, card, i);
            if (preferredCard == null || priority.CompareTo(preferredPriority) < 0)
            {
                preferredCard = card;
                preferredPriority = priority;
            }
        }

        return preferredCard;
    }

    [HideFromIl2Cpp]
    private static bool CanAutoPlayCard(CardModel card, bool onlyPlayWildCards, bool skipOneBreakRemainingCards)
    {
        if (card == null || card.IsBroken)
        {
            return false;
        }

        if (onlyPlayWildCards && !CardRules.IsWildCard(card))
        {
            return false;
        }

        return !skipOneBreakRemainingCards || !CardRules.HasOnlyOnePlayBeforeBreak(card);
    }

    [HideFromIl2Cpp]
    private static AutoPlayCardPriority GetPriority(PlayerModel player, CardModel card, int handIndex)
    {
        return new AutoPlayCardPriority(
            CardRules.IsTemporaryCard(player, card) ? 0 : 1,
            CardRules.HasDestroyOnPlay(card) ? 0 : 1,
            CardRules.HasCrack(card) ? 1 : 0,
            CardRules.GetAutoPlayTypeRank(card),
            handIndex);
    }

    private readonly struct AutoPlayCardPriority
    {
        public AutoPlayCardPriority(int temporaryRank, int destroyRank, int crackRank, int cardTypeRank, int handIndex)
        {
            TemporaryRank = temporaryRank;
            DestroyRank = destroyRank;
            CrackRank = crackRank;
            CardTypeRank = cardTypeRank;
            HandIndex = handIndex;
        }

        private int TemporaryRank { get; }
        private int DestroyRank { get; }
        private int CrackRank { get; }
        private int CardTypeRank { get; }
        private int HandIndex { get; }

        public int CompareTo(AutoPlayCardPriority other)
        {
            int temporaryCompare = TemporaryRank.CompareTo(other.TemporaryRank);
            if (temporaryCompare != 0)
            {
                return temporaryCompare;
            }

            int destroyCompare = DestroyRank.CompareTo(other.DestroyRank);
            if (destroyCompare != 0)
            {
                return destroyCompare;
            }

            int crackCompare = CrackRank.CompareTo(other.CrackRank);
            if (crackCompare != 0)
            {
                return crackCompare;
            }

            int cardTypeCompare = CardTypeRank.CompareTo(other.CardTypeRank);
            return cardTypeCompare != 0 ? cardTypeCompare : HandIndex.CompareTo(other.HandIndex);
        }
    }
}
