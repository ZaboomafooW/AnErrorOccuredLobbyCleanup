# AnErrorOccuredLobbyCleanup

> **Important:** Does **NOT** fix the error for you nor every cause of it. Installing this mod does **not** make **"An error occured!"** go away when **you** are trying to join somebody else.
>
> It only prevents one specific stale-lobby problem that can cause **other players** to get **"An error occured!"** when they try to join **you**. It cleans up the Steam lobby your client would otherwise leave behind after the real Lethal Company host is gone.

## What problem does this fix?

Some late-join and lobby-management mods intentionally stop players from leaving the Steam lobby when a round starts. That behavior is useful when the actual host has the mod, because the host can keep the Steam lobby around and reopen it later for legitimate late joining.

The problem is when **only a non-host client has that behavior**.

In that situation, the real host can leave, disconnect from, be kicked from, or be banned from the Steam lobby while the modded client stays behind. Steam can then leave that client holding the old lobby even though they are **not** the Lethal Company server.

The result can look like this:

1. The real host is removed from the Steam lobby.
2. A non-host client remains in the Steam lobby because of another mod.
3. Steam keeps the lobby alive with that client still in it.
4. Friends can still see **Join Game** for that client.
5. The displayed lobby is not backed by the actual Lethal Company host anymore.
6. Someone tries to join it and can end up at **"An error occured!"**.

That leftover lobby is what this mod cleans up. It also has a Main Menu failsafe: if Lethal Company reaches the `MainMenu` scene while `currentLobby` is still retained, the mod leaves that stale Steam lobby.

## What this mod does

The mod listens for Steam reporting that lobby members left, disconnected, were kicked, or were banned. On a **non-host client**, if the affected player is the actual Lethal Company host, the mod tells that client to leave the current Steam lobby too. It also caches the Steam lobby owner when entering the lobby and uses that ID only as a fallback when Lethal Company's player-slot host Steam ID is unavailable or zero.

In other words:

**Real host is removed from the Steam lobby -> this client leaves the Steam lobby too.**

That prevents the client from retaining a stale or dead lobby and continuing to appear joinable through Steam when there is no valid server behind that lobby anymore.

As a separate shutdown safety check, if Lethal Company is quitting while `currentLobby` still exists, the mod explicitly leaves that Steam lobby before its own shutdown cleanup finishes. This applies only while the game process is already exiting.

## What this mod prevents

- Your client retaining an orphaned Steam lobby after the real host is removed from it.
- Friends seeing a misleading **Join Game** option pointing at your client when you are not the server.
- One specific path that can cause **"An error occured!"** for other players trying to join you.
- A retained `currentLobby` being skipped by vanilla's quit-time `Disconnect()` path when `StartOfRound.Instance` is already null.

## What this mod does NOT do

- It does **NOT** fix **"An error occured!"** for the player who installs it.
- It does **not** remove **"An error occured!"** from Lethal Company.
- It does **not** fix every cause of **"An error occured!"**.
- It does **not** fix the error when **you** are trying to join somebody else's broken lobby.
- It does **not** make late joining work by itself.
- It does **not** make an invalid lobby valid.
- It does **not** close a legitimate Steam lobby that the actual host is still keeping open during normal gameplay.

If the actual host remains in the Steam lobby, the host-departure cleanup has nothing to clean up and does nothing.

## Compatibility

- Client-side stale-lobby safety fix during gameplay.
- Host-departure cleanup does nothing when you are the host.
- Quit-time cleanup may leave the current Steam lobby for either a host or client because the game application is already exiting.
- Does not require a specific late-join or lobby mod.
- Designed to coexist with mods that intentionally retain the Steam lobby for legitimate late joining when the host is actually running them.
- Targets Lethal Company v81.

## Installation

Install with Thunderstore Mod Manager/r2modman, or place the DLL in your `BepInEx/plugins` folder.

## Why is "Occured" misspelled?

Because the vanilla Lethal Company message is **"An error occured!"**.
