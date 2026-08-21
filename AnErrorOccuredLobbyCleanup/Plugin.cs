using BepInEx;
using BepInEx.Logging;
using Steamworks;
using Steamworks.Data;
using UnityEngine.SceneManagement;

namespace AnErrorOccuredLobbyCleanup;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "ZaboomafooW.AnErrorOccuredLobbyCleanup";
    public const string PluginName = "An Error Occured Lobby Cleanup";
    public const string PluginVersion = "1.0.10";

    private static ManualLogSource? LogSource;
    private static ulong CachedLobbyId;
    private static ulong CachedHostSteamId;
    private bool _subscribed;

    private void Awake()
    {
        LogSource = Logger;

        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
        SteamMatchmaking.OnLobbyMemberKicked += OnLobbyMemberKicked;
        SteamMatchmaking.OnLobbyMemberBanned += OnLobbyMemberBanned;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _subscribed = true;

        Logger.LogInfo($"[AnErrorOccuredLobbyCleanup] Loaded v{PluginVersion}");
        Logger.LogInfo("[AnErrorOccuredLobbyCleanup] Steam lobby host-departure hooks succeeded.");
    }

    private void OnApplicationQuit()
    {
        GameNetworkManager? gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager != null && !gameNetworkManager.disableSteam && gameNetworkManager.currentLobby.HasValue)
        {
            Logger.LogInfo("[AnErrorOccuredLobbyCleanup] Application quitting; exiting current Steam lobby.");
            gameNetworkManager.LeaveCurrentSteamLobby();
        }

        ClearHostCache();

        if (_subscribed)
        {
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyMemberKicked -= OnLobbyMemberKicked;
            SteamMatchmaking.OnLobbyMemberBanned -= OnLobbyMemberBanned;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _subscribed = false;
        }

        LogSource = null;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name != "MainMenu")
        {
            return;
        }

        GameNetworkManager? gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager != null && !gameNetworkManager.disableSteam && gameNetworkManager.currentLobby.HasValue)
        {
            LogSource?.LogWarning(
                "[AnErrorOccuredLobbyCleanup] MainMenu retained an orphaned Steam lobby; cleaning it up so other players don't get \"An error occured!\"");

            gameNetworkManager.LeaveCurrentSteamLobby();
        }

        ClearHostCache();
    }

    private static void OnLobbyEntered(Lobby lobby)
    {
        GameNetworkManager? gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager == null || gameNetworkManager.disableSteam || !gameNetworkManager.currentLobby.HasValue)
        {
            ClearHostCache();
            return;
        }

        ulong lobbyId = lobby.Id.Value;
        if (gameNetworkManager.currentLobby.Value.Id.Value != lobbyId)
        {
            return;
        }

        ClearHostCache();

        ulong ownerSteamId = lobby.Owner.Id.Value;
        if (lobbyId == 0UL || ownerSteamId == 0UL)
        {
            return;
        }

        CachedLobbyId = lobbyId;
        CachedHostSteamId = ownerSteamId;

        LogSource?.LogDebug(
            $"[AnErrorOccuredLobbyCleanup] Cached Steam lobby {CachedLobbyId} entry owner {CachedHostSteamId} as provisional host fallback.");
    }

    private static void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        HandleHostDeparture(lobby, friend, "left");
    }

    private static void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
    {
        HandleHostDeparture(lobby, friend, "disconnected from");
    }

    private static void OnLobbyMemberKicked(Lobby lobby, Friend friend, Friend actor)
    {
        HandleHostDeparture(lobby, friend, "was kicked from");
    }

    private static void OnLobbyMemberBanned(Lobby lobby, Friend friend, Friend actor)
    {
        HandleHostDeparture(lobby, friend, "was banned from");
    }

    private static void HandleHostDeparture(Lobby lobby, Friend friend, string departureReason)
    {
        ulong callbackLobbyId = lobby.Id.Value;

        if (friend.Id.Value == SteamClient.SteamId.Value)
        {
            HandleLocalLobbyRemoval(callbackLobbyId, departureReason);
            return;
        }

        GameNetworkManager? gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager == null || gameNetworkManager.disableSteam || gameNetworkManager.isHostingGame)
        {
            return;
        }

        if (!gameNetworkManager.currentLobby.HasValue)
        {
            ClearHostCache();
            return;
        }

        Lobby currentLobby = gameNetworkManager.currentLobby.Value;
        if (currentLobby.Id.Value != callbackLobbyId)
        {
            return;
        }

        if (CachedLobbyId != 0UL && CachedLobbyId != currentLobby.Id.Value)
        {
            ClearHostCache();
        }

        ulong hostSteamId = ResolveOriginalHostSteamId(callbackLobbyId);
        if (hostSteamId == 0UL || friend.Id.Value != hostSteamId)
        {
            return;
        }

        LogSource?.LogWarning(
            $"[AnErrorOccuredLobbyCleanup] Actual Lethal Company host {hostSteamId} {departureReason} Steam lobby {callbackLobbyId}; cleaning up orphaned Steam lobby so other players don't get \"An error occured!\"");

        QuarantineInheritedLobbyIfOwned(lobby, hostSteamId);
        gameNetworkManager.LeaveCurrentSteamLobby();
        ClearHostCacheForLobby(callbackLobbyId);
    }

    private static void QuarantineInheritedLobbyIfOwned(Lobby lobby, ulong hostSteamId)
    {
        try
        {
            ulong localSteamId = SteamClient.SteamId.Value;
            if (localSteamId == 0UL || localSteamId == hostSteamId || lobby.Owner.Id.Value != localSteamId)
            {
                return;
            }

            bool quarantined = lobby.SetJoinable(false);
            if (quarantined)
            {
                LogSource?.LogWarning(
                    $"[AnErrorOccuredLobbyCleanup] This client inherited Steam lobby {lobby.Id.Value}; marked it non-joinable before cleanup.");
            }
            else
            {
                LogSource?.LogWarning(
                    $"[AnErrorOccuredLobbyCleanup] This client inherited Steam lobby {lobby.Id.Value}, but Steam did not accept the non-joinable quarantine; cleanup will continue.");
            }
        }
        catch (System.Exception exception)
        {
            LogSource?.LogWarning(
                $"[AnErrorOccuredLobbyCleanup] Failed to quarantine inherited Steam lobby {lobby.Id.Value}: {exception.Message}. Cleanup will continue.");
        }
    }

    private static void HandleLocalLobbyRemoval(ulong lobbyId, string removalReason)
    {
        GameNetworkManager? gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager == null || gameNetworkManager.disableSteam || gameNetworkManager.isHostingGame)
        {
            ClearHostCacheForLobby(lobbyId);
            return;
        }

        if (!gameNetworkManager.currentLobby.HasValue)
        {
            ClearHostCacheForLobby(lobbyId);
            return;
        }

        Lobby currentLobby = gameNetworkManager.currentLobby.Value;
        if (currentLobby.Id.Value != lobbyId)
        {
            ClearHostCacheForLobby(lobbyId);
            return;
        }

        LogSource?.LogWarning(
            $"[AnErrorOccuredLobbyCleanup] Steam reports this client {removalReason} lobby {lobbyId}; clearing retained local lobby state.");

        gameNetworkManager.SetCurrentLobbyNull();
        gameNetworkManager.steamIdsInLobby.Clear();
        ClearHostCacheForLobby(lobbyId);
    }

    private static ulong ResolveOriginalHostSteamId(ulong lobbyId)
    {
        ulong liveHostSteamId = GetLiveHostSteamId();
        if (liveHostSteamId != 0UL)
        {
            if (CachedLobbyId != lobbyId || CachedHostSteamId != liveHostSteamId)
            {
                CachedLobbyId = lobbyId;
                CachedHostSteamId = liveHostSteamId;

                LogSource?.LogDebug(
                    $"[AnErrorOccuredLobbyCleanup] Confirmed Lethal Company host {CachedHostSteamId} for Steam lobby {CachedLobbyId} from player slot 0.");
            }

            return liveHostSteamId;
        }

        if (CachedLobbyId == lobbyId)
        {
            return CachedHostSteamId;
        }

        return 0UL;
    }

    private static ulong GetLiveHostSteamId()
    {
        StartOfRound? startOfRound = StartOfRound.Instance;
        if (startOfRound == null ||
            startOfRound.allPlayerScripts == null ||
            startOfRound.allPlayerScripts.Length == 0 ||
            startOfRound.allPlayerScripts[0] == null)
        {
            return 0UL;
        }

        return startOfRound.allPlayerScripts[0].playerSteamId;
    }

    private static void ClearHostCacheForLobby(ulong lobbyId)
    {
        if (CachedLobbyId != lobbyId)
        {
            return;
        }

        ClearHostCache();
    }

    private static void ClearHostCache()
    {
        CachedLobbyId = 0UL;
        CachedHostSteamId = 0UL;
    }
}
