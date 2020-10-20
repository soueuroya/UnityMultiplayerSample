using System;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;

    private List<NetworkObjects.NetworkPlayer> playerList = new List<NetworkObjects.NetworkPlayer>();
    private const float heartbeatTime = 6.0f;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        InvokeRepeating("ClientUpdate", 1, Utility.interval);
    }

    void ClientUpdate()
    {
        PlayerUpdateMsg playerUpdateMsg = new PlayerUpdateMsg(playerList);

        foreach (NetworkConnection c in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(playerUpdateMsg), c);
        }
    }

    void SendToClient(string message, NetworkConnection c)
    {
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c)
    {
        // Example to send a handshake message:
        NewClientMsg newClientMsg = new NewClientMsg();
        newClientMsg.player.id = c.InternalId.ToString();
         
        //position arrangement
        newClientMsg.player.cubePos = new Vector3(playerList.Count * 2.0f - 6.0f, 0.0f, 0.0f);

        foreach (NetworkConnection connection in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(newClientMsg), connection);
        }

        PlayerListMsg list = new PlayerListMsg(playerList);
        SendToClient(JsonUtility.ToJson(list), c);

        newClientMsg.cmd = Commands.PLAYER_ID;
        SendToClient(JsonUtility.ToJson(newClientMsg), c);      
        playerList.Add(newClientMsg.player);

        m_Connections.Add(c);
        Debug.Log("Server msg: Accepted a connection");
    }

    void OnData(DataStreamReader stream, int i)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.NEW_CLIENT:
                NewClientMsg ncMsg = JsonUtility.FromJson<NewClientMsg>(recMsg);
                Debug.Log("Server msg: New Client message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Server msg: Player update message received!");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                ServerMessage(suMsg);
                break;
            default:
                Debug.Log("SERVER ERROR: Unrecognized message received!");
                break;
        }
    }

    void ServerMessage(ServerUpdateMsg suMsg)
    {
        foreach (NetworkObjects.NetworkPlayer player in playerList)
            Utility.PassTransform(player, suMsg.player);
        Debug.Log("Server msg: Server update message received!");
        foreach (NetworkObjects.NetworkPlayer p in playerList)
        {
            Debug.Log("[ID: " + p.id + ", Position: "
                + p.cubePos + ", rotation: " + p.cubeRot + "]");
        }
    }

    void OnDisconnect(int i)
    {
        Debug.Log("Server msg: Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                for (int j = 0; j < playerList.Count; j++)
                    m_Connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        List<NetworkObjects.NetworkPlayer> droppedPlayers = new List<NetworkObjects.NetworkPlayer>();

        for (int i = 0; i < playerList.Count; i++)
        {
            if ((System.DateTime.Now - playerList[i].prevBeat).TotalSeconds > heartbeatTime)
            {
                NetworkObjects.NetworkPlayer temp = playerList[i];
                playerList.RemoveAt(i--);
                droppedPlayers.Add(temp);
            }
        }

        if (droppedPlayers.Count > 0)
        {
            DropPlayerMsg dpMsg = new DropPlayerMsg(droppedPlayers);
            for (int i = 0; i < m_Connections.Length; i++)
            {
                SendToClient(JsonUtility.ToJson(dpMsg), m_Connections[i]);
            }
        }

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c != default(NetworkConnection))
        {
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);

            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}