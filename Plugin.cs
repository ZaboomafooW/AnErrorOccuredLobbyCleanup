using BepInEx;
using BepInEx.Logging;
using Steamworks;
using Steamworks.Data;

namespace AnErrorOccuredLobbyCleanup;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "ZaboomafooW.AnErrorOccuredLobbyCleanup";
    public const string PluginName = "An Error Occured Lobby Cleanup";
    public const string PluginVersion = "1.0.0";

    private static ManualLogSource? LogSource;
    private bool _subscribed;

    private void Awake()
    {
        LogSource = Logger;

        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        _subscribed = true;

        Logger.LogInfo($"[AnErrorOccuredLobbyCleanup] Loaded v{PluginVersion}");
        Logger.LogInfo("[AnErrorOccuredLobbyCleanup] Steam lobby host-leave hook succeeded.");
    }

    private void OnApplicationQuit()
    {
        if (_subscribed)
        {
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            _subscribed = false;
        }

        LogSource = null;
    }

    private static void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        GameNetworkManager? gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager == null || gameNetworkManager.disableSteam || gameNetworkManager.isHostingGame)
        {
            return;
        }

        if (!gameNetworkManager.currentLobby.HasValue)
        {
            return;
        }

        Lobby currentLobby = gameNetworkManager.currentLobby.Value;
        if (currentLobby.Id.Value != lobby.Id.Value)
        {
            return;
        }

        StartOfRound? startOfRound = StartOfRound.Instance;
        if (startOfRound == null ||
            startOfRound.allPlayerScripts == null ||
            startOfRound.allPlayerScripts.Length == 0 ||
            startOfRound.allPlayerScripts[0] == null)
        {
            return;
        }

        ulong hostSteamId = startOfRound.allPlayerScripts[0].playerSteamId;
        if (hostSteamId == 0UL || friend.Id.Value != hostSteamId)
        {
            return;
        }

        LogSource?.LogWarning(
            $"[AnErrorOccuredLobbyCleanup] Actual Lethal Company host {hostSteamId} left Steam lobby {lobby.Id.Value}; leaving orphaned lobby.");

        gameNetworkManager.LeaveCurrentSteamLobby();
    }
}
