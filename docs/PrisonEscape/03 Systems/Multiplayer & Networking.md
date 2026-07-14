# Multiplayer & Networking

A secondary FPS layer with lobby, roles, and weapons — largely separate from the prison sim but sharing inventory/item code.

## Architecture

- **Riptide Networking** (embedded DLL: `Assets/Scripts/Multiplayer/RiptideNetworking.dll`) — dedicated server + Unity clients
- Server binary bundled at `StreamingAssets/Server/3dgameserver.exe`
- Tick: Unity fixed step **0.025 s → 40 Hz**; position updates every **2 ticks** (~0.05 s interpolation window); tick resync when divergence > 1
- Client prediction + reconciliation (reconcile when server error > 0.001)

## Scene / boot flow

```
MainMenu → FindGame (Steam persona or typed name) → connect
→ Lobby (map vote: Warehouse / Factory) → MatchStarting
→ LoadGameScene → SceneLoaderCallback → ClientLoadedGame → spawn
```

"Singleplayer" MP mode: `SingleplayerLauncher` starts the local server with `-maxPlayers 1` and connects to `127.0.0.1`. 
(The offline prison sim boots differently — `GameManager` in the prison scene; see [[Prisoner AI & NPCs]].)

## Protocol surface

**Server→Client:** playerSpawned, LobbyState, MapVoteUpdate, MatchStarting, MapOptions, Timer*, PlayerJoinedLobby, LoadGameScene, ReceiveRole, GameTimer, PlayerMovement, Sync, PlayerShot, PlayerHealth*, PlayerDied, PlayerJailed, PlayerDamage, InventoryUpdated*, CraftingSuccess* 
**Client→Server:** name, MapVote, ClientLoadedGame, PlayerInput (tick + bool[10] + forward), PlayerShoot, WeaponChange, TryPickupItem*, TryCraftItem*

\* = **enum defined but no handler/sender implemented yet** (networked inventory/crafting is stubbed).

## Roles & game state

`PlayerRole`: None / Civilian / Shooter · `GameState`: Warmup / InProgress / Ending. Role reveal UI exists; role-based weapon gating is commented out.

## Weapons

`Gun` defaults: range 100, damage 10, fire rate 0.5, mag 30, reload 2 s, Single/Automatic modes. `WeaponSway`, `RecoilController`, hitmarker, bullet holes (20 s), `PingDisplay` (green <60 ms, yellow <120).

## Steam

Steamworks.NET package; **identity only** (persona name for lobby). App ID still test **480** — must change before any Steam release.

## Key files

`Assets/Scripts/Multiplayer/` — `NetworkManager.cs`, `FindGame.cs`, `LobbyManager.cs`, `ClientLobbyHandlers.cs`, `GameClient.cs`, `Player/PlayerController.cs`, `Player/Weapons/`, `Interpolator.cs`, `MessageExtensions.cs`; `Assets/Scripts/SteamManager.cs`

Related: [[Player & Interaction]] · [[Inventory & Items]] · [[Content Inventory]]
