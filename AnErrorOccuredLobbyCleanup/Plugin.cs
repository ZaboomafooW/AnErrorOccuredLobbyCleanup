using AnErrorOccuredLobbyCleanup.Controllers;
using BepInEx;

namespace AnErrorOccuredLobbyCleanup;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "ZaboomafooW.AnErrorOccuredLobbyCleanup";
    public const string PluginName = "AnErrorOccuredLobbyCleanup";
    public const string PluginVersion = "1.0.11";

    private SteamLobbyCleanupController _cleanupController;

    private void Awake()
    {
        Logger.LogInfo($"[{PluginName}] Loaded v{PluginVersion}");

        _cleanupController = new SteamLobbyCleanupController(Logger);
        _cleanupController.Start();

        Logger.LogInfo($"[{PluginName}] Hook succeeded: Steam lobby and scene cleanup callbacks attached.");
    }

    private void OnApplicationQuit()
    {
        if (_cleanupController == null)
        {
            return;
        }

        _cleanupController.Shutdown();
        _cleanupController = null;
    }
}
