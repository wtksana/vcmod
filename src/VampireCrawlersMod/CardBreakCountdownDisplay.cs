using BepInEx.Configuration;
using Nosebleed.Pancake.View;

namespace VampireCrawlersMod;

internal static class CardBreakCountdownDisplay
{
    private const string DefaultCountdownColor = "#00ff66";

    private static ConfigEntry<string> _countdownColor;
    private static string _lastConfiguredColor;
    private static string _cachedCountdownColor = DefaultCountdownColor;

    public static void Configure(ConfigFile config)
    {
        _countdownColor = config.Bind(
            "CardBreakCountdown",
            "TextColor",
            DefaultCountdownColor,
            "碎裂剩余次数提示文字颜色，格式为 #RRGGBB 或 #RRGGBBAA。");
    }

    public static string AddCountdownLine(CardView cardView, string description)
    {
        int remainingPlays = CardRules.GetRemainingPlaysBeforeBreak(cardView?.CardModel);
        if (remainingPlays <= 0)
        {
            return description;
        }

        string countdownLine = $"<color={GetCountdownColor()}>碎裂剩余 {remainingPlays}</color>";
        if (string.IsNullOrEmpty(description))
        {
            return countdownLine;
        }

        return $"{countdownLine}\n{description}";
    }

    private static string GetCountdownColor()
    {
        string configuredColor = _countdownColor?.Value;
        if (configuredColor == _lastConfiguredColor)
        {
            return _cachedCountdownColor;
        }

        _lastConfiguredColor = configuredColor;
        _cachedCountdownColor = IsValidHexColor(configuredColor) ? configuredColor : DefaultCountdownColor;
        return _cachedCountdownColor;
    }

    private static bool IsValidHexColor(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] != '#')
        {
            return false;
        }

        int hexLength = value.Length - 1;
        if (hexLength != 6 && hexLength != 8)
        {
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            char c = value[i];
            bool isHex =
                c >= '0' && c <= '9' ||
                c >= 'a' && c <= 'f' ||
                c >= 'A' && c <= 'F';
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }
}
