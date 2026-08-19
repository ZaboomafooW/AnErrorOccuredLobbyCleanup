# AnErrorOccuredLobbyCleanup

> **Important:** This mod does **not** remove or generally fix Lethal Company's **"An error occured!"** message. It does **not** fix errors you get while trying to join somebody else, and it does not make broken lobbies joinable.

This mod prevents one specific cause of that error **for other players trying to join you**: a stale Steam lobby that still points at your client after the real Lethal Company host has already left it.

## What problem does this fix?

Some late-join and lobby-management mods intentionally stop players from leaving the Steam lobby when a round starts. That behavior is useful when the actual host has the mod, because the host can keep the Steam lobby around and reopen it later for legitimate late joining.

The problem is when **only a non-host client has that behavior**.

In that situation, the real host can leave the Steam lobby normally when the round starts while the modded client stays behind. Steam can then leave that client holding the old lobby even though they are **not** the Lethal Company server. Because Steam still sees a member in that lobby, the stale entry can remain advertised for as long as that client continues to hold it.

The result can look like this:

1. The real host starts the round and leaves the Steam lobby.
2. A non-host client remains in the Steam lobby because of another mod.
3. Steam keeps the lobby alive with that client still in it.
4. Friends can still see **Join Game** for that client.
5. The displayed lobby is not backed by the actual Lethal Company host anymore.
6. Someone tries to join it and can end up at **"An error occured!"**.

That leftover lobby is what this mod cleans up.

## What this mod does

The mod listens for Steam lobby members leaving. On a **non-host client**, if the player who left is the actual Lethal Company host, the mod tells that client to leave the current Steam lobby too.

In other words:

**Real host leaves the Steam lobby -> this client leaves the Steam lobby too.**

That prevents the client from retaining a stale or dead lobby and continuing to appear joinable through Steam when there is no valid server behind that lobby anymore.

## What this mod prevents

- Your client retaining an orphaned Steam lobby after the real host leaves it.
- Friends seeing a misleading **Join Game** option pointing at your client when you are not the server.
- One specific path that can cause **"An error occured!"** for other players trying to join you.

## What this mod does NOT do

- It does **not** remove **"An error occured!"** from Lethal Company.
- It does **not** fix every cause of **"An error occured!"**.
- It does **not** fix the error when **you** are trying to join somebody else's broken lobby.
- It does **not** make late joining work by itself.
- It does **not** make an invalid lobby valid.
- It does **not** close a legitimate Steam lobby that the actual host is still keeping open.

If the actual host remains in the Steam lobby, this mod has nothing to clean up and does nothing.

## Compatibility

- Client-side safety fix.
- Does nothing when you are the host.
- Does not require a specific late-join or lobby mod.
- Designed to coexist with mods that intentionally retain the Steam lobby for legitimate late joining when the host is actually running them.
- Targets Lethal Company v81.

## Installation

Install with Thunderstore Mod Manager/r2modman, or place the DLL in your `BepInEx/plugins` folder.

## Why is "Occured" misspelled?

Because the vanilla Lethal Company message is **"An error occured!"**.
