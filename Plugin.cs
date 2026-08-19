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
    public const string PluginVersion = "1.0.2";

    private static ManualLogSource? LogSource;
    private bool _subscribed;

    private void Awake()
    {
        LogSource = Logger;

        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
        _subscribed = true;

        Logger.LogInfo($"[AnErrorOccuredLobbyCleanup] Loaded v{PluginVersion}");
        Logger.LogInfo("[AnErrorOccuredLobbyCleanup] Steam lobby host-departure hooks succeeded.");
    }

    private void OnApplicationQuit()
    {
        if (_subscribed)
        {
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            _subscribed = false;
        }

        LogSource = null;
    }

    private static void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        HandleHostDeparture(lobby, friend, "left");
    }

    private static void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
    {
        HandleHostDeparture(lobby, friend, "disconnected from");
    }

    private static void HandleHostDeparture(Lobby lobby, Friend friend, string departureReason)
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
            $"[AnErrorOccuredLobbyCleanup] Actual Lethal Company host {hostSteamId} {departureReason} Steam lobby {lobby.Id.Value}; leaving orphaned lobby.");

        gameNetworkManager.LeaveCurrentSteamLobby();
    }
}
