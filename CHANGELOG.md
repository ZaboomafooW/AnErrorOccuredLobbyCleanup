# Changelog

## 1.0.11

- Reverified the orphaned Steam lobby cleanup paths against Lethal Company v81.
- Keeps the same cleanup behavior as 1.0.10 while using the game and BepInEx assemblies installed with Lethal Company for builds.
- Restores the spaced BepInEx display name and keeps the approved Thunderstore icon consistent with the previous release.
- Clarifies the documented host-cache cleanup wording without changing its behavior.
- No lobby-cleanup behavior changed.

## 1.0.10

- Standardizes cleanup wording on "orphaned Steam lobby" in logs and documentation.
- Cleanup logs now explain that the orphaned Steam lobby is being cleaned up to prevent other players from getting "An error occured!".
- No gameplay or network behavior changed.

## 1.0.9

- If a non-host client inherits Steam ownership after the actual Lethal Company host is removed, marks that orphaned lobby non-joinable before cleanup.
- Quarantine only runs after the existing host identity and current-lobby checks have already proven the removed player is the original Lethal Company host.
- If Steam rejects the non-joinable update, normal orphan-lobby cleanup still proceeds.

## 1.0.8

- On a non-host client, clears retained local Steam lobby bookkeeping if Steam reports that this client itself left, disconnected, was kicked, or was banned from its current lobby.
- The self-removal path only nulls `currentLobby`, clears `steamIdsInLobby`, and clears the per-lobby host cache; it does not disconnect or alter the active Netcode game session.

## 1.0.7

- Tightens the per-lobby host Steam ID cache so a verified player-slot-0 host replaces the provisional lobby-entry owner.
- Resets cached lobby and host IDs when the cached membership ends, a new lobby is entered, Main Menu is reached, or the application quits.
- Keeps cached host identity bound to its exact Steam lobby so a previous lobby cannot supply the host ID for a later lobby.

## 1.0.6

- Caches the Steam lobby owner when entering a lobby as a fallback host Steam ID.
- Uses the cached ID only when the v81 player-slot host Steam ID is unavailable or zero, while still requiring the same current Steam lobby.

## 1.0.5

- Adds a Main Menu failsafe that leaves any Steam lobby still retained after returning to the main menu.
- The failsafe only touches Steam lobby state and does not disconnect or alter an active Lethal Company session.

## 1.0.4

- Explicitly leaves any retained Steam lobby when the game application is quitting.
- Covers the v81 shutdown gap where `Disconnect()` does not enter its cleanup path if `StartOfRound.Instance` is already null.

## 1.0.3

- Cleans up a retained Steam lobby when Steam reports that the actual Lethal Company host was kicked from the lobby.
- Cleans up a retained Steam lobby when Steam reports that the actual Lethal Company host was banned from the lobby.

## 1.0.2

- Cleans up a retained Steam lobby when Steam reports that the actual Lethal Company host disconnected from the lobby without leaving normally.

## 1.0.1

- Clarified that the mod does NOT fix "An error occured!" for the player installing it.
- Clarified that it only prevents one specific orphaned-lobby cause of the error for other players trying to join that client.

## 1.0.0

- Initial public release.
- Cleans up a retained Steam lobby on non-host clients when the actual Lethal Company host leaves that lobby.
- Prevents the client from continuing to advertise an orphaned Steam join target after the real host is gone.
