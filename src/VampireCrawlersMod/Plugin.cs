using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace VampireCrawlersMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "ttat.vampirecrawlers.mod";
    public const string PluginName = "Vampire Crawlers Mod";
    public const string PluginVersion = "0.1.0";

    private readonly Harmony _harmony = new(PluginGuid);

    internal static ManualLogSource Logger { get; private set; }

    public override void Load()
    {
        Logger = base.Log;
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded");

        HandSortButtonController.Configure(Config);
        ClassInjector.RegisterTypeInIl2Cpp<HandSortButtonController>();
        AddComponent<HandSortButtonController>();
        _harmony.PatchAll(typeof(Plugin).Assembly);
    }
}
