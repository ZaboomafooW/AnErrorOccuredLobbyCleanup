using System;
using BepInEx.Logging;
using Steamworks;
using Steamworks.Data;
using UnityEngine.SceneManagement;

namespace AnErrorOccuredLobbyCleanup.Controllers;

internal sealed class SteamLobbyCleanupController
{
    private readonly ManualLogSource _logger;
    private ulong _cachedLobbyId;
    private ulong _cachedHostSteamId;
    private bool _isSubscribed;

    public SteamLobbyCleanupController(ManualLogSource logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        if (_isSubscribed)
        {
            return;
        }

        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
        SteamMatchmaking.OnLobbyMemberKicked += OnLobbyMemberKicked;
        SteamMatchmaking.OnLobbyMemberBanned += OnLobbyMemberBanned;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _isSubscribed = true;
    }

    public void Shutdown()
    {
        LeaveRetainedLobbyOnApplicationQuit();
        ClearHostCache();

        if (!_isSubscribed)
        {
            return;
        }

        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
        SteamMatchmaking.OnLobbyMemberKicked -= OnLobbyMemberKicked;
        SteamMatchmaking.OnLobbyMemberBanned -= OnLobbyMemberBanned;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _isSubscribed = false;
    }

    private void LeaveRetainedLobbyOnApplicationQuit()
    {
        GameNetworkManager gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager == null || gameNetworkManager.disableSteam || !gameNetworkManager.currentLobby.HasValue)
        {
            return;
        }

        _logger.LogInfo($"[{Plugin.PluginName}] Application quitting; exiting current Steam lobby.");
        gameNetworkManager.LeaveCurrentSteamLobby();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name != "MainMenu")
        {
            return;
        }

        GameNetworkManager gameNetworkManager = GameNetworkManager.Instance;
        if (gameNetworkManager != null && !gameNetworkManager.disableSteam && gameNetworkManager.currentLobby.HasValue)
        {
            _logger.LogWarning(
                $"[{Plugin.PluginName}] Feature executed: MainMenu retained an orphaned Steam lobby; cleaning it up so other players don't get \"An error occured!\"");

            gameNetworkManager.LeaveCurrentSteamLobby();
        }

        ClearHostCache();
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        GameNetworkManager gameNetworkManager = GameNetworkManager.Instance;
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

        _cachedLobbyId = lobbyId;
        _cachedHostSteamId = ownerSteamId;

        _logger.LogDebug(
            $"[{Plugin.PluginName}] Cached Steam lobby {_cachedLobbyId} entry owner {_cachedHostSteamId} as provisional host fallback.");
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        HandleHostDeparture(lobby, friend, "left");
    }

    private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
    {
        HandleHostDeparture(lobby, friend, "disconnected from");
    }

    private void OnLobbyMemberKicked(Lobby lobby, Friend friend, Friend actor)
    {
        HandleHostDeparture(lobby, friend, "was kicked from");
    }

    private void OnLobbyMemberBanned(Lobby lobby, Friend friend, Friend actor)
    {
        HandleHostDeparture(lobby, friend, "was banned from");
    }

    private void HandleHostDeparture(Lobby lobby, Friend friend, string departureReason)
    {
        ulong callbackLobbyId = lobby.Id.Value;

        if (friend.Id.Value == SteamClient.SteamId.Value)
        {
            HandleLocalLobbyRemoval(callbackLobbyId, departureReason);
            return;
        }

        GameNetworkManager gameNetworkManager = GameNetworkManager.Instance;
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

        if (_cachedLobbyId != 0UL && _cachedLobbyId != currentLobby.Id.Value)
        {
            ClearHostCache();
        }

        ulong hostSteamId = ResolveOriginalHostSteamId(callbackLobbyId);
        if (hostSteamId == 0UL || friend.Id.Value != hostSteamId)
        {
            return;
        }

        _logger.LogWarning(
            $"[{Plugin.PluginName}] Feature executed: actual Lethal Company host {hostSteamId} {departureReason} Steam lobby {callbackLobbyId}; cleaning up the orphaned Steam lobby so other players don't get \"An error occured!\"");

        QuarantineInheritedLobbyIfOwned(lobby, hostSteamId);
        gameNetworkManager.LeaveCurrentSteamLobby();
        ClearHostCacheForLobby(callbackLobbyId);
    }

    private void QuarantineInheritedLobbyIfOwned(Lobby lobby, ulong hostSteamId)
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
                _logger.LogWarning(
                    $"[{Plugin.PluginName}] This client inherited Steam lobby {lobby.Id.Value}; marked it non-joinable before cleanup.");
            }
            else
            {
                _logger.LogWarning(
                    $"[{Plugin.PluginName}] This client inherited Steam lobby {lobby.Id.Value}, but Steam did not accept the non-joinable quarantine; cleanup will continue.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                $"[{Plugin.PluginName}] Failed to quarantine inherited Steam lobby {lobby.Id.Value}: {exception.Message}. Cleanup will continue.");
        }
    }

    private void HandleLocalLobbyRemoval(ulong lobbyId, string removalReason)
    {
        GameNetworkManager gameNetworkManager = GameNetworkManager.Instance;
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

        _logger.LogWarning(
            $"[{Plugin.PluginName}] Feature executed: Steam reports this client {removalReason} lobby {lobbyId}; clearing retained local lobby state.");

        gameNetworkManager.SetCurrentLobbyNull();
        gameNetworkManager.steamIdsInLobby.Clear();
        ClearHostCacheForLobby(lobbyId);
    }

    private ulong ResolveOriginalHostSteamId(ulong lobbyId)
    {
        ulong liveHostSteamId = GetLiveHostSteamId();
        if (liveHostSteamId != 0UL)
        {
            if (_cachedLobbyId != lobbyId || _cachedHostSteamId != liveHostSteamId)
            {
                _cachedLobbyId = lobbyId;
                _cachedHostSteamId = liveHostSteamId;

                _logger.LogDebug(
                    $"[{Plugin.PluginName}] Confirmed Lethal Company host {_cachedHostSteamId} for Steam lobby {_cachedLobbyId} from player slot 0.");
            }

            return liveHostSteamId;
        }

        if (_cachedLobbyId == lobbyId)
        {
            return _cachedHostSteamId;
        }

        return 0UL;
    }

    private static ulong GetLiveHostSteamId()
    {
        StartOfRound startOfRound = StartOfRound.Instance;
        if (startOfRound == null ||
            startOfRound.allPlayerScripts == null ||
            startOfRound.allPlayerScripts.Length == 0 ||
            startOfRound.allPlayerScripts[0] == null)
        {
            return 0UL;
        }

        // v81 StartOfRound.IsClientFriendsWithHost identifies allPlayerScripts[0] as the host.
        return startOfRound.allPlayerScripts[0].playerSteamId;
    }

    private void ClearHostCacheForLobby(ulong lobbyId)
    {
        if (_cachedLobbyId != lobbyId)
        {
            return;
        }

        ClearHostCache();
    }

    private void ClearHostCache()
    {
        _cachedLobbyId = 0UL;
        _cachedHostSteamId = 0UL;
    }
}
