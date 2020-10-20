using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public GameObject playerCube;
    public string playerID;
    private List<Player> playerList = new List<Player>();

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }

    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect()
    {
        Debug.Log("Client Msg: We are now connected to the server");
    }

    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.NEW_CLIENT:
                NewClientMsg ncMsg1 = JsonUtility.FromJson<NewClientMsg>(recMsg);
                SpawnPlayer(ncMsg1.player);
                Debug.Log("Client Msg: New Client message received!");
                break;
            case Commands.CLIENT_DROP:
                DropPlayerMsg dpMsg = JsonUtility.FromJson<DropPlayerMsg>(recMsg);
                RemovePlayers(dpMsg.players);
                Debug.Log("Client Msg: Client drop message received!");
                break;
            case Commands.CLIENT_LIST:
                PlayerListMsg nplMsg = JsonUtility.FromJson<PlayerListMsg>(recMsg);
                for (int i = 0; i < nplMsg.players.Length; i++)
                {
                    SpawnPlayer(nplMsg.players[i]);
                }
                Debug.Log("Client Msg: Client list message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                PlayerUpdate(puMsg.updatedPlayers);
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Client Msg: Server update message received!");
                break;
            case Commands.PLAYER_ID:
                NewClientMsg ncMsg2 = JsonUtility.FromJson<NewClientMsg>(recMsg);
                playerID = ncMsg2.player.id;
                SpawnPlayer(ncMsg2.player);
                break;
            default:
                Debug.Log("Client Msg: Unrecognized message received!");
                break;
        }
    }

    private void SpawnPlayer(NetworkObjects.NetworkPlayer player)
    {
        foreach (Player p in playerList)
        {
            if (p.ID == player.id)
                return;
        }

        Player temp = Instantiate(playerCube, player.cubePos, Quaternion.Euler(player.cubeRot)).GetComponent<Player>();

        temp.ID = player.id;
        if (temp.networkClient.playerID == temp.ID)
            temp.isSmaeID = true;

        temp.transform.position = player.cubePos;
        temp.transform.eulerAngles = player.cubeRot;

        temp.gameObject.GetComponent<Renderer>().material.color = player.cubeColor;

        playerList.Add(temp);
    }

    private void RemovePlayers(NetworkObjects.NetworkPlayer[] players)
    {
        foreach (NetworkObjects.NetworkPlayer player in players)
        {
            for (int j = 0; j < playerList.Count; j++)
            {
                if (player.id == playerList[j].ID)
                {
                    Destroy(playerList[j].gameObject);
                    playerList.RemoveAt(j--);
                }
            }
        }
    }

    private void PlayerUpdate(NetworkObjects.NetworkPlayer[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            foreach (Player p in playerList)
            {
                if (p.ID == players[i].id)
                {
                    p.transform.position = players[i].cubePos;
                    p.transform.rotation = Quaternion.Euler(players[i].cubeRot);
                }
                Debug.Log("[ID: " + p.ID + ", Position: "
                    + p.transform.position + ", rotation: " + p.transform.rotation + "]");
            }
        }
    }

    public void UpdatePlayer(GameObject playercube)
    {
        Player tempPlayer = playercube.GetComponent<Player>();

        if(tempPlayer != null)
        {
            ServerUpdateMsg serverUpdateMsg = new ServerUpdateMsg();
            serverUpdateMsg.player.id = tempPlayer.ID;
            serverUpdateMsg.player.cubePos = tempPlayer.transform.position;
            serverUpdateMsg.player.cubeRot = tempPlayer.transform.eulerAngles;
            serverUpdateMsg.player.prevBeat = System.DateTime.Now;
            SendToServer(JsonUtility.ToJson(serverUpdateMsg));
        }
    }

    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect()
    {
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}