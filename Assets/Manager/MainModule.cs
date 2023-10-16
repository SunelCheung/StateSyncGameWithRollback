using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public class MainModule: MonoBehaviour
{
    public static readonly float frameInterval = 0.05f;
    private static MainModule _instance;
    public static MainModule Instance => _instance;
    
    public static InputManager InputManager;
    public static PlayerProxy PlayerProxy;
    public static ClientLocal[] Clients = { new(1), new(2), new(3) };
    public static ServerLogic Server;
    private static Dictionary<Tuple<int, int>, TimeSpan> sendStat = new ();
    private static Dictionary<Tuple<int, int, int>, TimeSpan> recvStat = new ();
    public bool Lockstep;
    public bool SymmetricDelay;
    public bool LateCommit;
    public int LateCommitDelay = 4000;
    public bool PoorConnectionExist;
    public int BadOneWayLatency = 500;
    public bool FastRate;
    public float FastRateScale = 1.4f;
    public int JitterMin; // one-way
    public int JitterMax; // one-way
    public GameObject[] Player;

    public void Awake()
    {
        _instance = this;
        // startTime = DateTime.Now;
        Server = new();
        InputManager = gameObject.GetOrAddComponent<InputManager>();
        PlayerProxy = gameObject.GetOrAddComponent<PlayerProxy>();
        NetworkManager.jitterMin = JitterMin; 
        NetworkManager.jitterMax = JitterMax; 
    }
    
    public void Update()
    {
        foreach (var client in Clients)
        {
            client.TryUpdate();
            NetworkManager.ProcessPacket();
            Server.TryUpdate();
        }
    }
    
    public static void CollectSendInfo(NetworkPacket packet)
    {
        if (packet.type != NetworkPacket.Type.Command)
        {
            return;
        }
        
        var tuple = packet.content as Tuple<int, Player.Instruction>;
                
        if (!sendStat.TryAdd(new Tuple<int, int>(tuple.Item1 + 1, packet.src), PastTime))
        {
            Debug.LogError($"{tuple.Item1 + 1}-{packet.src}-{packet.dst}");
        }
    }
    
    public static void CollectRecvInfo(NetworkPacket packet)
    {
        var time = PastTime;
        switch (packet.type)
        {
            case NetworkPacket.Type.Command:
                var tuple = packet.content as Tuple<int, Player.Instruction>;
                if (!recvStat.TryAdd(new Tuple<int, int, int>(tuple.Item1 + 1, packet.src, packet.dst), time))
                {
                    Debug.Log($"{tuple.Item1 + 1}-{packet.src}-{packet.dst}");
                }
                break;
            case NetworkPacket.Type.State:
                foreach (var player in (packet.content as World).playerDict.Values)
                {
                    if (!recvStat.TryAdd(new Tuple<int, int, int>(player.frame, player.id, packet.dst), time))
                    {
                        // Debug.Log($"{player.frame}-{player.id}-{packet.dst}");
                    }
                }
                break;
            // default:
            //     return;
        }
    }

    public static TimeSpan PastTime => TimeSpan.FromSeconds(Time.timeSinceLevelLoadAsDouble);
    
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
            // else
            // {
            //     Debug.LogError("有问题");
            // }
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
