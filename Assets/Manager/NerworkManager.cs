using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPacket
{
    public int id;
    public Type type;
    public TimeSpan time = MainModule.PastTime;
    public int src;
    public int dst;
    public object content;
    
    public enum Type
    {
        Command,
        State,
        Ack,
    }
}

public static class NetworkManager
{
    public static int jitterMin; 
    public static int jitterMax;
    private static System.Random random = new();
    private static Dictionary<int, Action<NetworkPacket>> callback = new();
    private static Dictionary<Tuple<int, int>, Queue<InternalPacket>> packetQueue = new();
    private static List<Queue<InternalPacket>> tempList = new();

    class InternalPacket
    {
        public TimeSpan EAT;
        public NetworkPacket load;
    }
    
    private static Queue<InternalPacket> GetQueue(int src, int dst)
    {
        var tuple = new Tuple<int, int>(src, dst);
        if (packetQueue.TryGetValue(tuple, out var queue))
        {
            return queue;
        }

        queue = new Queue<InternalPacket>();
        packetQueue[tuple] = queue;
        return queue;
    }
    private static Queue<InternalPacket> GetQueue(NetworkPacket packet) => GetQueue(packet.src, packet.dst);

    public static void Send(NetworkPacket packet)
    {
        MainModule.CollectSendInfo(packet);
        
        int delay = 0;
        if (packet.src == 2 && MainModule.Instance.LateCommit)
        {
            delay = MainModule.Instance.LateCommitDelay;
        }
        else if (packet.src == 3)
        {
            delay = MainModule.Instance.BadOneWayLatency;
        }
        if (packet.dst != 0 && packet.src != 0)
        {
            Debug.LogError("invalid operation!");
            return;
        }
        
        GetQueue(packet).Enqueue(new InternalPacket
        {
            EAT = MainModule.PastTime + TimeSpan.FromMilliseconds(random.Next(jitterMin, jitterMax) + delay),
            load = packet,
        });
    }
    
    public static void Release(int src, int dst)
    {
        foreach (var packet in GetQueue(src, dst))
        {
            packet.EAT = MainModule.PastTime + TimeSpan.FromMilliseconds(random.Next(jitterMin, jitterMax));
        }
    }

    public static void RegisterCb(int id, Action<NetworkPacket> cb)
    {
        if (!callback.TryAdd(id, cb))
        {
            callback[id] += cb;
        }
    }

    public static void ProcessPacket()
    {
        tempList.Clear();
        tempList.AddRange(packetQueue.Values);
        var time = MainModule.PastTime;
        foreach (var queue in tempList)
        {
            while(queue.TryPeek(out var packet) && time >= packet.EAT)
            {
                queue.Dequeue();
                callback[packet.load.dst](packet.load);
                MainModule.CollectRecvInfo(packet.load);
            }
        }
    }
}

