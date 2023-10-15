using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MainModule: MonoBehaviour
{
    public static readonly float frameInterval = 0.05f;
    private static MainModule _instance;
    public static MainModule Instance => _instance;
    
    public static InputManager InputManager;
    public static PlayerProxy PlayerProxy;
    public static ClientLocal[] Clients = { new(1), new(2), new(3) };
    public static ServerLogic Server;
    private static Dictionary<Tuple<int, int>, DateTime> sendStat = new ();
    private static Dictionary<Tuple<int, int, int>, DateTime> recvStat = new ();
    public bool Lockstep;
    public bool SymmetricDelay;
    public bool LateCommit;
    public int LateCommitDelay = 4000;
    public int BadOneWayLatency = 500;
    public GameObject[] Player;
    // public bool FastRate;
    private static float curFixedTime;
    private static float lastUpdateTime;

    public void Awake()
    {
        _instance = this;
        Server = new();
        InputManager = gameObject.GetOrAddComponent<InputManager>();
        PlayerProxy = gameObject.GetOrAddComponent<PlayerProxy>();
    }
    
    public void Update()
    {
        NetworkManager.ProcessPacket();
        
        if (curFixedTime - lastUpdateTime >= frameInterval)
        {
            foreach (var client in Clients)
            {
                client.Update();
            }
            Server.Update();
            lastUpdateTime += frameInterval;
        }

        curFixedTime += Time.deltaTime;
    }

    public static void CollectSendInfo(NetworkPacket packet)
    {
        if (packet.type != NetworkPacket.Type.Command)
        {
            return;
        }
        
        var tuple = packet.content as Tuple<int, Player.Instruction>;
                
        if (!sendStat.TryAdd(new Tuple<int, int>(tuple.Item1 + 1, packet.src), DateTime.Now))
        {
            Debug.LogError($"{tuple.Item1 + 1}-{packet.src}-{packet.dst}");
        }
        
    }
    
    public static void CollectRecvInfo(NetworkPacket packet)
    {
        var nowTime = DateTime.Now;
        switch (packet.type)
        {
            case NetworkPacket.Type.Command:
                var tuple = packet.content as Tuple<int, Player.Instruction>;
                if (!recvStat.TryAdd(new Tuple<int, int, int>(tuple.Item1 + 1, packet.src, packet.dst), nowTime))
                {
                    Debug.Log($"{tuple.Item1 + 1}-{packet.src}-{packet.dst}");
                }
                break;
            case NetworkPacket.Type.State:
                foreach (var player in (packet.content as World).playerDict.Values)
                {
                    if (!recvStat.TryAdd(new Tuple<int, int, int>(player.frame, player.id, packet.dst), nowTime))
                    {
                        // Debug.Log($"{player.frame}-{player.id}-{packet.dst}");
                    }
                }
                break;
            // default:
            //     return;
        }
    }

    public void OnApplicationQuit()
    {
        var sb = new StringBuilder();
        sb.Append("Total frame:");
        sb.Append(Server.realtimeFrame);
        sb.Append("\n");
        if (LateCommit)
        {
            sb.Append("LateCommit ON");
            sb.Append("\n");
        }
        sb.Append("Anti-Cheat:");
        if (Lockstep)
        {
            sb.Append("Lockstep");
        }
        else if (SymmetricDelay)
        {
            sb.Append("SymmetricDelay");
        }
        else
        {
            sb.Append("NONE");
        }
        
        var timeDeltaStat = new TimeSpan[Clients.Length + 1, Clients.Length + 1];
        var count = new int[Clients.Length + 1, Clients.Length + 1];
        foreach (var pair in recvStat)
        {
            (int frame, int src, int dst) = pair.Key;
            if (sendStat.TryGetValue(new Tuple<int, int>(frame, src), out var sendTime))
            {
                timeDeltaStat[dst, src] += pair.Value - sendTime;
                count[dst, src]++;
            }
            else
            {
                Debug.LogError("有问题");
            }
        }

        for (int i = 0; i <= Clients.Length; i++)
        {
            sb.Append($"\n");
            sb.Append(i==0?"server":$"client{i}");
            foreach (var client in Clients)
            {
                if (count[i, client.id] == 0)
                {
                    continue;
                }
                var deltaTime = (int)timeDeltaStat[i, client.id].TotalMilliseconds / count[i, client.id];
                sb.Append($"\tfrom{client.id}:{deltaTime}");
            }
        }
        
        Debug.Log(sb);
    }
}

