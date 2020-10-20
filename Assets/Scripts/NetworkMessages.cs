using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands
    {
        NEW_CLIENT,
        CLIENT_DROP,
        CLIENT_LIST,
        PLAYER_UPDATE,
        SERVER_UPDATE,
        PLAYER_ID,
        HeartBeat,
    }

    [System.Serializable]
    public class NetworkHeader
    {
        public Commands cmd;
    }

    [System.Serializable]
    public class NewClientMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;
        public NewClientMsg()
        {
            cmd = Commands.NEW_CLIENT;
            player = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class DropPlayerMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] players;
        public DropPlayerMsg(List<NetworkObjects.NetworkPlayer> playerList)
        {
            cmd = Commands.CLIENT_DROP;
            players = new NetworkObjects.NetworkPlayer[playerList.Count];

            int i = 0;
            foreach(NetworkObjects.NetworkPlayer player in playerList)
            {
                players[i] = player;
                i++;
            }
        }
    }

    [System.Serializable]
    public class PlayerListMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] players;
        public PlayerListMsg(List<NetworkObjects.NetworkPlayer> playerList)
        {
            cmd = Commands.CLIENT_LIST;
            players = new NetworkObjects.NetworkPlayer[playerList.Count];

            int i = 0;
            foreach(NetworkObjects.NetworkPlayer player in playerList)
            {
                players[i] = player;
                i++;
            }
        }
    }

    [System.Serializable]
    public class PlayerUpdateMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] updatedPlayers;
        public PlayerUpdateMsg(List<NetworkObjects.NetworkPlayer> playerList)
        {
            cmd = Commands.PLAYER_UPDATE;
            updatedPlayers = new NetworkObjects.NetworkPlayer[playerList.Count];

            int i = 0;
            foreach (NetworkObjects.NetworkPlayer player in playerList)
            {
                updatedPlayers[i] = player;
                i++;
            }
        }
    }

    [System.Serializable]
    public class ServerUpdateMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;
        public ServerUpdateMsg()
        {
            player = new NetworkObjects.NetworkPlayer();
            cmd = Commands.SERVER_UPDATE;
        }
    }
}

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject
    {
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject
    {
        public System.DateTime prevBeat;

        public Vector3 cubePos;
        public Vector3 cubeRot; //Euler 

        public Color cubeColor;

        public NetworkPlayer()
        {
            prevBeat = System.DateTime.Now;

            cubePos = new Vector3(0.0f, 0.0f, 0.0f);
            cubeRot = new Vector3(0.0f, 0.0f, 0.0f);

            cubeColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
        }
    }
}
