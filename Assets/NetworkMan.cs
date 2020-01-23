using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;

    private bool spawned = false;
    private List<GameObject> cubes = new List<GameObject>();
    private List<Player> toBeSpawned = new List<Player>();

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        
        udp.Connect("3.15.19.251", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE
    };
    
    [Serializable]
    public class Message
    {
        public commands cmd;
        public Player player;
    }

    [Serializable]
    public class receivedColor
    {
        public float R;
        public float G;
        public float B;
    }

    [Serializable]
    public class Player
    {
        public string id;
       
        public receivedColor color;        
    }

    [Serializable]
    public class NewPlayer{
        
    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;

    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            switch(latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    toBeSpawned.Add(latestMessage.player);
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                   
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        
        if(toBeSpawned.Count > 0)
        {
            for (int i = 0; i < toBeSpawned.Count; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = latestMessage.player.id;
                cubes.Add(cube);
            }

            toBeSpawned.Clear();
        }
     
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            foreach(GameObject c in cubes)
            {
                if(c.name == lastestGameState.players[i].id)
                {
                    Color newColor = new Color(lastestGameState.players[i].color.R, lastestGameState.players[i].color.G, lastestGameState.players[i].color.B);
                    c.GetComponent<Renderer>().material.color = newColor;
                }
            }
            
        }
    }

    void DestroyPlayers()
    {
      
    }
    
    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}