using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Riptide;

public class ClientLobbyHandlers : MonoBehaviour
{
    [MessageHandler((ushort)ServerToClientId.MapOptions)]
    private static void MapOptionsReceived(Message message)
    {
        int count = message.GetInt();
        string[] mapNames = new string[count];

        for (int i = 0; i < count; i++)
            mapNames[i] = message.GetString();

        Debug.Log("[Client] Received map options: " + string.Join(", ", mapNames));

        LobbyManager.Singleton?.ApplyMapOptionsFromServer(mapNames);
    }

    [MessageHandler((ushort)ServerToClientId.LobbyState)]
    private static void LobbyStateReceived(Message message)
    {
        var state = (LobbyManager.LobbyState)message.GetByte();
        ushort endTick = message.GetUShort(); 

        LobbyManager.Singleton?.SetLobbyState(state, endTick);
    }

    [MessageHandler((ushort)ServerToClientId.MapVoteUpdate)]
    private static void MapVoteUpdate(Message message)
    {
        int mapCount = message.GetInt();
        for (int i = 0; i < mapCount; i++)
        {
            string mapName = message.GetString();
            int votes = message.GetInt();
            LobbyManager.Singleton?.UpdateMapVotes(mapName, votes);
        }
    }

    [MessageHandler((ushort)ServerToClientId.MatchStarting)]
    private static void MatchStarting(Message message)
    {
        string mapName = message.GetString();
        LobbyManager.Singleton?.ShowWinningMapPanel(mapName);
    }

    [MessageHandler((ushort)ServerToClientId.PlayerJoinedLobby)]
    private static void BroadcastLobbyRoster(Message message)
    {
        int count = message.GetInt();
        var roster = new Dictionary<ushort, string>();

        for(int i = 0; i < count; i++)
        {
            ushort clientId = message.GetUShort();
            string name = message.GetString();
            roster[clientId] = name;
        }

        LobbyManager.Singleton?.SetLobbyRoster(roster);
    }
}