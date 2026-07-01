# GEMINI.md - Project Context

This document provides a comprehensive overview of the Unity 3D multiplayer game project for AI-assisted development.

## Project Overview

This is a multiplayer first-person shooter game built with Unity and C#. It utilizes the Riptide Networking library for its client-server communication and integrates with Steamworks for player identity.

The game features a lobby system where players can gather before a match, vote on a map, and then are transitioned into the game scene. The core gameplay seems to involve roles, specifically "Shooter" and "Civilian", suggesting a social deduction or asymmetrical objective-based game mode.

### Core Technologies

*   **Game Engine:** Unity
*   **Programming Language:** C#
*   **Networking:** Riptide Networking (a lightweight C# networking library)
*   **Platform Integration:** Steamworks.NET (for Steam integration)

### Key Architectural Concepts

*   **Client-Server Architecture:** The project follows a dedicated server model. The client is responsible for sending inputs and rendering the game state, while the server holds the authoritative game state.
*   **Tick-Based Synchronization:** The game uses a custom tick system (`NetworkManager.ServerTick`, `NetworkManager.LocalTick`) to ensure that client and server simulations are synchronized. This is crucial for fair and accurate gameplay.
*   **Client-Side Prediction & Server Reconciliation:** The `PlayerController` implements client-side prediction, allowing for responsive local movement. It sends inputs to the server and then reconciles its position based on the authoritative state sent back by the server, correcting any discrepancies.
*   **Interpolation for Remote Players:** Remote players' movement is smoothed using an `Interpolator` component to avoid jittery movement caused by network latency.
*   **Singleton Pattern:** Many core manager classes (`GameLogic`, `NetworkManager`, `LobbyManager`) are implemented as Singletons for easy global access.
*   **Message-Based Communication:** The client and server communicate by passing messages defined in the `ServerToClientId` and `ClientToServerId` enums. Riptide's `MessageHandler` attribute is used to route incoming messages to the correct methods.

## Building and Running

This is a Unity project. To run or build the game, you must use the Unity Editor.

1.  Open the project's root folder in the Unity Hub.
2.  Open the project with the appropriate Unity Editor version (the exact version is not specified in the scripts).
3.  To run in the editor, open a scene (e.g., the main menu or a test scene) and press the "Play" button.
4.  To build the project, go to `File > Build Settings`, select your target platform (e.g., Windows), and click "Build".

**Note:** A running instance of the game's server is required for the client to connect to.

## Key Files and Directories

*   `Assets/Scripts/`: The main directory for all C# scripts.
*   `GameLogic.cs`: A central singleton holding references to essential game prefabs like the local and remote player models.
*   `SteamManager.cs`: Handles initialization and callbacks for the Steamworks API.
*   `Multiplayer/NetworkManager.cs`: The heart of the networking layer. It manages the Riptide client, connection state, and tick synchronization. It also contains the crucial `ServerToClientId` and `ClientToServerId` message enums.
*   `Multiplayer/LobbyManager.cs`: Manages the pre-game lobby UI and state, including map voting and countdowns.
*   `Multiplayer/GameClient.cs`: Handles client-side logic during the main game phase, such as receiving and displaying role information.
*   `Multiplayer/Player/Player.cs`: The core script for any player instance in the game. It handles spawning, stores player data (ID, username), and receives state updates from the server.
*   `Multiplayer/Player/PlayerController.cs`: This script is only on the *local* player. It captures keyboard/mouse input, performs client-side prediction, and sends inputs to the server.
*   `Multiplayer/Interpolator.cs`: Used on remote player instances to smoothly interpolate their position between network updates.
*   `RiptideNetworking.dll`: The compiled library for the Riptide Networking engine.
