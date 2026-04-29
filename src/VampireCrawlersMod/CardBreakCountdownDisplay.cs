using Nosebleed.Pancake.View;

namespace VampireCrawlersMod;

internal static class CardBreakCountdownDisplay
{
    public static string AddCountdownLine(CardView cardView, string description)
    {
        int remainingPlays = CardRules.GetRemainingPlaysBeforeBreak(cardView?.CardModel?.BreakableCard);
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
}
