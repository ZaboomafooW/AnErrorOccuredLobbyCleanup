# AnErrorOccuredLobbyCleanup

Fixes one Steam lobby cleanup problem that can leave a non-host client advertising a lobby after the real Lethal Company host is gone.

> This does **not** fix every cause of **"An error occured!"**, and it does not fix a broken lobby that you are trying to join. It prevents other players from being sent to an orphaned Steam lobby that your client is still holding.

## The problem

Some late-join and lobby-management mods keep players in the Steam lobby after a round starts. That is useful when the actual host is running the mod and intentionally keeps the lobby available for late joining.

The bad case is when only a non-host client keeps that Steam lobby membership. If the real host leaves, disconnects, gets kicked, or gets banned, Steam can leave the non-host client holding the old lobby even though that client is not the Lethal Company server. Steam may also transfer lobby ownership to that client.

Friends can still see **Join Game**, but the lobby no longer points to a valid Lethal Company host. Joining it can lead to **"An error occured!"**.

This mod cleans up that orphaned Steam lobby so other players don't get "An error occured!" trying to join it.

## What it does

- Watches Steam lobby leave, disconnect, kick, and ban events.
- Identifies the original Lethal Company host from player slot 0 when that Steam ID is available. The Steam lobby owner at entry is kept only as a per-lobby fallback until slot 0 can confirm the host.
- If the real host is removed from the current Steam lobby, a non-host client cleans up its copy of that orphaned lobby.
- If Steam has already transferred ownership of the orphaned lobby to that client, the mod marks it non-joinable before cleanup.
- If Steam reports that the local non-host client itself has already been removed from the lobby, the mod clears the retained local Steam lobby bookkeeping without disconnecting the active Netcode game session.
- If a retained Steam lobby survives into `MainMenu`, the mod cleans it up there as a final failsafe.
- If the game is quitting while a Steam lobby is still retained, the mod exits that lobby during shutdown.

The cached host Steam ID is tied to the exact Steam lobby it came from and is cleared when that lobby ends, when a new lobby is entered, on Main Menu, and on application quit. A host ID from a previous lobby is never used for a later lobby.

## What it does not change

- It does not make late joining work by itself.
- It does not change the Lethal Company Netcode host.
- It does not modify player state, round state, spawning, or connection approval.
- It does not close a legitimate Steam lobby that the actual host is intentionally keeping open during normal gameplay.
- It does not fix unrelated causes of **"An error occured!"**.

## Compatibility

- Client-side cleanup for non-host players during gameplay.
- Host-departure cleanup does nothing when you are the Lethal Company host.
- Does not require a specific late-join or lobby-management mod.
- Designed to coexist with mods that intentionally retain the Steam lobby when the actual host is running them.
- Targets Lethal Company v81.

## Installation

Install with Thunderstore Mod Manager/r2modman, or place the DLL in `BepInEx/plugins`.

## Why is "Occured" misspelled?

Because the vanilla Lethal Company message is **"An error occured!"**.
