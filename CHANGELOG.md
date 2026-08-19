# Changelog

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
- Clarified that it only prevents one specific stale-lobby cause of the error for other players trying to join that client.

## 1.0.0

- Initial public release.
- Cleans up a retained Steam lobby on non-host clients when the actual Lethal Company host leaves that lobby.
- Prevents the client from continuing to advertise a stale Steam join target after the real host is gone.
