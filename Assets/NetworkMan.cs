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
    private List<GameObject> cubes = new List<GameObject>(); // all the cubes that exist in the scene
    private List<Player> toBeSpawned = new List<Player>();
    private List<Player> toBeDestroyed = new List<Player>();
    private List<Player> spawnedPlayers = new List<Player>();
    

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
        UPDATE,
        DESTROY
    };

    [Serializable]
    public class PlayerTransform
    {
        public Vector3 position;

        public Vector3 rotation;
    }

    [Serializable]
    public class Player
    {
        public string id;

        public receivedColor color;

        public Vector3 position;

        public Vector3 rotation;
    }

    [Serializable]
    public class Message
    {
        public commands cmd;
        public Player player;
        public Player[] connectedPlayers;
    }

    [Serializable]
    public class receivedColor
    {
        public float R;
        public float G;
        public float B;
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

                    for (int i = 0; i < latestMessage.connectedPlayers.Length; i++)
                    {
                        if(!IsSpawned(latestMessage.connectedPlayers[i]))
                        toBeSpawned.Add(latestMessage.connectedPlayers[i]);
                    }
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                   
                    break;

                case commands.DESTROY:
                    toBeDestroyed.Add(latestMessage.player);
                    
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
                cube.name = toBeSpawned[i].id;
                cube.AddComponent<PlayerController>();
                cube.GetComponent<PlayerController>().currentRotation = toBeSpawned[i].position;
                cube.GetComponent<PlayerController>().currentRotation = toBeSpawned[i].rotation;
                cubes.Add(cube);
                spawnedPlayers.Add(toBeSpawned[i]);
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
                    c.transform.position = lastestGameState.players[i].position;
                    c.transform.eulerAngles = lastestGameState.players[i].rotation;
                }
            }
            
        }
    }


    void DestroyPlayers()
    {
      if(toBeDestroyed.Count > 0)
        {
            for (int i = 0; i < toBeDestroyed.Count; i++)
            {
                for (int j = 0; j < cubes.Count; j++)
                {
                    if (cubes[j].name == toBeDestroyed[i].id) cubes.RemoveAt(j);
                }

                Destroy(GameObject.Find(toBeDestroyed[i].id));
            }

            toBeDestroyed.Clear();

           
        }
    }
    
    private void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    public void UpdateTransformToServer(Vector3 p, Vector3 r)
    {
        PlayerTransform playerTransform = new PlayerTransform();
        playerTransform.position = p;
        playerTransform.rotation = r;

        string jsonString = JsonUtility.ToJson(playerTransform);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(jsonString);
        udp.Send(sendBytes, sendBytes.Length);
    }

    private bool IsSpawned(Player p)
    {
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            if (p.id == spawnedPlayers[i].id) return true;
        }

        return false;
    }

    private void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
