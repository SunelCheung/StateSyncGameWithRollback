using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ServerLogic
{
    private static readonly int max_acc_delay_ms = 5000;
    private static readonly int max_window_size = (int)(max_acc_delay_ms / MainModule.frameInterval / 1000);
    private static readonly int max_jitter_ms = 100;
    private static readonly int max_jitter_size = (int)(max_jitter_ms / MainModule.frameInterval / 1000);
    public World world = new();

    public int realtimeFrame;
    public int last_sent_package_id;

    public Dictionary<int, Player.Instruction>[] instDict;
    public Dictionary<int, Vector2>[] snapshot;
    public int[] pings;
    public int[] leftWindowIndex;
    public bool[] reCalc;

    public static readonly bool Lockstep = MainModule.Instance.Lockstep;
    public static bool SymmetricDelay => MainModule.Instance.SymmetricDelay;

    public ServerLogic()
    {
        NetworkManager.RegisterCb(0, ProcessPacket);
        int size = world.playerDict.Count + 1;
        instDict = new Dictionary<int, Player.Instruction>[size];
        pings = new int[size];
        leftWindowIndex = new int[size];
        leftWindowIndex[0] = int.MaxValue;
        snapshot = new Dictionary<int, Vector2>[size];
        reCalc = new bool[size];
        
        foreach (var id in world.playerDict.Keys)
        {
            instDict[id] = new ();
            snapshot[id] = new();
        }
    }

    private void ProcessPacket(NetworkPacket packet)
    {
        if (packet.src == 0)
        {
            Debug.LogError("invalid packet: feign to others");
            return;
        }

        var timeDelta = (int)(MainModule.PastTime - packet.time).TotalMilliseconds;
        if (timeDelta < 0 || timeDelta > max_acc_delay_ms)
        {
            Debug.Log($"invalid packet: {timeDelta} ms from {packet.src} excess max tolerance {max_acc_delay_ms}");
            return;
        }

        pings[packet.src] = timeDelta;
        var tuple = packet.content as Tuple<int, Player.Instruction>;
        if (!instDict[packet.src].TryAdd(tuple.Item1, tuple.Item2))
        {
            Debug.Log($"invalid packet: repeated frame{tuple.Item1} instruction from {packet.src}");
            return;
        }

        if (tuple.Item2 != null && tuple.Item1 <= world.frame )
        {
            reCalc[packet.src] = true;
        }
        var ack_packet = new NetworkPacket
        {
            id = ++last_sent_package_id,
            src = 0,
            dst = packet.src,
            type = NetworkPacket.Type.Ack,
            content = tuple.Item1,
        };
        NetworkManager.Send(ack_packet);
    }

    public void TakeSnapshot(Player player, int frame)
    {
        snapshot[player.id][frame] = player.pos;
    }
    
    public void TakeSnapshot()
    {
        int expireFrame = world.frame - max_window_size;
        foreach (var player in world.playerDict.Values)
        {
            if (expireFrame >= 0)
            {
                instDict[player.id].Remove(expireFrame);
                snapshot[player.id].Remove(expireFrame);
                // leftWindowIndex[player.id] = Math.Max(leftWindowIndex[player.id], expireFrame);
                if (leftWindowIndex[player.id] < expireFrame)
                {
                    leftWindowIndex[player.id] = expireFrame;
                }
            }

            TakeSnapshot(player, world.frame);
        }
    }
    
    public void Update()
    {
        realtimeFrame++;
        bool update = false;
        if (!Lockstep)
        {
            foreach (var player in world.playerDict.Values)
            {
                if (reCalc[player.id])
                {
                    world[player.id].pos = snapshot[player.id][leftWindowIndex[player.id]];
                    for (int i = leftWindowIndex[player.id]; i < world.frame; i++)
                    {
                        instDict[player.id].TryGetValue(i + 1, out player.inst);
                        player.UpdatePos();
                        // if(player.inst)
                        //     Debug.LogError($"{i+1}-{player}");
                        TakeSnapshot(player, i);
                    }
                    reCalc[player.id] = false;
                }
                for (int i = leftWindowIndex[player.id]; i < world.frame; i++)
                {
                    if (instDict[player.id].ContainsKey(i + 1))
                    {
                        instDict[player.id].Remove(i+1);
                        // snapshot[player.id].Remove(i);
                        leftWindowIndex[player.id]++;
                    }
                }
            }
        }
        
        for (int i = world.frame; i < realtimeFrame; i++)
        {
            bool allArrive = true;
            if (realtimeFrame - world.frame < (Lockstep ? max_window_size : max_jitter_size)) // when realtimeFrame == world.frame can't proceed.
            {
                foreach (var player in world.playerDict.Values)
                {
                    if (!instDict[player.id].ContainsKey(world.frame + 1))
                    {
                        allArrive = false;
                        break;
                    }
                }
            }
            
            if (allArrive)
            {
                foreach (var player in world.playerDict.Values)
                {
                    if (instDict[player.id].TryGetValue(world.frame + 1, out player.inst) && Lockstep)
                    {
                        instDict[player.id].Remove(world.frame + 1);
                    }
                }

                if (!Lockstep)
                {
                    TakeSnapshot();
                }
                world.Update();
                update = true;
            }
        }
        
        if (update)
        {
            foreach (var pair in world.playerDict)
            {
                pair.Value.frame = Lockstep ? world.frame : leftWindowIndex[pair.Key];
                if (SymmetricDelay && leftWindowIndex[pair.Key] + max_jitter_size <= world.frame)
                {
                    CastState(pair.Key, leftWindowIndex[pair.Key]);
                }
                else
                {
                    CastState(pair.Key);
                }
            }
        }
    }

    public void CastState(int id, int frame = -1)
    {
        var packet = new NetworkPacket
        {
            id = ++last_sent_package_id,
            src = 0,
            dst = id,
            type = NetworkPacket.Type.State,
            content = world, // Note: this object can't be modified later!!
        };
        
        if (frame >= 0)
        {
            // Debug.LogError(frame);
            var newWorld = new World(world);
            foreach (var player in newWorld.playerDict.Values)
            {
                if (snapshot[player.id].TryGetValue(frame, out var pos))
                {
                    player.pos = pos;
                }
            }
            packet.content = newWorld;
        }
        
        NetworkManager.Send(packet);
    }
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("server realtime_frame:");
        sb.Append(realtimeFrame);
        sb.Append("\t ping:");
        sb.Append(pings[1]);
        sb.Append(" ");
        sb.Append(pings[2]);
        sb.Append("\n");
        sb.Append(world);
        return sb.ToString();
    }
}