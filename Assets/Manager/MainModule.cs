using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MainModule: MonoBehaviour
{
    public static readonly float frameInterval = 1f / 20;
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
    public bool FastRate;
    public float FastRateScale = 1.4f;
    public bool PoorConnectionExist;
    public int BadOneWayLatency = 240;
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
        
        if (packet.dst != 0)
        {
            Debug.LogError("有问题");
        }
        var tuple = packet.content as Tuple<int, Player.Instruction>;
        
        if (!sendStat.TryAdd(new Tuple<int, int>(tuple.Item1, packet.src), PastTime))
        {
            Debug.LogError($"{tuple.Item1}-{packet.src}-{packet.dst}");
        }
    }
    
    public static void CollectRecvInfo(NetworkPacket packet)
    {
        var time = PastTime;
        switch (packet.type)
        {
            case NetworkPacket.Type.Command:
                var tuple = packet.content as Tuple<int, Player.Instruction>;
                if (packet.dst != 0)
                {
                    Debug.LogError("有问题");
                }
                if (!recvStat.TryAdd(new Tuple<int, int, int>(tuple.Item1, packet.src, 0), time))
                {
                    Debug.Log($"{tuple.Item1}-{packet.src}-0");
                }
                break;
            case NetworkPacket.Type.State:
                var world = packet.content as World;
                foreach (var player in world.playerDict.Values)
                {
                    recvStat.TryAdd(new Tuple<int, int, int>(player.frame, player.id, packet.dst), time);
                }
                break;
        }
    }

    public static TimeSpan PastTime => TimeSpan.FromSeconds(Time.timeSinceLevelLoadAsDouble);
    
    public void OnApplicationQuit()
    {
        var sb = new StringBuilder();
        sb.Append("Total frame:");
        sb.Append(Server.realtimeFrame);
        sb.Append("\nCheat:\t");
        if (LateCommit)
        {
            sb.Append($"LateCommit(Delay:{LateCommitDelay})\t");
        }
        if (FastRate)
        {
            sb.Append($"FastRate(Scale:{FastRateScale})\t");
        }
        if (!LateCommit && !FastRate)
        {
            sb.Append($"NONE");
        }
        
        sb.Append("\nAnti-Cheat:\t");
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
            else if(frame != 0)
            {
                Debug.LogError($"{frame}-{src}-{dst}");
            }
        }

        for (int i = 0; i <= Clients.Length; i++)
        {
            sb.Append($"\n");
            sb.Append(i==0?"server\t":$"client{i}\t");
            foreach (var client in Clients)
            {
                if (count[i, client.id] == 0)
                {
                    continue;
                }
                var deltaTime = (int)timeDeltaStat[i, client.id].TotalMilliseconds / count[i, client.id];
                sb.Append($"from{client.id}:{deltaTime}\t");
            }
        }
        
        Debug.Log(sb);
    }
}

